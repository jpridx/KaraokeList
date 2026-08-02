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
}
