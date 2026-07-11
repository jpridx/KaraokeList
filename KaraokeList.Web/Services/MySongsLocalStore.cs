using Blazored.LocalStorage;

using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IMySongsLocalStore
{
    Task<bool> GetShowGenreFiltersAsync();
    Task SetShowGenreFiltersAsync(bool show);
    Task<bool> GetShowDetailedGenreFiltersAsync();
    Task SetShowDetailedGenreFiltersAsync(bool show);
    Task<SingerListKind> GetListKindAsync();
    Task SetListKindAsync(SingerListKind kind);
    Task<string> GetSortByAsync();
    Task<string> GetSortDirAsync();
    Task SetSortPreferenceAsync(string sortBy, string sortDir);
    Task<string> GetSearchTextAsync();
    Task<int?> GetFilterGenreIdAsync();
    Task<string?> GetFilterGroupNameAsync();
    Task<bool> GetGroupByGenreAsync();
    Task SetFilterStateAsync(string searchText, int? filterGenreId, string? filterGroupName, bool groupByGenre);
    Task<CachedMySongsLists?> GetCachedListsAsync();
    Task SaveCachedListsAsync(CachedMySongsLists cache);
    Task ClearCatalogCacheAsync();
}

public sealed class MySongsLocalStore(ILocalStorageService localStorage) : IMySongsLocalStore
{
    private const string ShowGenreFiltersKey = "karaoke.mySongs.showGenreFilters";
    private const string ShowDetailedGenreFiltersKey = "karaoke.mySongs.showDetailedGenreFilters";
    private const string ListKindKey = "karaoke.mySongs.listKind";
    private const string SortByKey = "karaoke.mySongs.sortBy";
    private const string SortDirKey = "karaoke.mySongs.sortDir";
    private const string SearchTextKey = "karaoke.mySongs.searchText";
    private const string FilterGenreIdKey = "karaoke.mySongs.filterGenreId";
    private const string FilterGroupNameKey = "karaoke.mySongs.filterGroupName";
    private const string GroupByGenreKey = "karaoke.mySongs.groupByGenre";
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

    public async Task<bool> GetShowDetailedGenreFiltersAsync()
    {
        var value = await localStorage.GetItemAsync<bool?>(ShowDetailedGenreFiltersKey);
        return value ?? false;
    }

    public Task SetShowDetailedGenreFiltersAsync(bool show) =>
        localStorage.SetItemAsync(ShowDetailedGenreFiltersKey, show).AsTask();

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

    public async Task<string> GetSearchTextAsync()
    {
        var value = await localStorage.GetItemAsync<string?>(SearchTextKey);
        return value ?? string.Empty;
    }

    public Task<int?> GetFilterGenreIdAsync() =>
        localStorage.GetItemAsync<int?>(FilterGenreIdKey).AsTask();

    public Task<string?> GetFilterGroupNameAsync() =>
        localStorage.GetItemAsync<string?>(FilterGroupNameKey).AsTask();

    public async Task<bool> GetGroupByGenreAsync()
    {
        var value = await localStorage.GetItemAsync<bool?>(GroupByGenreKey);
        return value ?? false;
    }

    public async Task SetFilterStateAsync(
        string searchText,
        int? filterGenreId,
        string? filterGroupName,
        bool groupByGenre)
    {
        await localStorage.SetItemAsync(SearchTextKey, searchText);
        await localStorage.SetItemAsync(FilterGenreIdKey, filterGenreId);
        await localStorage.SetItemAsync(FilterGroupNameKey, filterGroupName);
        await localStorage.SetItemAsync(GroupByGenreKey, groupByGenre);
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
