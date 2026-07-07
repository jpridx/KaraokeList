using Blazored.LocalStorage;

namespace KaraokeList.Web.Services;

public interface ISingerProfileLocalStore
{
    Task<int?> GetCachedSingerIdAsync();
    Task SaveCachedSingerIdAsync(int singerId);
    Task ClearCachedSingerIdAsync();
}

public sealed class SingerProfileLocalStore(ILocalStorageService localStorage) : ISingerProfileLocalStore
{
    private const string CachedSingerIdKey = "karaoke.profile.cachedSingerId";

    public async Task<int?> GetCachedSingerIdAsync()
    {
        var value = await localStorage.GetItemAsync<int?>(CachedSingerIdKey);
        return value is > 0 ? value : null;
    }

    public Task SaveCachedSingerIdAsync(int singerId) =>
        localStorage.SetItemAsync(CachedSingerIdKey, singerId).AsTask();

    public Task ClearCachedSingerIdAsync() =>
        localStorage.RemoveItemAsync(CachedSingerIdKey).AsTask();
}
