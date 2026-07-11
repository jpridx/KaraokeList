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
}
