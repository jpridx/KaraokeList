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
/// Standard sync: log catalog (repertoire stats + exclusions), My Songs lists, My Performances,
/// and tickler settings (explicit fetch ensures settings refresh even when list caches are fresh).
/// </summary>
public sealed class CatalogSyncService(
    ILogCatalogLoader logCatalogLoader,
    IMySongsLoader mySongsLoader,
    IMyPerformancesLoader performancesLoader,
    IPerformanceCacheCoordinator performanceCacheCoordinator,
    IKaraokeApiClient api,
    ITicklerSettingsLocalStore ticklerSettingsStore,
    ICatalogVersionService versionService) : ICatalogSyncService
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
            await performancesLoader.LoadAsync();
            await performanceCacheCoordinator.RebuildRecentLogsFromPerformancesAsync();

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
}
