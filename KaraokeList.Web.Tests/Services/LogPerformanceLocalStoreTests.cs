using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class LogPerformanceLocalStoreTests
{
    [Fact]
    public async Task ReplaceRecentLogsIfBaselineAsync_skips_write_when_a_newer_local_log_arrives()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var baselineLoggedAt = new DateTime(2026, 6, 11);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            99, "Stale Song", "Artist", "Old Venue", baselineLoggedAt, null, baselineLoggedAt));

        var newerLoggedAt = baselineLoggedAt.AddHours(2);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            2, "Just Logged", "Artist", "Main Stage", newerLoggedAt.Date, null, newerLoggedAt));

        var mergedFromServer = new List<RecentLoggedPerformance>
        {
            new(1, "Server Song", "Artist", "Main Stage", new DateTime(2026, 7, 9), null, new DateTime(2026, 7, 9))
        };

        var saved = await store.ReplaceRecentLogsIfBaselineAsync(mergedFromServer, baselineLoggedAt);

        Assert.Equal("Just Logged", saved[0].Title);
        Assert.DoesNotContain(saved, log => log.Title == "Server Song");
    }

    [Fact]
    public async Task PatchRecentLogAsync_updates_matching_entry_and_preserves_loggedAt()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var performedOn = new DateTime(2026, 8, 2);
        var loggedAt = new DateTime(2026, 8, 2, 20, 0, 0);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            1, "Song", "Artist", "Old Venue", performedOn, null, loggedAt));

        await store.PatchRecentLogAsync(
            1,
            performedOn,
            new RecentLoggedPerformance(
                1, "Song", "Artist", "New Venue", performedOn, -2, loggedAt));

        var saved = await store.GetRecentLogsAsync();
        Assert.Single(saved);
        Assert.Equal("New Venue", saved[0].VenueName);
        Assert.Equal(-2, saved[0].KeyChangeSemitones);
        Assert.Equal(loggedAt, saved[0].LoggedAt);
    }

    [Fact]
    public async Task RemoveRecentLogAsync_removes_matching_entry()
    {
        var store = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var performedOn = new DateTime(2026, 8, 2);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            1, "Song", "Artist", "Venue", performedOn, null, performedOn));

        await store.RemoveRecentLogAsync(1, performedOn);

        Assert.Empty(await store.GetRecentLogsAsync());
    }
}
