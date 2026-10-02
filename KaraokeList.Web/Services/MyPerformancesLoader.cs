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
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return await LoadOfflineOrFailAsync(null, needsSingerLink: false);
        }
    }

    private async Task<MyPerformancesLoadResult> PreferCachedOrTransientApiResultAsync(
        IReadOnlyList<MyPerformanceEntryDto> apiPerformances)
    {
        var cached = await TryGetCachedAsync();
        if (cached is not null)
        {
            return cached;
        }

        return BuildResult(apiPerformances, FromCache: false, DateTime.UtcNow);
    }

    public async Task<MyPerformancesLoadResult?> TryGetCachedAsync()
    {
        var cached = await store.GetCachedAsync();
        if (cached is null || cached.Performances.Count == 0)
        {
            return null;
        }

        return BuildResult(cached.Performances, FromCache: true, cached.CachedAtUtc);
    }

    private async Task<MyPerformancesLoadResult> LoadOfflineOrFailAsync(
        string? errorMessage,
        bool needsSingerLink)
    {
        var cached = await store.GetCachedAsync();
        if (cached is null || cached.Performances.Count == 0)
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

        return BuildResult(cached.Performances, FromCache: true, cached.CachedAtUtc);
    }

    public async Task PatchPerformanceAsync(MyPerformanceEntryDto updated)
    {
        InvalidateInFlightLoads();

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

    public async Task RemovePerformanceAsync(int performanceId)
    {
        InvalidateInFlightLoads();

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

}
