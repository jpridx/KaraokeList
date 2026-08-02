using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record CatalogSyncResult(bool Succeeded, string? ErrorMessage)
{
    public static CatalogSyncResult Ok() => new(true, null);
    public static CatalogSyncResult Fail(string message) => new(false, message);
}

public interface ICatalogSyncService
{
    Task<CatalogSyncResult> SyncFromServerAsync(CancellationToken cancellationToken = default);
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
    ICatalogVersionService versionService) : ICatalogSyncService
{
    public async Task<CatalogSyncResult> SyncFromServerAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            versionService.Invalidate();

            await logCatalogLoader.LoadAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await mySongsLoader.LoadAsync(
                SingerListKind.MyRepertoire,
                sortBy: "lastPerformed",
                sortDir: "desc",
                genreId: null);
            cancellationToken.ThrowIfCancellationRequested();

            await performancesLoader.LoadAsync();
            cancellationToken.ThrowIfCancellationRequested();

            var settingsResult = await api.GetTicklerSettingsAsync();
            if (settingsResult.Succeeded && settingsResult.Settings is not null)
            {
                await ticklerSettingsStore.SaveAsync(settingsResult.Settings);
            }

            return CatalogSyncResult.Ok();
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is OperationCanceledException)
        {
            if (ex is OperationCanceledException)
            {
                throw;
            }

            return CatalogSyncResult.Fail(ApiTransientFailure.ColdStartMessage);
        }
        catch (Exception ex)
        {
            return CatalogSyncResult.Fail(ex.Message);
        }
    }
}
