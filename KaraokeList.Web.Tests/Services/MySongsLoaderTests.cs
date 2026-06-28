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

    private sealed class ListsApiStub : IKaraokeApiClient
    {
        public bool ThrowOffline { get; init; }

        private void ThrowIfOffline()
        {
            if (ThrowOffline)
            {
                throw new HttpRequestException("offline");
            }
        }

        public Task<SingerListsResult> GetMyListsAsync()
        {
            ThrowIfOffline();
            return Task.FromResult(SingerListsResult.Ok(
            [
                new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" },
                new SingerListDto { Id = 2, Kind = SingerListKind.WantToSing, DisplayName = "Want to sing" },
                new SingerListDto { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
            ]));
        }

        public Task<RepertoireResult> GetListSongsAsync(
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

        public Task<RepertoireResult> GetMyRepertoireAsync(
            string sortBy = "lastPerformed",
            string sortDir = "desc",
            int? genreId = null,
            bool includeAll = false) => throw new NotSupportedException();
        public Task<SingerListImportResult> ImportListSongsAsync(ImportSingerListSongsRequest request) => throw new NotSupportedException();
        public Task<ListSongActionResult> AddListSongAsync(int listId, int songId) => throw new NotSupportedException();
        public Task<ListSongActionResult> RemoveListSongAsync(int listId, int songId) => throw new NotSupportedException();
        public Task<SongListMembershipResult> GetSongListMembershipAsync(int songId) => throw new NotSupportedException();
        public Task<SongTicklerExclusionResult> GetSongTicklerExclusionAsync(int songId) => throw new NotSupportedException();
        public Task<TicklerExclusionActionResult> SetSongTicklerExclusionAsync(int songId, UpdateSongTicklerExclusionRequest request) => throw new NotSupportedException();
        public Task<TicklerExclusionActionResult> RemoveSongTicklerExclusionAsync(int songId) => throw new NotSupportedException();
        public Task<List<SongDto>> GetSongsAsync() => throw new NotSupportedException();
        public Task<List<ArtistLookupDto>> GetArtistLookupsAsync() => throw new NotSupportedException();
        public Task<List<VenueDto>> GetVenuesAsync() => throw new NotSupportedException();
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
        public Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request) => throw new NotSupportedException();
        public Task<PasswordRecoveryResult> ForgotPasswordAsync(ForgotPasswordRequest request) => throw new NotSupportedException();
        public Task<PasswordRecoveryResult> ResetPasswordAsync(ResetPasswordRequest request) => throw new NotSupportedException();
        public Task<SongSummaryResult> GetMySongSummaryAsync(int songId) => throw new NotSupportedException();
        public Task<RepertoireGenresResult> GetMyRepertoireGenresAsync() => throw new NotSupportedException();
        public Task<StaleSongsResult> GetMyStaleSongsAsync(int? days = null, int? limit = null) => throw new NotSupportedException();
        public Task<TicklerSettingsResult> GetTicklerSettingsAsync() => throw new NotSupportedException();
        public Task<TicklerSettingsUpdateResult> UpdateTicklerSettingsAsync(UpdateTicklerSettingsRequest request) => throw new NotSupportedException();
        public Task<SingerStatsResult> GetMySingerStatsAsync(
            int topVenues = 0,
            int topSongs = 0,
            int topArtists = 0,
            int newRepertoireDays = 0) => throw new NotSupportedException();
        public Task<MyPerformancesResult> GetMyPerformancesAsync(int? venueId = null, string sortDir = "desc") => throw new NotSupportedException();
        public Task UpdatePerformanceAsync(PerformanceDto dto) => throw new NotSupportedException();
        public Task DeletePerformanceAsync(int id) => throw new NotSupportedException();
        public Task<List<AdminUserDto>> GetAdminUsersAsync() => throw new NotSupportedException();
        public Task<AdminUserUpdateResult> UpdateAdminUserAsync(UpdateAdminUserRequest request) => throw new NotSupportedException();
    }
}
