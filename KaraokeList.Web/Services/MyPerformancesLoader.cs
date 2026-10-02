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
}

public sealed class MyPerformancesLoader(
    IKaraokeApiClient api,
    IMyPerformancesLocalStore store) : IMyPerformancesLoader
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
                await store.SaveCachedAsync(new CachedMyPerformances(
                    result.Performances,
                    cachedAt,
                    CurrentCacheSchemaVersion));

                return BuildResult(result.Performances, FromCache: false, cachedAt);
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
