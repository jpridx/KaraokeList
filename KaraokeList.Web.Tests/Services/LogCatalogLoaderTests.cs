using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class LogCatalogLoaderTests
{
    [Fact]
    public async Task LoadAsync_when_online_saves_catalog_and_returns_fresh_data()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var api = new CatalogApiStub
        {
            Songs =
            [
                new SongDto { Id = 1, Title = "Jeopardy", Artist = 10 },
                new SongDto { Id = 2, Title = "Sweet Caroline", Artist = 11 }
            ],
            Artists =
            [
                new ArtistLookupDto { Id = 10, Name = "The Greg Kihn Band" },
                new ArtistLookupDto { Id = 11, Name = "Neil Diamond" }
            ],
            Venues = [new VenueDto { Id = 3, VenueName = "Main Stage" }],
            RepertoireSongIds = [1]
        };
        var loader = new LogCatalogLoader(api, store);

        var snapshot = await loader.LoadAsync();

        Assert.False(snapshot.FromCache);
        Assert.True(snapshot.HasCatalog);
        Assert.Equal(2, snapshot.Songs.Count);
        Assert.True(snapshot.Songs.First(s => s.Id == 1).InRepertoire);
        Assert.False(snapshot.Songs.First(s => s.Id == 2).InRepertoire);

        var cached = await store.GetCachedCatalogAsync();
        Assert.NotNull(cached);
        Assert.Equal(2, cached.Songs.Count);
        Assert.Single(cached.Venues);
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

        var loader = new LogCatalogLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var snapshot = await loader.LoadAsync();

        Assert.True(snapshot.FromCache);
        Assert.True(snapshot.HasCatalog);
        Assert.Single(snapshot.Songs);
        Assert.Equal("Bohemian Rhapsody", snapshot.Songs[0].Title);
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

        var loader = new LogCatalogLoader(new CatalogApiStub { ThrowOffline = true }, store);
        var result = await loader.LoadVenuesAsync();

        Assert.True(result.FromCache);
        Assert.Single(result.Venues);
        Assert.Equal("Side Room", result.Venues[0].VenueName);
    }

    private sealed class CatalogApiStub : IKaraokeApiClient
    {
        public bool ThrowOffline { get; init; }
        public List<SongDto> Songs { get; init; } = [];
        public List<ArtistLookupDto> Artists { get; init; } = [];
        public List<VenueDto> Venues { get; init; } = [];
        public HashSet<int> RepertoireSongIds { get; init; } = [];

        private void ThrowIfOffline()
        {
            if (ThrowOffline)
            {
                throw new HttpRequestException("offline");
            }
        }

        public Task<List<SongDto>> GetSongsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Songs);
        }

        public Task<List<ArtistLookupDto>> GetArtistLookupsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Artists);
        }

        public Task<List<VenueDto>> GetVenuesAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(Venues);
        }

        public Task<RepertoireResult> GetMyRepertoireAsync(
            string sortBy = "lastPerformed",
            string sortDir = "desc",
            int? genreId = null,
            bool includeAll = false)
        {
            ThrowIfOffline();
            return Task.FromResult(RepertoireResult.Ok(
                RepertoireSongIds.Select(id => new RepertoireSongDto { SongId = id }).ToList()));
        }

        public Task<PerformanceCreateResult> TryCreatePerformanceAsync(PerformanceDto dto) => throw new NotSupportedException();
        public Task CreatePerformanceAsync(PerformanceDto dto) => throw new NotSupportedException();
        public Task<AuthResult> LoginAsync(LoginRequest request) => throw new NotSupportedException();
        public Task<AuthResult> RegisterAsync(RegisterRequest request) => throw new NotSupportedException();
        public Task<RegistrationInfoDto?> GetRegistrationInfoAsync() => throw new NotSupportedException();
        public Task CreateVenueAsync(VenueDto dto) => throw new NotSupportedException();
        public Task UpdateVenueAsync(VenueDto dto) => throw new NotSupportedException();
        public Task DeleteVenueAsync(int id) => throw new NotSupportedException();
        public Task<List<GenreDto>> GetGenresAsync() => throw new NotSupportedException();
        public Task CreateGenreAsync(GenreDto dto) => throw new NotSupportedException();
        public Task UpdateGenreAsync(GenreDto dto) => throw new NotSupportedException();
        public Task DeleteGenreAsync(int id) => throw new NotSupportedException();
        public Task<List<ArtistDto>> GetArtistsAsync() => throw new NotSupportedException();
        public Task CreateArtistAsync(ArtistDto dto) => throw new NotSupportedException();
        public Task UpdateArtistAsync(ArtistDto dto) => throw new NotSupportedException();
        public Task DeleteArtistAsync(int id) => throw new NotSupportedException();
        public Task<List<SingerDto>> GetSingersAsync() => throw new NotSupportedException();
        public Task CreateSingerAsync(SingerDto dto) => throw new NotSupportedException();
        public Task UpdateSingerAsync(SingerDto dto) => throw new NotSupportedException();
        public Task DeleteSingerAsync(int id) => throw new NotSupportedException();
        public Task CreateSongAsync(SongDto dto) => throw new NotSupportedException();
        public Task UpdateSongAsync(SongDto dto) => throw new NotSupportedException();
        public Task DeleteSongAsync(int id) => throw new NotSupportedException();
        public Task<List<PerformanceDto>> GetPerformancesAsync(int? songId = null) => throw new NotSupportedException();
        public Task<UserProfileDto?> GetProfileAsync() => throw new NotSupportedException();
        public Task<InviteShareDto?> GetInviteShareAsync() => throw new NotSupportedException();
        public Task<AuthResult> LinkSingerAsync(LinkSingerRequest request) => throw new NotSupportedException();
        public Task<SongSummaryResult> GetMySongSummaryAsync(int songId) => throw new NotSupportedException();
        public Task<RepertoireGenresResult> GetMyRepertoireGenresAsync() => throw new NotSupportedException();
        public Task<MyPerformancesResult> GetMyPerformancesAsync(int? venueId = null, string sortDir = "desc") => throw new NotSupportedException();
        public Task UpdatePerformanceAsync(PerformanceDto dto) => throw new NotSupportedException();
        public Task DeletePerformanceAsync(int id) => throw new NotSupportedException();
        public Task<List<AdminUserDto>> GetAdminUsersAsync() => throw new NotSupportedException();
        public Task<AdminUserUpdateResult> UpdateAdminUserAsync(UpdateAdminUserRequest request) => throw new NotSupportedException();
    }
}
