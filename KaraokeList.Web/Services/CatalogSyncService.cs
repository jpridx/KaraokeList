using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record CatalogSyncResult(bool Succeeded, string? ErrorMessage)
{
    public static CatalogSyncResult Ok() => new(true, null);
    public static CatalogSyncResult Fail(string message) => new(false, message);
}

public interface ICatalogSyncService
{
    Task<CatalogSyncResult> SyncFromServerAsync();
}

/// <summary>
/// Standard sync: log catalog (repertoire stats + exclusions), My Songs lists, My Performances, tickler settings.
/// </summary>
public sealed class CatalogSyncService(
    ILogCatalogLoader logCatalogLoader,
    IMySongsLoader mySongsLoader,
    IMyPerformancesLoader performancesLoader,
    IKaraokeApiClient api,
    ITicklerSettingsLocalStore ticklerSettingsStore,
    ICatalogVersionService versionService,
    ILogPerformanceLocalStore logStore) : ICatalogSyncService
{
    public async Task<CatalogSyncResult> SyncFromServerAsync()
    {
        try
        {
            versionService.Invalidate();

            await logCatalogLoader.LoadAsync();
            await mySongsLoader.LoadAsync(
                SingerListKind.MyRepertoire,
                sortBy: "lastPerformed",
                sortDir: "desc",
                genreId: null);
            var performances = await performancesLoader.LoadAsync();
            await HydrateRecentLogsAsync(performances.Performances);

            var settingsResult = await api.GetTicklerSettingsAsync();
            if (settingsResult.Succeeded && settingsResult.Settings is not null)
            {
                await ticklerSettingsStore.SaveAsync(settingsResult.Settings);
            }

            return CatalogSyncResult.Ok();
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return CatalogSyncResult.Fail(ApiTransientFailure.ColdStartMessage);
        }
        catch (Exception ex)
        {
            return CatalogSyncResult.Fail(ex.Message);
        }
    }

    private async Task HydrateRecentLogsAsync(IReadOnlyList<MyPerformanceEntryDto> performances)
    {
        if (performances.Count == 0)
        {
            return;
        }

        var localLogs = await logStore.GetRecentLogsAsync();
        var pending = await logStore.GetPendingPerformancesAsync();
        var merged = RecentLogsHydrator.Merge(performances, localLogs, pending);
        if (merged.Count == 0)
        {
            return;
        }

        await logStore.ReplaceRecentLogsAsync(merged);
    }
}
