namespace KaraokeList.Web.Services;

public interface IStaleSongsLoader
{
    /// <summary>
    /// Returns the last cached result from local storage without touching the API.
    /// Returns null when no cache is available.
    /// </summary>
    Task<StaleSongsLoadResult?> TryGetCachedAsync();

    /// <summary>
    /// Fetches fresh data from the API and saves it to local storage on success.
    /// Falls back to the cached result only when the API is unreachable (offline / transient failure).
    /// </summary>
    Task<StaleSongsLoadResult> LoadAsync();
}

/// <summary>
/// Fetches fresh data from the API and saves it to local storage on success.
/// Falls back to the cached result only when the API is unreachable (offline / transient failure).
/// </summary>
public sealed class StaleSongsLoader(
    IKaraokeApiClient api,
    IStaleSongsLocalStore store) : IStaleSongsLoader
{
    public async Task<StaleSongsLoadResult?> TryGetCachedAsync()
    {
        var cached = await store.GetCachedAsync();
        if (cached is null)
        {
            return null;
        }

        return StaleSongsLoadResult.Cached(cached.Response, cached.CachedAtUtc);
    }

    public async Task<StaleSongsLoadResult> LoadAsync()
    {
        var result = await api.GetMyStaleSongsAsync();
        if (result.Succeeded && result.Response is not null)
        {
            await store.SaveCachedAsync(new CachedStaleSongs(result.Response, DateTime.UtcNow));
            return StaleSongsLoadResult.Live(result.Response);
        }

        // API failed — show the last cached result if available.
        var cached = await store.GetCachedAsync();
        if (cached is not null)
        {
            return StaleSongsLoadResult.Cached(cached.Response, cached.CachedAtUtc);
        }

        return StaleSongsLoadResult.Failed(
            result.ErrorMessage ?? "Could not load stale songs. Connect and reload to see suggestions.");
    }
}
