using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using NullVersion = KaraokeList.Web.Tests.TestDoubles.NullCatalogVersionService;

namespace KaraokeList.Web.Tests.Services;

public sealed class LogCatalogLoaderTests
{
    private static LogCatalogLoader CreateLoader(
        IKaraokeApiClient api,
        LogPerformanceLocalStore store,
        ICatalogVersionService? versionService = null,
        ITicklerExclusionsLocalStore? exclusionsStore = null) =>
        new(
            api,
            store,
            versionService ?? new NullVersion(),
            exclusionsStore ?? new TicklerExclusionsLocalStore(new InMemoryLocalStorage()));

    [Fact]
    public async Task LoadAsync_when_online_saves_catalog_and_returns_fresh_data()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var api = new CatalogApiStub
        {
            Songs =
            [
                new SongDto
                {
                    Id = 1,
                    Title = "Jeopardy",
                    Artists = [new SongArtistDto { ArtistId = 10, DisplayOrder = 0, Name = "The Greg Kihn Band" }]
                },
                new SongDto
                {
                    Id = 2,
                    Title = "Sweet Caroline",
                    Artists = [new SongArtistDto { ArtistId = 11, DisplayOrder = 0, Name = "Neil Diamond" }]
                }
            ],
            Artists =
            [
                new ArtistLookupDto { Id = 10, Name = "The Greg Kihn Band" },
                new ArtistLookupDto { Id = 11, Name = "Neil Diamond" }
            ],
            Genres =
            [
                new GenreDto { Id = 1, GenreName = "Rock" },
                new GenreDto { Id = 2, GenreName = "Pop" }
            ],
            Venues = [new VenueDto { Id = 3, VenueName = "Main Stage" }],
            RepertoireSongIds = [1],
            WorkingUpSongIds = [2]
        };
        var loader = CreateLoader(api, store);

        var snapshot = await loader.LoadAsync();

        Assert.False(snapshot.FromCache);
        Assert.True(snapshot.HasCatalog);
        Assert.Equal(2, snapshot.Songs.Count);
        Assert.True(snapshot.Songs.First(s => s.Id == 1).InRepertoire);
        Assert.False(snapshot.Songs.First(s => s.Id == 2).InRepertoire);
        Assert.True(snapshot.Songs.First(s => s.Id == 2).InWorkingUp);

        var cached = await store.GetCachedCatalogAsync();
        Assert.NotNull(cached);
        Assert.Equal(2, cached.Songs.Count);
        Assert.Single(cached.Venues);
        Assert.Single(cached.WorkingUpSongIds ?? []);
        Assert.Equal(2, cached.Artists?.Count);
        Assert.Equal(2, cached.Genres?.Count);
    }

    [Fact]
    public async Task LoadLookupsAsync_when_online_saves_and_returns_fresh_lookups()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var api = new CatalogApiStub
        {
            Artists = [new ArtistLookupDto { Id = 10, Name = "Queen" }],
            Genres = [new GenreDto { Id = 5, GenreName = "Rock" }]
        };
        var loader = CreateLoader(api, store);

        var result = await loader.LoadLookupsAsync();

        Assert.False(result.FromCache);
        Assert.Single(result.Artists);
        Assert.Single(result.Genres);

        var cached = await store.GetCachedCatalogAsync();
        Assert.NotNull(cached);
        Assert.Single(cached!.Artists!);
        Assert.Single(cached.Genres!);
    }

    [Fact]
    public async Task TryGetCachedLookupsAsync_returns_cached_artists_and_genres()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedCatalogAsync(new CachedLogCatalog(
            [],
            [],
            [],
            DateTime.UtcNow,
            [],
            Artists: [new CachedArtistEntry(10, "Queen")],
            Genres: [new CachedGenreEntry(5, "Rock")]));

        var loader = CreateLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var result = await loader.TryGetCachedLookupsAsync();

        Assert.NotNull(result);
        Assert.True(result!.FromCache);
        Assert.Equal("Queen", result.Artists[0].Name);
        Assert.Equal("Rock", result.Genres[0].GenreName);
    }

    [Fact]
    public async Task LoadLookupsAsync_when_offline_returns_cached_lookups()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedCatalogAsync(new CachedLogCatalog(
            [],
            [],
            [],
            DateTime.UtcNow,
            [],
            Artists: [new CachedArtistEntry(10, "Queen")],
            Genres: [new CachedGenreEntry(5, "Rock")]));

        var loader = CreateLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var result = await loader.LoadLookupsAsync();

        Assert.True(result.FromCache);
        Assert.Equal("Queen", result.Artists[0].Name);
        Assert.Equal("Rock", result.Genres[0].GenreName);
    }

    [Fact]
    public async Task LoadAsync_when_offline_returns_cached_catalog_with_working_up_badges()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedCatalogAsync(new CachedLogCatalog(
            [
                new CachedSongEntry(5, "Bohemian Rhapsody", "Queen"),
                new CachedSongEntry(6, "Jeopardy", "The Greg Kihn Band")
            ],
            [6],
            [new CachedVenueEntry(9, "Side Room")],
            DateTime.UtcNow.AddHours(-1),
            [5]));

        var loader = CreateLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var snapshot = await loader.LoadAsync();

        Assert.True(snapshot.FromCache);
        Assert.True(snapshot.HasCatalog);
        Assert.Equal(2, snapshot.Songs.Count);
        Assert.True(snapshot.Songs.First(s => s.Id == 5).InWorkingUp);
        Assert.Contains(5, snapshot.WorkingUpSongIds);
    }

    [Fact]
    public async Task LoadAsync_when_offline_returns_cached_catalog()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedCatalogAsync(new CachedLogCatalog(
            [new CachedSongEntry(5, "Bohemian Rhapsody", "Queen")],
            [5],
            [new CachedVenueEntry(9, "Side Room")],
            DateTime.UtcNow.AddHours(-1)));

        var loader = CreateLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var snapshot = await loader.LoadAsync();

        Assert.True(snapshot.FromCache);
        Assert.True(snapshot.HasCatalog);
        Assert.Single(snapshot.Songs);
        Assert.Equal("Bohemian Rhapsody", snapshot.Songs[0].Title);
    }

    [Fact]
    public async Task PatchCachedSongAsync_appends_song_and_updates_cache_tag()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedCatalogAsync(new CachedLogCatalog(
            [new CachedSongEntry(1, "Old Song", "Old Artist")],
            [1],
            [new CachedVenueEntry(3, "Main Stage")],
            DateTime.UtcNow.AddHours(-1),
            [],
            "old-tag"));

        var versionService = new FixedCatalogVersionService("new-tag");
        var loader = CreateLoader(new CatalogApiStub(), store, versionService);

        var snapshot = await loader.PatchCachedSongAsync(99, "Brand New", "New Artist");

        Assert.True(snapshot.FromCache);
        Assert.True(snapshot.HasCatalog);
        Assert.Contains(snapshot.Songs, s => s.Id == 99 && s.Title == "Brand New");

        var cached = await store.GetCachedCatalogAsync();
        Assert.NotNull(cached);
        Assert.Equal(2, cached!.Songs.Count);
        Assert.Equal("new-tag", cached.CacheTag);
    }

    private sealed class FixedCatalogVersionService(string tag) : ICatalogVersionService
    {
        public Task<string?> GetCacheTagAsync(bool forceRefresh = false) => Task.FromResult<string?>(tag);
        public void Invalidate() { }
    }

    [Fact]
    public async Task LoadVenuesAsync_when_offline_returns_cached_venues()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedCatalogAsync(new CachedLogCatalog(
            [],
            [],
            [new CachedVenueEntry(9, "Side Room")],
            DateTime.UtcNow));

        var loader = CreateLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var result = await loader.LoadVenuesAsync();

        Assert.True(result.FromCache);
        Assert.Single(result.Venues);
        Assert.Equal("Side Room", result.Venues[0].VenueName);
    }

    private sealed class CatalogApiStub : NotImplementedApiClient
    {
        public bool ThrowOffline { get; init; }
        public List<SongDto> Songs { get; init; } = [];
        public List<ArtistLookupDto> Artists { get; init; } = [];
        public List<GenreDto> Genres { get; init; } = [];
        public List<VenueDto> Venues { get; init; } = [];
        public HashSet<int> RepertoireSongIds { get; init; } = [];
        public HashSet<int> WorkingUpSongIds { get; init; } = [];

        private void ThrowIfOffline()
        {
            if (ThrowOffline)
            {
                throw new HttpRequestException("offline");
            }
        }

        public override Task<List<SongDto>> GetSongsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Songs);
        }

        public override Task<List<ArtistLookupDto>> GetArtistLookupsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Artists);
        }

        public override Task<List<GenreDto>> GetGenresAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Genres);
        }

        public override Task<List<VenueDto>> GetVenuesAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Venues);
        }

        public override Task<SingerListsResult> GetMyListsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(SingerListsResult.Ok(
            [
                new SingerListDto
                {
                    Id = 1,
                    Kind = SingerListKind.MyRepertoire,
                    DisplayName = SingerListKindNames.DisplayName(SingerListKind.MyRepertoire)
                },
                new SingerListDto
                {
                    Id = 3,
                    Kind = SingerListKind.WorkingUp,
                    DisplayName = SingerListKindNames.DisplayName(SingerListKind.WorkingUp)
                }
            ]));
        }

        public override Task<RepertoireResult> GetListSongsAsync(
            int listId,
            string sortBy = "title",
            string sortDir = "asc",
            int? genreId = null)
        {
            ThrowIfOffline();
            if (listId != 1 && listId != 3)
            {
                return Task.FromResult(RepertoireResult.Ok([]));
            }

            if (listId == 3)
            {
                return Task.FromResult(RepertoireResult.Ok(
                    WorkingUpSongIds.Select(id => new RepertoireSongDto { SongId = id }).ToList()));
            }

            return Task.FromResult(RepertoireResult.Ok(
                RepertoireSongIds.Select(id => new RepertoireSongDto { SongId = id }).ToList()));
        }

        public override Task<TicklerExclusionsResult> GetMyTicklerExclusionsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(TicklerExclusionsResult.Ok([]));
        }
    }
}
