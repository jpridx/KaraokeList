using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class RecentLogsHydratorTests
{
    [Fact]
    public void Merge_prefers_api_performances_over_stale_local_logs()
    {
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "July Song A", new DateTime(2026, 7, 9)),
            CreateApiEntry(2, "July Song B", new DateTime(2026, 7, 9))
        };

        var local = new List<RecentLoggedPerformance>
        {
            CreateLocalEntry(3, "June Song", new DateTime(2026, 6, 11))
        };

        var merged = RecentLogsHydrator.Merge(api, local, []);

        Assert.Equal(2, merged.Count);
        Assert.Equal("July Song A", merged[0].Title);
        Assert.Equal("July Song B", merged[1].Title);
    }

    [Fact]
    public void Merge_keeps_pending_local_logs_not_yet_on_server()
    {
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Older Song", new DateTime(2026, 6, 1))
        };

        var local = new List<RecentLoggedPerformance>
        {
            CreateLocalEntry(2, "Offline Song", new DateTime(2026, 7, 10), "Offline Venue")
        };

        var pending = new List<PendingPerformanceEntry>
        {
            new(
                Guid.NewGuid(),
                SingerId: 1,
                SongId: 2,
                VenueId: 9,
                PerformedOn: new DateTime(2026, 7, 10),
                KeyChangeSemitones: null,
                Title: "Offline Song",
                ArtistName: "Artist",
                VenueName: "Offline Venue",
                QueuedAt: DateTime.UtcNow)
        };

        var merged = RecentLogsHydrator.Merge(api, local, pending);

        Assert.Equal(2, merged.Count);
        Assert.Equal("Offline Song", merged[0].Title);
        Assert.Equal("Older Song", merged[1].Title);
    }

    private static MyPerformanceEntryDto CreateApiEntry(int songId, string title, DateTime performedOn) =>
        new()
        {
            SongId = songId,
            Title = title,
            ArtistName = "Artist",
            VenueName = "Main Stage",
            PerformedOn = performedOn
        };

    private static RecentLoggedPerformance CreateLocalEntry(
        int songId,
        string title,
        DateTime performedOn,
        string venueName = "Main Stage") =>
        new(songId, title, "Artist", venueName, performedOn, null, performedOn);
}
