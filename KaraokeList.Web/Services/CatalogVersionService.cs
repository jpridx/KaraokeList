namespace KaraokeList.Web.Services;

public interface ICatalogVersionService
{
    /// <summary>
    /// Returns the current server-side cache tag, or null if the server is unreachable.
    /// The result is memoised for the duration of the app session (one fetch per tab lifetime).
    /// </summary>
    Task<string?> GetCacheTagAsync(bool forceRefresh = false);

    /// <summary>
    /// Discards any memoised tag so the next call fetches from the server again.
    /// </summary>
    void Invalidate();
}

public sealed class CatalogVersionService(IKaraokeApiClient api) : ICatalogVersionService
{
    private string? _tag;
    private bool _fetched;

    public async Task<string?> GetCacheTagAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _fetched)
        {
            return _tag;
        }

        try
        {
            var versionTask = api.GetAppVersionAsync();
            if (await Task.WhenAny(versionTask, Task.Delay(ApiSlowRequestNotifier.PageLoadTimeout))
                != versionTask)
            {
                return _fetched ? _tag : null;
            }

            var version = await versionTask;
            _tag = version?.CacheTag;
            _fetched = true;
        }
        catch
        {
            // Offline or API error. If we already have a known tag, keep using it.
            if (_fetched)
            {
                return _tag;
            }
        }

        return _tag;
    }

    public void Invalidate()
    {
        _tag = null;
        _fetched = false;
    }
}
