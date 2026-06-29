using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class MySongsLoaderTests
{
    [Fact]
    public async Task LoadAsync_when_online_saves_lists_and_returns_sorted_songs()
    {
        var store = new MySongsLocalStore(new InMemoryLocalStorage());
        var api = new ListsApiStub();
        var loader = new MySongsLoader(api, store);

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

        var loader = new MySongsLoader(new ListsApiStub { ThrowOffline = true }, store);
        var result = await loader.LoadAsync(SingerListKind.WorkingUp, "title", "asc", genreId: null);

        Assert.True(result.FromCache);
        Assert.True(result.HasCache);
        Assert.Single(result.Songs);
        Assert.Equal("Bohemian Rhapsody", result.Songs[0].Title);
    }

    [Fact]
    public async Task LoadAsync_when_offline_without_cache_returns_error()
    {
        var loader = new MySongsLoader(
            new ListsApiStub { ThrowOffline = true },
            new MySongsLocalStore(new InMemoryLocalStorage()));

        var result = await loader.LoadAsync(SingerListKind.MyRepertoire, "title", "asc", genreId: null);

        Assert.True(result.FromCache);
        Assert.False(result.HasCache);
        Assert.NotNull(result.ErrorMessage);
    }

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
    }
}
