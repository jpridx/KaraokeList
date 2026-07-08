using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMyPerformancesLoader
{
    Task<MyPerformancesLoadResult> LoadAsync();

    Task<MyPerformancesLoadResult?> TryGetCachedAsync();
}

public sealed class MyPerformancesLoader(
    IKaraokeApiClient api,
    IMyPerformancesLocalStore store) : IMyPerformancesLoader
{
    private const int CurrentCacheSchemaVersion = 1;

    public async Task<MyPerformancesLoadResult> LoadAsync()
    {
        try
        {
            var result = await api.GetMyPerformancesAsync(venueId: null, sortDir: "desc");
            if (!result.Succeeded)
            {
                return await LoadOfflineOrFailAsync(
                    result.ErrorMessage,
                    result.ErrorMessage?.Contains("not linked", StringComparison.OrdinalIgnoreCase) == true);
            }

            var cachedAt = DateTime.UtcNow;
            await store.SaveCachedAsync(new CachedMyPerformances(
                result.Performances,
                cachedAt,
                CurrentCacheSchemaVersion));

            return BuildResult(result.Performances, FromCache: false, cachedAt);
        }
        catch (Exception ex) when (IsOfflineFailure(ex))
        {
            return await LoadOfflineOrFailAsync(null, needsSingerLink: false);
        }
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

    private static bool IsOfflineFailure(Exception ex) =>
        ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException;
}
