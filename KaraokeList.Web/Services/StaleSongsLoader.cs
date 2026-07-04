namespace KaraokeList.Web.Services;

public interface IStaleSongsLoader
{
    Task<StaleSongsLoadResult> LoadAsync();
}

/// <summary>
/// Always fetches fresh data from the API and saves it to local storage on success.
/// Falls back to the cached result only when the API is unreachable (offline / transient failure).
/// </summary>
public sealed class StaleSongsLoader(
    IKaraokeApiClient api,
    IStaleSongsLocalStore store) : IStaleSongsLoader
{
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
