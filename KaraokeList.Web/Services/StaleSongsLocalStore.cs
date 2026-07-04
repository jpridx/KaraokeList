using Blazored.LocalStorage;

namespace KaraokeList.Web.Services;

public interface IStaleSongsLocalStore
{
    Task<CachedStaleSongs?> GetCachedAsync();
    Task SaveCachedAsync(CachedStaleSongs cache);
    Task ClearCacheAsync();
}

public sealed class StaleSongsLocalStore(ILocalStorageService localStorage) : IStaleSongsLocalStore
{
    private const string CachedKey = "karaoke.staleSongs.cached";

    public Task<CachedStaleSongs?> GetCachedAsync() =>
        localStorage.GetItemAsync<CachedStaleSongs?>(CachedKey).AsTask();

    public Task SaveCachedAsync(CachedStaleSongs cache) =>
        localStorage.SetItemAsync(CachedKey, cache).AsTask();

    public Task ClearCacheAsync() =>
        localStorage.RemoveItemAsync(CachedKey).AsTask();
}
