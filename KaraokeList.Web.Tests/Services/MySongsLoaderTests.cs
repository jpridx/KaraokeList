using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using NullVersion = KaraokeList.Web.Tests.TestDoubles.NullCatalogVersionService;

namespace KaraokeList.Web.Tests.Services;

public sealed class MySongsLoaderTests
{
    private static MySongsLoader CreateLoader(
        IKaraokeApiClient api,
        MySongsLocalStore store,
        LogPerformanceLocalStore? logStore = null,
        ICatalogVersionService? versionService = null)
    {
        var log = logStore ?? new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var myListsLoader = new MyListsLoader(
            api,
            store,
            log,
            versionService ?? new NullVersion(),
            new TicklerSettingsLocalStore(new InMemoryLocalStorage()));
        return new MySongsLoader(myListsLoader, store, log);
    }

    [Fact]
    public async Task LoadAsync_when_online_saves_lists_and_returns_sorted_songs()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var api = new ListsApiStub();
        var loader = CreateLoader(api, store);

        var result = await loader.LoadAsync(SingerListKind.WorkingUp, "title", "asc", genreId: null);

        Assert.False(result.FromCache);
        Assert.True(result.HasCache);
        Assert.Equal(2, result.Songs.Count);
        Assert.Equal("Alpha", result.Songs[0].Title);

        var cached = await store.GetCachedListsAsync();
        Assert.NotNull(cached);
        Assert.Equal(3, cached.ListsSongs.Count);
    }

    [Fact]
    public async Task LoadAsync_when_offline_returns_cached_lists()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }],
            [new CachedListSongsEntry(
                SingerListKind.WorkingUp,
                [new RepertoireSongDto { SongId = 9, Title = "Bohemian Rhapsody", ArtistName = "Queen" }])],
            DateTime.UtcNow.AddHours(-2)));

        var loader = CreateLoader(new ListsApiStub { ThrowOffline = true }, store);
        var result = await loader.LoadAsync(SingerListKind.WorkingUp, "title", "asc", genreId: null);

        Assert.True(result.FromCache);
        Assert.True(result.HasCache);
        Assert.Single(result.Songs);
        Assert.Equal("Bohemian Rhapsody", result.Songs[0].Title);
    }

    [Fact]
    public async Task LoadAsync_when_offline_without_cache_returns_error()
    {
        var loader = CreateLoader(
            new ListsApiStub { ThrowOffline = true },
            new MySongsLocalStore(new InMemoryLocalStorage()));

        var result = await loader.LoadAsync(SingerListKind.MyRepertoire, "title", "asc", genreId: null);

        Assert.True(result.FromCache);
        Assert.False(result.HasCache);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task TryGetCachedAsync_returns_old_schema_cache_instead_of_clearing_it()
    {
        // Old cache (SchemaVersion = 0) must be served rather than cleared so that
        // the fast-path in LoadListsAsync can show content immediately during DB wake-up.
        // NeedsRefreshAsync() returns true for old schema, so a background refresh will
        // fetch up-to-date data (with correct GenreId) once the API is reachable.
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [new RepertoireSongDto { SongId = 5, Title = "Old Song", ArtistName = "Old Artist" }])],
            DateTime.UtcNow,
            CacheTag: null,
            SchemaVersion: 0));

        var loader = CreateLoader(new ListsApiStub(), store);
        var result = await loader.TryGetCachedAsync(SingerListKind.MyRepertoire, "title", "asc", genreId: null);

        Assert.NotNull(result);
        Assert.True(result.FromCache);
        Assert.Single(result.Songs);
        Assert.Equal("Old Song", result.Songs[0].Title);

        // Old cache must still be present in the store (not cleared).
        var rawCache = await store.GetCachedListsAsync();
        Assert.NotNull(rawCache);
        Assert.Equal(0, rawCache.SchemaVersion);
    }

    [Fact]
    public async Task NeedsRefreshAsync_returns_true_for_old_schema_to_trigger_background_refresh()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [new RepertoireSongDto { SongId = 5, Title = "Old Song", ArtistName = "Old Artist" }])],
            DateTime.UtcNow,
            CacheTag: null,
            SchemaVersion: 0));

        var loader = CreateLoader(new ListsApiStub(), store);
        var needsRefresh = await loader.NeedsRefreshAsync();

        Assert.True(needsRefresh);
    }

    [Fact]
    public async Task TryGetCachedAsync_filters_by_group_name()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var genreGroups = CreateSampleGenreGroups();
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [
                    new RepertoireSongDto { SongId = 1, Title = "Rock Song", ArtistName = "A", GenreId = 10, GenreName = "Classic Rock" },
                    new RepertoireSongDto { SongId = 2, Title = "Pop Song", ArtistName = "B", GenreId = 20, GenreName = "Pop Rock" }
                ])],
            DateTime.UtcNow,
            CacheTag: "tag",
            SchemaVersion: 2,
            genreGroups));

        var loader = CreateLoader(new ListsApiStub(), store);
        var result = await loader.TryGetCachedAsync(
            SingerListKind.MyRepertoire,
            "title",
            "asc",
            genreId: null,
            groupName: "Rock");

        Assert.NotNull(result);
        Assert.Single(result.Songs);
        Assert.Equal("Rock Song", result.Songs[0].Title);
        Assert.Equal(["Rock", "Pop"], result.FilterGroups);
    }

    [Fact]
    public async Task TryGetCachedAsync_genre_filter_takes_precedence_over_group()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var genreGroups = CreateSampleGenreGroups();
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [
                    new RepertoireSongDto { SongId = 1, Title = "Rock Song", ArtistName = "A", GenreId = 10, GenreName = "Classic Rock" },
                    new RepertoireSongDto { SongId = 2, Title = "Metal Song", ArtistName = "B", GenreId = 11, GenreName = "Hair Metal" }
                ])],
            DateTime.UtcNow,
            CacheTag: "tag",
            SchemaVersion: 2,
            genreGroups));

        var loader = CreateLoader(new ListsApiStub(), store);
        var result = await loader.TryGetCachedAsync(
            SingerListKind.MyRepertoire,
            "title",
            "asc",
            genreId: 11,
            groupName: "Rock");

        Assert.NotNull(result);
        Assert.Single(result.Songs);
        Assert.Equal("Metal Song", result.Songs[0].Title);
    }

    [Fact]
    public async Task TryGetCachedAsync_with_active_genre_filter_still_returns_filter_genres()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var genreGroups = CreateSampleGenreGroups();
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [
                    new RepertoireSongDto { SongId = 1, Title = "Rock Song", ArtistName = "A", GenreId = 10, GenreName = "Classic Rock" },
                    new RepertoireSongDto { SongId = 2, Title = "Metal Song", ArtistName = "B", GenreId = 11, GenreName = "Hair Metal" },
                    new RepertoireSongDto { SongId = 3, Title = "Pop Song", ArtistName = "C", GenreId = 20, GenreName = "Pop Rock" }
                ])],
            DateTime.UtcNow,
            CacheTag: "tag",
            SchemaVersion: 2,
            genreGroups));

        var loader = CreateLoader(new ListsApiStub(), store);
        var result = await loader.TryGetCachedAsync(
            SingerListKind.MyRepertoire,
            "title",
            "asc",
            genreId: 11,
            groupName: null);

        Assert.NotNull(result);
        Assert.Single(result.Songs);
        Assert.Equal("Metal Song", result.Songs[0].Title);
        Assert.Equal(["Rock", "Pop"], result.FilterGroups);
        Assert.Equal(3, result.FilterGenres.Count);
        Assert.Contains(result.FilterGenres, g => g.Id == 10 && g.GenreName == "Classic Rock");
        Assert.Contains(result.FilterGenres, g => g.Id == 11 && g.GenreName == "Hair Metal");
        Assert.Contains(result.FilterGenres, g => g.Id == 20 && g.GenreName == "Pop Rock");
    }

    [Fact]
    public async Task PatchCachedSongGenreAsync_updates_genre_across_all_cached_lists()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [
                new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" },
                new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
            ],
            [
                new CachedListSongsEntry(
                    SingerListKind.MyRepertoire,
                    [new RepertoireSongDto { SongId = 5, Title = "Old Song", ArtistName = "Old Artist", GenreId = 1, GenreName = "Pop" }]),
                new CachedListSongsEntry(
                    SingerListKind.WorkingUp,
                    [new RepertoireSongDto { SongId = 5, Title = "Old Song", ArtistName = "Old Artist", GenreId = 1, GenreName = "Pop" }])
            ],
            DateTime.UtcNow,
            CacheTag: "tag",
            SchemaVersion: 2));

        var loader = CreateLoader(new ListsApiStub(), store);
        await loader.PatchCachedSongGenreAsync(5, 10, "Classic Rock");

        var cached = await store.GetCachedListsAsync();
        Assert.NotNull(cached);
        Assert.All(cached.ListsSongs, entry =>
        {
            var song = Assert.Single(entry.Songs);
            Assert.Equal(10, song.GenreId);
            Assert.Equal("Classic Rock", song.GenreName);
        });
    }

    [Fact]
    public async Task SetSongPerformanceStatsAsync_updates_exact_stats_without_incrementing()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [
                    new RepertoireSongDto
                    {
                        SongId = 5,
                        Title = "Old Song",
                        ArtistName = "Old Artist",
                        ArtistDisplay = "Old Artist",
                        LastPerformedOn = new DateTime(2025, 1, 1),
                        PerformanceCount = 7
                    }
                ])],
            DateTime.UtcNow));

        var loader = CreateLoader(new ListsApiStub(), store);
        var lastPerformedOn = new DateTime(2026, 8, 1);

        await loader.SetSongPerformanceStatsAsync(
            5,
            "New Song",
            "New Artist",
            "New Artist",
            lastPerformedOn,
            2);

        var cached = await store.GetCachedListsAsync();
        var song = Assert.Single(cached!.ListsSongs.Single().Songs);
        Assert.Equal(2, song.PerformanceCount);
        Assert.Equal(lastPerformedOn.Date, song.LastPerformedOn);
        Assert.Equal("New Song", song.Title);
        Assert.Equal("New Artist", song.ArtistName);
    }

    [Fact]
    public async Task SetSongPerformanceStatsAsync_sets_zero_and_clears_last_performed_on()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            [new CachedListSongsEntry(
                SingerListKind.MyRepertoire,
                [
                    new RepertoireSongDto
                    {
                        SongId = 5,
                        Title = "Old Song",
                        ArtistName = "Old Artist",
                        ArtistDisplay = "Old Artist",
                        LastPerformedOn = new DateTime(2025, 1, 1),
                        PerformanceCount = 3
                    }
                ])],
            DateTime.UtcNow));

        var loader = CreateLoader(new ListsApiStub(), store);

        await loader.SetSongPerformanceStatsAsync(
            5,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            0);

        var cached = await store.GetCachedListsAsync();
        var song = Assert.Single(cached!.ListsSongs.Single().Songs);
        Assert.Equal(0, song.PerformanceCount);
        Assert.Null(song.LastPerformedOn);
        Assert.Equal("Old Song", song.Title);
        Assert.Equal("Old Artist", song.ArtistName);
    }

    [Fact]
    public async Task RemoveSongFromCachedListAsync_removes_from_want_to_sing_cache()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [
                new SingerListDto { Id = 2, Kind = SingerListKind.WantToSing, DisplayName = "Want to sing" },
                new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
            ],
            [
                new CachedListSongsEntry(
                    SingerListKind.WantToSing,
                    [
                        new RepertoireSongDto { SongId = 5, Title = "Want Song", ArtistName = "Artist A" },
                        new RepertoireSongDto { SongId = 6, Title = "Keep Song", ArtistName = "Artist B" }
                    ]),
                new CachedListSongsEntry(
                    SingerListKind.WorkingUp,
                    [new RepertoireSongDto { SongId = 5, Title = "Want Song", ArtistName = "Artist A" }])
            ],
            DateTime.UtcNow));

        var loader = CreateLoader(new ListsApiStub(), store);
        await loader.RemoveSongFromCachedListAsync(SingerListKind.WantToSing, 5);

        var cached = await store.GetCachedListsAsync();
        var wantSongs = cached!.ListsSongs.Single(e => e.Kind == SingerListKind.WantToSing).Songs;
        Assert.Single(wantSongs);
        Assert.Equal(6, wantSongs[0].SongId);

        var workingUpSongs = cached.ListsSongs.Single(e => e.Kind == SingerListKind.WorkingUp).Songs;
        Assert.Single(workingUpSongs);
        Assert.Equal(5, workingUpSongs[0].SongId);
    }

    [Fact]
    public async Task RemoveSongFromCachedListAsync_removes_from_working_up_and_log_cache()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }],
            [new CachedListSongsEntry(
                SingerListKind.WorkingUp,
                [
                    new RepertoireSongDto { SongId = 5, Title = "Working Song", ArtistName = "Artist A" },
                    new RepertoireSongDto { SongId = 6, Title = "Keep Song", ArtistName = "Artist B" }
                ])],
            DateTime.UtcNow));
        await logStore.SaveCachedCatalogAsync(new CachedLogCatalog(
            [new CachedSongEntry(5, "Working Song", "Artist A"), new CachedSongEntry(6, "Keep Song", "Artist B")],
            [],
            [],
            DateTime.UtcNow.AddHours(-1),
            WorkingUpSongIds: [5, 6]));

        var loader = CreateLoader(new ListsApiStub(), store, logStore);
        await loader.RemoveSongFromCachedListAsync(SingerListKind.WorkingUp, 5);

        var cached = await store.GetCachedListsAsync();
        var workingUpSongs = cached!.ListsSongs.Single(e => e.Kind == SingerListKind.WorkingUp).Songs;
        Assert.Single(workingUpSongs);
        Assert.Equal(6, workingUpSongs[0].SongId);

        var logCache = await logStore.GetCachedCatalogAsync();
        Assert.NotNull(logCache);
        Assert.Single(logCache!.WorkingUpSongIds ?? []);
        Assert.Equal(6, logCache.WorkingUpSongIds![0]);
    }

    [Fact]
    public async Task AddSongToCachedListAsync_adds_to_working_up_and_log_cache()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedListsAsync(new CachedMySongsLists(
            [new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }],
            [new CachedListSongsEntry(
                SingerListKind.WorkingUp,
                [new RepertoireSongDto { SongId = 6, Title = "Existing", ArtistName = "Artist B" }])],
            DateTime.UtcNow));
        await logStore.SaveCachedCatalogAsync(new CachedLogCatalog(
            [new CachedSongEntry(7, "New Song", "Artist C")],
            [],
            [],
            DateTime.UtcNow.AddHours(-1),
            WorkingUpSongIds: [6]));

        var loader = CreateLoader(new ListsApiStub(), store, logStore);
        var newSong = new RepertoireSongDto { SongId = 7, Title = "New Song", ArtistName = "Artist C" };
        await loader.AddSongToCachedListAsync(SingerListKind.WorkingUp, newSong);

        var cached = await store.GetCachedListsAsync();
        var workingUpSongs = cached!.ListsSongs.Single(e => e.Kind == SingerListKind.WorkingUp).Songs;
        Assert.Equal(2, workingUpSongs.Count);
        Assert.Contains(workingUpSongs, s => s.SongId == 7);

        var logCache = await logStore.GetCachedCatalogAsync();
        Assert.NotNull(logCache);
        Assert.Equal(2, logCache!.WorkingUpSongIds?.Count);
        Assert.Contains(7, logCache.WorkingUpSongIds ?? []);
    }

    [Fact]
    public async Task RemoveSongFromCachedListAsync_no_op_when_cache_missing()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var loader = CreateLoader(new ListsApiStub(), store, logStore);

        await loader.RemoveSongFromCachedListAsync(SingerListKind.WorkingUp, 5);

        Assert.Null(await store.GetCachedListsAsync());
        Assert.Null(await logStore.GetCachedCatalogAsync());
    }

    private static IReadOnlyList<GenreGroupDto> CreateSampleGenreGroups() =>
    [
        new()
        {
            GroupName = "Rock",
            SortOrder = 1,
            Genres =
            [
                new GenreGroupMemberDto { GenreId = 10, GenreName = "Classic Rock", IsPrimary = true },
                new GenreGroupMemberDto { GenreId = 11, GenreName = "Hair Metal", IsPrimary = true }
            ]
        },
        new()
        {
            GroupName = "Pop",
            SortOrder = 2,
            Genres = [new GenreGroupMemberDto { GenreId = 20, GenreName = "Pop Rock", IsPrimary = true }]
        }
    ];

    private sealed class ListsApiStub : NotImplementedApiClient
    {
        public bool ThrowOffline { get; init; }

        private void ThrowIfOffline()
        {
            if (ThrowOffline)
            {
                throw new HttpRequestException("offline");
            }
        }

        public override Task<SingerListsResult> GetMyListsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(SingerListsResult.Ok(
            [
                new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" },
                new SingerListDto { Id = 2, Kind = SingerListKind.WantToSing, DisplayName = "Want to sing" },
                new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
            ]));
        }

        public override Task<RepertoireResult> GetListSongsAsync(
            int listId,
            string sortBy = "title",
            string sortDir = "asc",
            int? genreId = null)
        {
            ThrowIfOffline();
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

        public override Task<List<GenreGroupDto>> GetGenreGroupsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(new List<GenreGroupDto>());
        }

        public override Task<TicklerSettingsResult> GetTicklerSettingsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(TicklerSettingsResult.Ok(new TicklerSettingsDto()));
        }
    }
}
