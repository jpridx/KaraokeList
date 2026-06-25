using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class PendingPerformanceSyncServiceTests
{
    [Fact]
    public async Task TrySyncAsync_syncs_all_pending_items_on_success()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var first = CreatePendingEntry();
        var second = CreatePendingEntry();
        await store.EnqueuePendingPerformanceAsync(first);
        await store.EnqueuePendingPerformanceAsync(second);

        var api = new StubApiClient(success: true);
        var sync = new PendingPerformanceSyncService(store, api);

        var result = await sync.TrySyncAsync();

        Assert.Equal(2, result.SyncedCount);
        Assert.Equal(0, result.RemainingCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(await store.GetPendingPerformancesAsync());
        Assert.Equal(2, api.CreateCalls);
    }

    [Fact]
    public async Task TrySyncAsync_stops_on_transient_failure_and_leaves_remaining_queue()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.EnqueuePendingPerformanceAsync(CreatePendingEntry());
        await store.EnqueuePendingPerformanceAsync(CreatePendingEntry());

        var api = new StubApiClient(success: false, isTransient: true);
        var sync = new PendingPerformanceSyncService(store, api);

        var result = await sync.TrySyncAsync();

        Assert.Equal(0, result.SyncedCount);
        Assert.Equal(2, result.RemainingCount);
        Assert.Equal(1, api.CreateCalls);
    }

    [Fact]
    public async Task TrySyncAsync_removes_permanent_failures_from_queue()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await store.EnqueuePendingPerformanceAsync(CreatePendingEntry());

        var api = new StubApiClient(success: false, isTransient: false, errorMessage: "Invalid song.");
        var sync = new PendingPerformanceSyncService(store, api);

        var result = await sync.TrySyncAsync();

        Assert.Equal(0, result.SyncedCount);
        Assert.Equal(0, result.RemainingCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("Invalid song.", result.LastError);
        Assert.Empty(await store.GetPendingPerformancesAsync());
    }

    private static PendingPerformanceEntry CreatePendingEntry() =>
        new(
            Guid.NewGuid(),
            SingerId: 1,
            SongId: 42,
            VenueId: 3,
            PerformedOn: DateTime.Today,
            KeyChangeSemitones: null,
            Title: "Sweet Caroline",
            ArtistName: "Neil Diamond",
            VenueName: "Side Room",
            QueuedAt: DateTime.Now);

    private sealed class StubApiClient(bool success, bool isTransient = false, string? errorMessage = null)
        : IKaraokeApiClient
    {
        public int CreateCalls { get; private set; }

        public Task<PerformanceCreateResult> TryCreatePerformanceAsync(PerformanceDto dto)
        {
            CreateCalls++;
            return Task.FromResult(new PerformanceCreateResult(success, isTransient, errorMessage));
        }

        public Task CreatePerformanceAsync(PerformanceDto dto) =>
            throw new NotSupportedException();

        public Task<AuthResult> LoginAsync(LoginRequest request) => throw new NotSupportedException();
        public Task<AuthResult> RegisterAsync(RegisterRequest request) => throw new NotSupportedException();
        public Task<RegistrationInfoDto?> GetRegistrationInfoAsync() => throw new NotSupportedException();
        public Task<List<VenueDto>> GetVenuesAsync() => throw new NotSupportedException();
        public Task CreateVenueAsync(VenueDto dto) => throw new NotSupportedException();
        public Task UpdateVenueAsync(VenueDto dto) => throw new NotSupportedException();
        public Task DeleteVenueAsync(int id) => throw new NotSupportedException();
        public Task<List<GenreDto>> GetGenresAsync() => throw new NotSupportedException();
        public Task CreateGenreAsync(GenreDto dto) => throw new NotSupportedException();
        public Task UpdateGenreAsync(GenreDto dto) => throw new NotSupportedException();
        public Task DeleteGenreAsync(int id) => throw new NotSupportedException();
        public Task<List<ArtistDto>> GetArtistsAsync() => throw new NotSupportedException();
        public Task<List<ArtistLookupDto>> GetArtistLookupsAsync() => throw new NotSupportedException();
        public Task CreateArtistAsync(ArtistDto dto) => throw new NotSupportedException();
        public Task UpdateArtistAsync(ArtistDto dto) => throw new NotSupportedException();
        public Task DeleteArtistAsync(int id) => throw new NotSupportedException();
        public Task<List<SingerDto>> GetSingersAsync() => throw new NotSupportedException();
        public Task CreateSingerAsync(SingerDto dto) => throw new NotSupportedException();
        public Task UpdateSingerAsync(SingerDto dto) => throw new NotSupportedException();
        public Task DeleteSingerAsync(int id) => throw new NotSupportedException();
        public Task<List<SongDto>> GetSongsAsync() => throw new NotSupportedException();
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
        public Task<RepertoireResult> GetMyRepertoireAsync(string sortBy = "lastPerformed", string sortDir = "desc", int? genreId = null, bool includeAll = false) => throw new NotSupportedException();
        public Task<RepertoireGenresResult> GetMyRepertoireGenresAsync() => throw new NotSupportedException();
        public Task<SingerListsResult> GetMyListsAsync() => throw new NotSupportedException();
        public Task<RepertoireResult> GetListSongsAsync(
            int listId,
            string sortBy = "title",
            string sortDir = "asc",
            int? genreId = null) => throw new NotSupportedException();
        public Task<SingerListImportResult> ImportListSongsAsync(ImportSingerListSongsRequest request) =>
            throw new NotSupportedException();
        public Task<ListSongActionResult> AddListSongAsync(int listId, int songId) => throw new NotSupportedException();
        public Task<ListSongActionResult> RemoveListSongAsync(int listId, int songId) => throw new NotSupportedException();
        public Task<SongListMembershipResult> GetSongListMembershipAsync(int songId) => throw new NotSupportedException();
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
