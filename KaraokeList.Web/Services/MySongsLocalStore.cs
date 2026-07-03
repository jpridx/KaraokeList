using Blazored.LocalStorage;

using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMySongsLocalStore
{
    Task<bool> GetShowGenreFiltersAsync();
    Task SetShowGenreFiltersAsync(bool show);
    Task<SingerListKind> GetListKindAsync();
    Task SetListKindAsync(SingerListKind kind);
    Task<CachedMySongsLists?> GetCachedListsAsync();
    Task SaveCachedListsAsync(CachedMySongsLists cache);
    Task ClearCatalogCacheAsync();
}

public sealed class MySongsLocalStore(ILocalStorageService localStorage) : IMySongsLocalStore
{
    private const string ShowGenreFiltersKey = "karaoke.mySongs.showGenreFilters";
    private const string ListKindKey = "karaoke.mySongs.listKind";
    private const string CachedListsKey = "karaoke.mySongs.cachedLists";

    public async Task<bool> GetShowGenreFiltersAsync()
    {
        var value = await localStorage.GetItemAsync<bool?>(ShowGenreFiltersKey);
        return value ?? false;
    }

    public Task SetShowGenreFiltersAsync(bool show) =>
        localStorage.SetItemAsync(ShowGenreFiltersKey, show).AsTask();

    public async Task<SingerListKind> GetListKindAsync()
    {
        var value = await localStorage.GetItemAsync<SingerListKind?>(ListKindKey);
        return value ?? SingerListKind.MyRepertoire;
    }

    public Task SetListKindAsync(SingerListKind kind) =>
        localStorage.SetItemAsync(ListKindKey, kind).AsTask();

    public Task<CachedMySongsLists?> GetCachedListsAsync() =>
        localStorage.GetItemAsync<CachedMySongsLists?>(CachedListsKey).AsTask();

    public Task SaveCachedListsAsync(CachedMySongsLists cache) =>
        localStorage.SetItemAsync(CachedListsKey, cache).AsTask();

    public Task ClearCatalogCacheAsync() =>
        localStorage.RemoveItemAsync(CachedListsKey).AsTask();
}
