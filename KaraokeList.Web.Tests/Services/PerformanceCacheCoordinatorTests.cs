using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class PerformanceCacheCoordinatorTests
{
    [Fact]
    public async Task PatchAfterUpdateAsync_updates_performances_and_recent_logs()
    {
        var performancesStore = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await performancesStore.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 10,
                SongId = 5,
                Title = "Song",
                ArtistName = "Artist",
                VenueName = "Old Venue",
                PerformedOn = new DateTime(2026, 8, 2)
            }
        ],
            DateTime.UtcNow));

        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        var performedOn = new DateTime(2026, 8, 2);
        var loggedAt = new DateTime(2026, 8, 2, 21, 0, 0);
        await logStore.AddRecentLogAsync(new RecentLoggedPerformance(
            5, "Song", "Artist", "Old Venue", performedOn, null, loggedAt));

        var mySongsLoader = new Mock<IMySongsLoader>();
        var coordinator = new PerformanceCacheCoordinator(
            new MyPerformancesLoader(new NotImplementedApiClient(), performancesStore),
            logStore,
            mySongsLoader.Object);

        var before = new PerformanceEditSnapshot(
            10, 5, "Song", "Artist", "Artist", performedOn, 1, "Old Venue", null);
        var after = before with { VenueId = 2, VenueName = "New Venue" };

        await coordinator.PatchAfterUpdateAsync(before, after);

        var performances = await performancesStore.GetCachedAsync();
        Assert.Equal("New Venue", performances!.Performances[0].VenueName);

        var recentLogs = await logStore.GetRecentLogsAsync();
        Assert.Equal("New Venue", recentLogs[0].VenueName);
        Assert.Equal(loggedAt, recentLogs[0].LoggedAt);
    }

    [Fact]
    public async Task RebuildRecentLogsFromPerformancesAsync_replaces_recent_logs_from_cache()
    {
        var performancesStore = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await performancesStore.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                SongId = 1,
                Title = "Fresh Song",
                ArtistName = "Artist",
                VenueName = "Main Stage",
                PerformedOn = new DateTime(2026, 7, 9)
            }
        ],
            DateTime.UtcNow));

        var logStore = new LogPerformanceLocalStore(new InMemoryLocalStorage());
        await logStore.AddRecentLogAsync(new RecentLoggedPerformance(
            99, "Stale Song", "Artist", "Old Venue", new DateTime(2026, 6, 11), null, new DateTime(2026, 6, 11)));

        var coordinator = new PerformanceCacheCoordinator(
            new MyPerformancesLoader(new NotImplementedApiClient(), performancesStore),
            logStore,
            new Mock<IMySongsLoader>().Object);

        await coordinator.RebuildRecentLogsFromPerformancesAsync();

        var recentLogs = await logStore.GetRecentLogsAsync();
        Assert.Single(recentLogs);
        Assert.Equal("Fresh Song", recentLogs[0].Title);
        Assert.Equal("Main Stage", recentLogs[0].VenueName);
    }
}
