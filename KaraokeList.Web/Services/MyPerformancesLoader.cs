using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMyPerformancesLoader
{
    /// <summary>
    /// Prevents in-flight <see cref="LoadAsync"/> calls from persisting API snapshots to local storage.
    /// </summary>
    void InvalidateInFlightLoads();

    Task<MyPerformancesLoadResult> LoadAsync();

    Task<MyPerformancesLoadResult?> TryGetCachedAsync();

    Task PatchPerformanceAsync(MyPerformanceEntryDto updated);

    Task RemovePerformanceAsync(int performanceId);

    /// <summary>
    /// Inserts a performance at the front of an existing cache.
    /// When <paramref name="created"/> has no id, assigns one above the cached max so same-day lists stay newest-first.
    /// Does nothing when My Performances has not been cached yet.
    /// </summary>
    Task AddPerformanceAsync(MyPerformanceEntryDto created);
}

public sealed class MyPerformancesLoader(
    IKaraokeApiClient api,
    IMyPerformancesLocalStore store,
    ILogPerformanceLocalStore? logStore = null) : IMyPerformancesLoader
{
    private const int CurrentCacheSchemaVersion = 1;
    private int loadCommitGeneration;
    private readonly SemaphoreSlim cacheCommitLock = new(1, 1);

    public void InvalidateInFlightLoads() => Interlocked.Increment(ref loadCommitGeneration);

    public async Task<MyPerformancesLoadResult> LoadAsync()
    {
        var commitGeneration = Volatile.Read(ref loadCommitGeneration);
        try
        {
            var result = await api.GetMyPerformancesAsync(venueId: null, sortDir: "desc");
            if (!result.Succeeded)
            {
                return await LoadOfflineOrFailAsync(
                    result.ErrorMessage,
                    result.ErrorMessage?.Contains("not linked", StringComparison.OrdinalIgnoreCase) == true);
            }

            await cacheCommitLock.WaitAsync();
            try
            {
                if (commitGeneration != Volatile.Read(ref loadCommitGeneration))
                {
                    return await PreferCachedOrTransientApiResultAsync(result.Performances);
                }

                var cachedAt = DateTime.UtcNow;
                var performances = await MergeUnsyncedPendingAsync(result.Performances);
                await store.SaveCachedAsync(new CachedMyPerformances(
                    performances,
                    cachedAt,
                    CurrentCacheSchemaVersion));

                return BuildResult(performances, FromCache: false, cachedAt);
            }
            finally
            {
                cacheCommitLock.Release();
            }
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return await LoadOfflineOrFailAsync(null, needsSingerLink: false);
        }
    }

    private async Task<MyPerformancesLoadResult> PreferCachedOrTransientApiResultAsync(
        IReadOnlyList<MyPerformanceEntryDto> _)
    {
        var stored = await store.GetCachedAsync();
        if (stored is not null)
        {
            return BuildStoredCacheResult(stored);
        }

        // Do not return a stale API snapshot when this load was invalidated and nothing is stored.
        return BuildResult([], FromCache: false, null);
    }

    public async Task<MyPerformancesLoadResult?> TryGetCachedAsync()
    {
        var cached = await store.GetCachedAsync();
        if (cached is null)
        {
            return null;
        }

        return BuildStoredCacheResult(cached);
    }

    private async Task<MyPerformancesLoadResult> LoadOfflineOrFailAsync(
        string? errorMessage,
        bool needsSingerLink)
    {
        var cached = await store.GetCachedAsync();
        if (cached is null)
        {
            if (needsSingerLink)
            {
                return new MyPerformancesLoadResult(
                    [],
                    FromCache: false,
                    HasCache: false,
                    null,
                    errorMessage,
                    true);
            }

            return new MyPerformancesLoadResult(
                [],
                FromCache: true,
                HasCache: false,
                null,
                errorMessage ?? "Could not load performances. Open My Performances once while online to cache them.",
                false);
        }

        return BuildStoredCacheResult(cached);
    }

    public async Task PatchPerformanceAsync(MyPerformanceEntryDto updated)
    {
        InvalidateInFlightLoads();

        await cacheCommitLock.WaitAsync();
        try
        {
            var cached = await store.GetCachedAsync();
            if (cached is null)
            {
                return;
            }

            var performances = cached.Performances.ToList();
            var index = performances.FindIndex(p => p.Id == updated.Id);
            if (index < 0)
            {
                return;
            }

            performances[index] = updated;
            await store.SaveCachedAsync(new CachedMyPerformances(
                performances,
                DateTime.UtcNow,
                cached.SchemaVersion));
        }
        finally
        {
            cacheCommitLock.Release();
        }
    }

    public async Task AddPerformanceAsync(MyPerformanceEntryDto created)
    {
        InvalidateInFlightLoads();

        await cacheCommitLock.WaitAsync();
        try
        {
            var cached = await store.GetCachedAsync();
            if (cached is null)
            {
                return;
            }

            if (created.Id <= 0)
            {
                var maxId = cached.Performances.Count == 0
                    ? 0
                    : cached.Performances.Max(p => p.Id);
                created.Id = maxId + 1;
            }

            var performances = cached.Performances.ToList();
            performances.Insert(0, created);
            await store.SaveCachedAsync(new CachedMyPerformances(
                performances,
                DateTime.UtcNow,
                cached.SchemaVersion));
        }
        finally
        {
            cacheCommitLock.Release();
        }
    }

    public async Task RemovePerformanceAsync(int performanceId)
    {
        InvalidateInFlightLoads();

        await cacheCommitLock.WaitAsync();
        try
        {
            var cached = await store.GetCachedAsync();
            if (cached is null)
            {
                return;
            }

            var performances = cached.Performances.Where(p => p.Id != performanceId).ToList();
            if (performances.Count == cached.Performances.Count)
            {
                return;
            }

            await store.SaveCachedAsync(new CachedMyPerformances(
                performances,
                DateTime.UtcNow,
                cached.SchemaVersion));
        }
        finally
        {
            cacheCommitLock.Release();
        }
    }

    private async Task<IReadOnlyList<MyPerformanceEntryDto>> MergeUnsyncedPendingAsync(
        IReadOnlyList<MyPerformanceEntryDto> performances)
    {
        if (logStore is null)
        {
            return performances;
        }

        var pending = await logStore.GetPendingPerformancesAsync();
        if (pending.Count == 0)
        {
            return performances;
        }

        var merged = performances.ToList();
        var unmatchedOnServer = performances
            .GroupBy(p => (p.SongId, p.PerformedOn.Date, p.VenueId))
            .ToDictionary(group => group.Key, group => group.Count());
        var maxId = merged.Count == 0 ? 0 : merged.Max(p => p.Id);

        for (var i = pending.Count - 1; i >= 0; i--)
        {
            var entry = pending[i];
            var key = (entry.SongId, entry.PerformedOn.Date, (int?)entry.VenueId);
            if (unmatchedOnServer.TryGetValue(key, out var remaining) && remaining > 0)
            {
                unmatchedOnServer[key] = remaining - 1;
                continue;
            }

            maxId++;
            merged.Insert(0, new MyPerformanceEntryDto
            {
                Id = maxId,
                SongId = entry.SongId,
                Title = entry.Title,
                ArtistName = entry.ArtistName,
                ArtistDisplay = entry.ArtistName,
                PerformedOn = entry.PerformedOn,
                VenueId = entry.VenueId,
                VenueName = entry.VenueName,
                KeyChangeSemitones = entry.KeyChangeSemitones,
                OtherPerformers = (entry.OtherPerformers ?? [])
                    .Select(p => new CoPerformerDto
                    {
                        SingerId = p.SingerId,
                        Name = p.DisplayName ?? string.Empty
                    })
                    .ToList()
            });
        }

        return merged;
    }

    private static MyPerformancesLoadResult BuildResult(
        IReadOnlyList<MyPerformanceEntryDto> performances,
        bool FromCache,
        DateTime? cachedAt) =>
        new(
            performances,
            FromCache,
            HasCache: performances.Count > 0,
            cachedAt,
            null,
            false);

    private static MyPerformancesLoadResult BuildStoredCacheResult(CachedMyPerformances cached) =>
        new(
            cached.Performances,
            FromCache: true,
            HasCache: true,
            cached.CachedAtUtc,
            null,
            false);

}
