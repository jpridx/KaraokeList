using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class LogPerformanceLocalStoreTests
{
    private static LogPerformanceLocalStore CreateStore() =>
        new(new InMemoryLocalStorage());

    [Fact]
    public async Task EnqueuePendingPerformanceAsync_persists_and_lists_entries()
    {
        var store = CreateStore();
        var entry = CreatePendingEntry();

        await store.EnqueuePendingPerformanceAsync(entry);

        var pending = await store.GetPendingPerformancesAsync();
        Assert.Single(pending);
        Assert.Equal(entry.Id, pending[0].Id);
        Assert.Equal(entry.Title, pending[0].Title);
    }

    [Fact]
    public async Task RemovePendingPerformanceAsync_removes_matching_entry()
    {
        var store = CreateStore();
        var first = CreatePendingEntry();
        var second = CreatePendingEntry();
        await store.EnqueuePendingPerformanceAsync(first);
        await store.EnqueuePendingPerformanceAsync(second);

        await store.RemovePendingPerformanceAsync(first.Id);

        var pending = await store.GetPendingPerformancesAsync();
        Assert.Single(pending);
        Assert.Equal(second.Id, pending[0].Id);
    }

    [Fact]
    public async Task SaveCachedCatalogAsync_persists_snapshot()
    {
        var store = CreateStore();
        var catalog = new CachedLogCatalog(
            [new CachedSongEntry(1, "Test Song", "Test Artist")],
            [1],
            [new CachedVenueEntry(2, "Main Stage")],
            DateTime.UtcNow);

        await store.SaveCachedCatalogAsync(catalog);

        var loaded = await store.GetCachedCatalogAsync();
        Assert.NotNull(loaded);
        Assert.Single(loaded.Songs);
        Assert.Equal("Test Song", loaded.Songs[0].Title);
        Assert.Single(loaded.Venues);
    }

    [Fact]
    public async Task SaveFormDefaultsAsync_persists_venue()
    {
        var store = CreateStore();
        await store.SaveFormDefaultsAsync(new LogFormDefaults(7));

        var loaded = await store.GetFormDefaultsAsync();
        Assert.NotNull(loaded);
        Assert.Equal(7, loaded!.VenueId);
    }

    private static PendingPerformanceEntry CreatePendingEntry() =>
        new(
            Guid.NewGuid(),
            SingerId: 1,
            SongId: 42,
            VenueId: 3,
            PerformedOn: DateTime.Today,
            KeyChangeSemitones: -2,
            Title: "Jeopardy",
            ArtistName: "The Greg Kihn Band",
            VenueName: "Main Stage",
            QueuedAt: DateTime.Now);
}
