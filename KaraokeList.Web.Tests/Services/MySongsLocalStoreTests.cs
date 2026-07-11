using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class MySongsLocalStoreTests
{
    private static MySongsLocalStore CreateStore() =>
        new(new InMemoryLocalStorage());

    [Fact]
    public async Task GetSortByAsync_returns_default_when_not_stored()
    {
        var store = CreateStore();

        Assert.Equal("lastPerformed", await store.GetSortByAsync());
    }

    [Fact]
    public async Task GetSortDirAsync_returns_default_when_not_stored()
    {
        var store = CreateStore();

        Assert.Equal("desc", await store.GetSortDirAsync());
    }

    [Fact]
    public async Task SetSortPreferenceAsync_persists_sort_field_and_direction()
    {
        var store = CreateStore();

        await store.SetSortPreferenceAsync("title", "asc");

        Assert.Equal("title", await store.GetSortByAsync());
        Assert.Equal("asc", await store.GetSortDirAsync());
    }

    [Fact]
    public async Task GetSortByAsync_falls_back_to_default_for_invalid_value()
    {
        var localStorage = new InMemoryLocalStorage();
        await localStorage.SetItemAsync("karaoke.mySongs.sortBy", "not-a-sort");
        var store = new MySongsLocalStore(localStorage);

        Assert.Equal("lastPerformed", await store.GetSortByAsync());
    }

    [Fact]
    public async Task GetSortDirAsync_falls_back_to_default_for_invalid_value()
    {
        var localStorage = new InMemoryLocalStorage();
        await localStorage.SetItemAsync("karaoke.mySongs.sortDir", "sideways");
        var store = new MySongsLocalStore(localStorage);

        Assert.Equal("desc", await store.GetSortDirAsync());
    }

    [Fact]
    public async Task SetSortPreferenceAsync_ignores_invalid_values()
    {
        var store = CreateStore();

        await store.SetSortPreferenceAsync("invalid", "sideways");

        Assert.Equal("lastPerformed", await store.GetSortByAsync());
        Assert.Equal("desc", await store.GetSortDirAsync());
    }

    [Fact]
    public async Task GetShowDetailedGenreFiltersAsync_returns_default_when_not_stored()
    {
        var store = CreateStore();

        Assert.False(await store.GetShowDetailedGenreFiltersAsync());
    }

    [Fact]
    public async Task SetShowDetailedGenreFiltersAsync_persists_preference()
    {
        var store = CreateStore();

        await store.SetShowDetailedGenreFiltersAsync(true);

        Assert.True(await store.GetShowDetailedGenreFiltersAsync());
    }

    [Fact]
    public async Task GetSearchTextAsync_returns_empty_when_not_stored()
    {
        var store = CreateStore();

        Assert.Equal(string.Empty, await store.GetSearchTextAsync());
    }

    [Fact]
    public async Task GetFilterGenreIdAsync_returns_null_when_not_stored()
    {
        var store = CreateStore();

        Assert.Null(await store.GetFilterGenreIdAsync());
    }

    [Fact]
    public async Task GetFilterGroupNameAsync_returns_null_when_not_stored()
    {
        var store = CreateStore();

        Assert.Null(await store.GetFilterGroupNameAsync());
    }

    [Fact]
    public async Task GetGroupByGenreAsync_returns_false_when_not_stored()
    {
        var store = CreateStore();

        Assert.False(await store.GetGroupByGenreAsync());
    }

    [Fact]
    public async Task SetFilterStateAsync_persists_search_genre_filter_and_group_by()
    {
        var store = CreateStore();

        await store.SetFilterStateAsync("beatles", 42, null, groupByGenre: true);

        Assert.Equal("beatles", await store.GetSearchTextAsync());
        Assert.Equal(42, await store.GetFilterGenreIdAsync());
        Assert.Null(await store.GetFilterGroupNameAsync());
        Assert.True(await store.GetGroupByGenreAsync());
    }

    [Fact]
    public async Task SetFilterStateAsync_persists_group_name_filter()
    {
        var store = CreateStore();

        await store.SetFilterStateAsync(string.Empty, null, "Rock", groupByGenre: false);

        Assert.Equal(string.Empty, await store.GetSearchTextAsync());
        Assert.Null(await store.GetFilterGenreIdAsync());
        Assert.Equal("Rock", await store.GetFilterGroupNameAsync());
        Assert.False(await store.GetGroupByGenreAsync());
    }
}
