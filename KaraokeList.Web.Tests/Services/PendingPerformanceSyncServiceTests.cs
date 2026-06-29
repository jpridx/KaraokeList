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
        : NotImplementedApiClient
    {
        public int CreateCalls { get; private set; }

        public override Task<PerformanceCreateResult> TryCreatePerformanceAsync(PerformanceDto dto)
        {
            CreateCalls++;
            return Task.FromResult(new PerformanceCreateResult(success, isTransient, errorMessage));
        }
    }
}
