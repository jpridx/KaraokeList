using Blazored.LocalStorage;

using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMySongsLocalStore
{
    Task<bool> GetShowGenreFiltersAsync();
    Task SetShowGenreFiltersAsync(bool show);
    Task<SingerListKind> GetListKindAsync();
    Task SetListKindAsync(SingerListKind kind);
    Task<string> GetSortByAsync();
    Task<string> GetSortDirAsync();
    Task SetSortPreferenceAsync(string sortBy, string sortDir);
    Task<CachedMySongsLists?> GetCachedListsAsync();
    Task SaveCachedListsAsync(CachedMySongsLists cache);
    Task ClearCatalogCacheAsync();
}

public sealed class MySongsLocalStore(ILocalStorageService localStorage) : IMySongsLocalStore
{
    private const string ShowGenreFiltersKey = "karaoke.mySongs.showGenreFilters";
    private const string ListKindKey = "karaoke.mySongs.listKind";
    private const string SortByKey = "karaoke.mySongs.sortBy";
    private const string SortDirKey = "karaoke.mySongs.sortDir";
    private const string CachedListsKey = "karaoke.mySongs.cachedLists";

    private static readonly HashSet<string> AllowedSortBy =
        new(StringComparer.OrdinalIgnoreCase) { "lastPerformed", "title", "artist", "genre" };

    private static readonly HashSet<string> AllowedSortDir =
        new(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };

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

    public async Task<string> GetSortByAsync()
    {
        var value = await localStorage.GetItemAsync<string?>(SortByKey);
        return IsAllowedSortBy(value) ? value! : "lastPerformed";
    }

    public async Task<string> GetSortDirAsync()
    {
        var value = await localStorage.GetItemAsync<string?>(SortDirKey);
        return IsAllowedSortDir(value) ? value! : "desc";
    }

    public async Task SetSortPreferenceAsync(string sortBy, string sortDir)
    {
        await localStorage.SetItemAsync(SortByKey, IsAllowedSortBy(sortBy) ? sortBy : "lastPerformed");
        await localStorage.SetItemAsync(SortDirKey, IsAllowedSortDir(sortDir) ? sortDir : "desc");
    }

    public Task<CachedMySongsLists?> GetCachedListsAsync() =>
        localStorage.GetItemAsync<CachedMySongsLists?>(CachedListsKey).AsTask();

    public Task SaveCachedListsAsync(CachedMySongsLists cache) =>
        localStorage.SetItemAsync(CachedListsKey, cache).AsTask();

    public Task ClearCatalogCacheAsync() =>
        localStorage.RemoveItemAsync(CachedListsKey).AsTask();

    private static bool IsAllowedSortBy(string? value) =>
        value is not null && AllowedSortBy.Contains(value);

    private static bool IsAllowedSortDir(string? value) =>
        value is not null && AllowedSortDir.Contains(value);
}
