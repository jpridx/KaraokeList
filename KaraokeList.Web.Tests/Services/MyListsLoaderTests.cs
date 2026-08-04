using System.Reflection;
using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using NullVersion = KaraokeList.Web.Tests.TestDoubles.NullCatalogVersionService;

namespace KaraokeList.Web.Tests.Services;

public sealed class MyListsLoaderTests
{
    private static MyListsLoader CreateLoader(
        IKaraokeApiClient api,
        MySongsLocalStore mySongsStore,
        LogPerformanceLocalStore logStore,
        ICatalogVersionService? versionService = null,
        ITicklerSettingsLocalStore? ticklerSettingsStore = null) =>
        new(
            api,
            mySongsStore,
            logStore,
            versionService ?? new NullVersion(),
            ticklerSettingsStore ?? new TicklerSettingsLocalStore(new InMemoryLocalStorage()));

    [Fact]
    public async Task LoadAsync_populates_both_my_songs_and_log_catalog_list_fields()
    {
        var mySongsStore = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await logStore.SaveCachedCatalogAsync(new CachedLogCatalog(
            [new CachedSongEntry(1, "Jeopardy", "The Greg Kihn Band")],
            [],
            [],
            DateTime.UtcNow.AddHours(-1)));

        var api = new ListsApiStub();
        var loader = CreateLoader(api, mySongsStore, logStore);

        var bundle = await loader.LoadAsync();

        Assert.True(bundle.Succeeded);
        Assert.False(bundle.FromCache);
        Assert.Equal(3, bundle.Lists.Count);
        Assert.True(bundle.SongsByKind.ContainsKey(SingerListKind.MyRepertoire));

        var mySongsCache = await mySongsStore.GetCachedListsAsync();
        Assert.NotNull(mySongsCache);
        Assert.Equal(3, mySongsCache!.ListsSongs.Count);

        var logCache = await logStore.GetCachedCatalogAsync();
        Assert.NotNull(logCache);
        Assert.Single(logCache!.RepertoireSongIds);
        Assert.Equal(2, logCache.WorkingUpSongIds?.Count);
    }

    [Fact]
    public async Task LoadAsync_preserves_log_catalog_cached_at_when_syncing_list_fields()
    {
        var mySongsStore = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var catalogCachedAt = DateTime.UtcNow.AddHours(-6);
        await logStore.SaveCachedCatalogAsync(new CachedLogCatalog(
            [new CachedSongEntry(1, "Jeopardy", "The Greg Kihn Band")],
            [],
            [],
            catalogCachedAt));

        var loader = CreateLoader(new ListsApiStub(), mySongsStore, logStore);

        _ = await loader.LoadAsync();

        var logCache = await logStore.GetCachedCatalogAsync();
        Assert.NotNull(logCache);
        Assert.Equal(catalogCachedAt, logCache!.CachedAtUtc);
    }

    [Fact]
    public async Task LoadAsync_clears_in_flight_task_after_completion()
    {
        var loader = CreateLoader(
            new ListsApiStub(),
            new MySongsLocalStore(new InMemoryLocalStorage()),
            new LogPerformanceLocalStore(new InMemoryLocalStorage()));

        await loader.LoadAsync();

        var inFlight = typeof(MyListsLoader)
            .GetField("inFlightLoad", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(loader);
        Assert.Null(inFlight);
    }

    [Fact]
    public async Task LoadAsync_when_cache_fresh_skips_api()
    {
        var mySongsStore = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var api = new CountingListsApiStub();
        var loader = CreateLoader(api, mySongsStore, logStore);

        var first = await loader.LoadAsync();
        Assert.False(first.FromCache);
        Assert.False(await loader.NeedsRefreshAsync());
        Assert.NotNull(await loader.TryGetCachedAsync());
        api.ResetCounts();

        var second = await loader.LoadAsync();

        Assert.False(second.FromCache);
        Assert.Equal(0, api.MyListsCallCount);
        Assert.Equal(0, api.ListSongsCallCount);
    }

    [Fact]
    public async Task LoadAsync_concurrent_calls_share_one_in_flight_fetch()
    {
        var mySongsStore = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var api = new GatedListsApiStub();
        var loader = CreateLoader(api, mySongsStore, logStore);

        var first = loader.LoadAsync(forceRefresh: true);
        var second = loader.LoadAsync(forceRefresh: true);
        await Task.Delay(50);
        api.ReleaseGate();
        await Task.WhenAll(first, second);

        Assert.Equal(1, api.MyListsCallCount);
        Assert.Equal(3, api.ListSongsCallCount);
    }

    [Fact]
    public async Task LoadAsync_concurrent_calls_without_force_refresh_share_one_fetch()
    {
        var mySongsStore = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var api = new GatedListsApiStub();
        var loader = CreateLoader(api, mySongsStore, logStore);

        var first = loader.LoadAsync();
        var second = loader.LoadAsync();
        await Task.Delay(50);
        api.ReleaseGate();
        await Task.WhenAll(first, second);

        Assert.Equal(1, api.MyListsCallCount);
        Assert.Equal(3, api.ListSongsCallCount);
    }

    [Fact]
    public async Task LoadAsync_caches_tickler_settings_from_api()
    {
        var mySongsStore = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var ticklerSettingsStore = new TicklerSettingsLocalStore(new InMemoryLocalStorage());
        var api = new ListsApiStub
        {
            TicklerSettings = new TicklerSettingsDto { StaleAfterDays = 30, SongLimit = 3 }
        };
        var loader = CreateLoader(api, mySongsStore, logStore, ticklerSettingsStore: ticklerSettingsStore);

        await loader.LoadAsync();

        var cached = await ticklerSettingsStore.GetAsync();
        Assert.Equal(30, cached.StaleAfterDays);
        Assert.Equal(3, cached.SongLimit);
    }

    private class ListsApiStub : NotImplementedApiClient
    {
        public TicklerSettingsDto TicklerSettings { get; init; } = new();

        public override Task<SingerListsResult> GetMyListsAsync() =>
            Task.FromResult(SingerListsResult.Ok(
            [
                new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" },
                new SingerListDto { Id = 2, Kind = SingerListKind.WantToSing, DisplayName = "Want to sing" },
                new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
            ]));

        public override Task<RepertoireResult> GetListSongsAsync(
            int listId,
            string sortBy = "title",
            string sortDir = "asc",
            int? genreId = null)
        {
            var songs = listId switch
            {
                1 => new List<RepertoireSongDto>
                {
                    new() { SongId = 1, Title = "Jeopardy", ArtistName = "The Greg Kihn Band" }
                },
                3 => new List<RepertoireSongDto>
                {
                    new() { SongId = 2, Title = "Zebra", ArtistName = "Artist Z" },
                    new() { SongId = 3, Title = "Alpha", ArtistName = "Artist A" }
                },
                _ => new List<RepertoireSongDto>()
            };

            return Task.FromResult(RepertoireResult.Ok(songs));
        }

        public override Task<List<GenreGroupDto>> GetGenreGroupsAsync() =>
            Task.FromResult(new List<GenreGroupDto>());

        public override Task<TicklerSettingsResult> GetTicklerSettingsAsync() =>
            Task.FromResult(TicklerSettingsResult.Ok(TicklerSettings));
    }

    private class CountingListsApiStub : ListsApiStub
    {
        public int MyListsCallCount { get; private set; }
        public int ListSongsCallCount { get; private set; }

        public void ResetCounts()
        {
            MyListsCallCount = 0;
            ListSongsCallCount = 0;
        }

        public override Task<SingerListsResult> GetMyListsAsync()
        {
            MyListsCallCount++;
            return base.GetMyListsAsync();
        }

        public override Task<RepertoireResult> GetListSongsAsync(
            int listId,
            string sortBy = "title",
            string sortDir = "asc",
            int? genreId = null)
        {
            ListSongsCallCount++;
            return base.GetListSongsAsync(listId, sortBy, sortDir, genreId);
        }
    }

    private class GatedListsApiStub : ListsApiStub
    {
        private readonly TaskCompletionSource gate = new();

        public int MyListsCallCount { get; private set; }
        public int ListSongsCallCount { get; private set; }

        public void ReleaseGate() => gate.TrySetResult();

        public override async Task<SingerListsResult> GetMyListsAsync()
        {
            MyListsCallCount++;
            await gate.Task;
            return await base.GetMyListsAsync();
        }

        public override Task<RepertoireResult> GetListSongsAsync(
            int listId,
            string sortBy = "title",
            string sortDir = "asc",
            int? genreId = null)
        {
            ListSongsCallCount++;
            return base.GetListSongsAsync(listId, sortBy, sortDir, genreId);
        }
    }
}
