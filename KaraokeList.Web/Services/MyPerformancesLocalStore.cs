using Blazored.LocalStorage;

namespace KaraokeList.Web.Services;

public interface IMyPerformancesLocalStore
{
    Task<CachedMyPerformances?> GetCachedAsync();
    Task SaveCachedAsync(CachedMyPerformances cache);
    Task ClearCacheAsync();
}

public sealed class MyPerformancesLocalStore(ILocalStorageService localStorage) : IMyPerformancesLocalStore
{
    private const string CachedKey = "karaoke.myPerformances.cached";

    public Task<CachedMyPerformances?> GetCachedAsync() =>
        localStorage.GetItemAsync<CachedMyPerformances?>(CachedKey).AsTask();

    public Task SaveCachedAsync(CachedMyPerformances cache) =>
        localStorage.SetItemAsync(CachedKey, cache).AsTask();

    public Task ClearCacheAsync() =>
        localStorage.RemoveItemAsync(CachedKey).AsTask();
}
