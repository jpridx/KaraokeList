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
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Older Song", new DateTime(2026, 6, 1))
        };

        var local = new List<RecentLoggedPerformance>
        {
            new(2, "Offline Song", "Artist", "Offline Venue", new DateTime(2026, 7, 10), null, now)
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
                QueuedAt: now)
        };

        var merged = RecentLogsHydrator.Merge(api, local, pending, hydratedAt: now.AddMinutes(-5), now: now);

        Assert.Equal(2, merged.Count);
        Assert.Equal("Offline Song", merged[0].Title);
        Assert.Equal("Older Song", merged[1].Title);
    }

    [Fact]
    public void Merge_limits_to_max_recent_logs()
    {
        var hydratedAt = new DateTime(2026, 8, 2, 21, 0, 0);
        var api = Enumerable.Range(1, 6)
            .Select(i => CreateApiEntry(i, $"Song {i}", new DateTime(2026, 7, i)))
            .ToList();

        var merged = RecentLogsHydrator.Merge(api, [], [], hydratedAt: hydratedAt);

        Assert.Equal(LogPerformanceLocalStore.MaxRecentLogs, merged.Count);
        Assert.Equal("Song 6", merged[0].Title);
        Assert.Equal("Song 4", merged[2].Title);
    }

    [Fact]
    public void Merge_preserves_local_loggedAt_when_api_has_same_performance()
    {
        var performedOn = new DateTime(2026, 8, 2);
        var freshLoggedAt = new DateTime(2026, 8, 2, 21, 15, 0);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Tonight Song", performedOn)
        };
        var local = new List<RecentLoggedPerformance>
        {
            new(1, "Tonight Song", "Artist", "Main Stage", performedOn, null, freshLoggedAt)
        };

        var merged = RecentLogsHydrator.Merge(api, local, [], hydratedAt: freshLoggedAt.AddMinutes(5));

        Assert.Single(merged);
        Assert.Equal(freshLoggedAt, merged[0].LoggedAt);
    }

    [Fact]
    public void Merge_keeps_fresh_local_entries_not_yet_on_server()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var freshLoggedAt = now.AddMinutes(-10);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Older Song", new DateTime(2026, 7, 1))
        };
        var local = new List<RecentLoggedPerformance>
        {
            new(2, "Just Logged", "Artist", "Main Stage", now.Date, null, freshLoggedAt)
        };

        var merged = RecentLogsHydrator.Merge(api, local, [], hydratedAt: now.AddHours(-1), now: now);

        Assert.Equal(2, merged.Count);
        Assert.Equal("Just Logged", merged[0].Title);
        Assert.Equal(freshLoggedAt, merged[0].LoggedAt);
    }

    [Fact]
    public void Merge_uses_hydratedAt_for_api_entries_without_local_match()
    {
        var hydratedAt = new DateTime(2026, 8, 2, 21, 0, 0);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "July Song", new DateTime(2026, 7, 9))
        };

        var merged = RecentLogsHydrator.Merge(api, [], [], hydratedAt: hydratedAt);

        Assert.Single(merged);
        Assert.Equal(hydratedAt, merged[0].LoggedAt);
    }

    [Fact]
    public void Merge_handles_duplicate_local_keys_without_throwing()
    {
        var performedOn = new DateTime(2026, 8, 2);
        var olderLoggedAt = new DateTime(2026, 8, 2, 20, 0, 0);
        var newerLoggedAt = new DateTime(2026, 8, 2, 21, 0, 0);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Repeat Song", performedOn)
        };
        var local = new List<RecentLoggedPerformance>
        {
            new(1, "Repeat Song", "Artist", "Main Stage", performedOn, null, olderLoggedAt),
            new(1, "Repeat Song", "Artist", "Main Stage", performedOn, null, newerLoggedAt)
        };

        var merged = RecentLogsHydrator.Merge(api, local, [], hydratedAt: newerLoggedAt.AddMinutes(5));

        Assert.Single(merged);
        Assert.Equal(newerLoggedAt, merged[0].LoggedAt);
    }

    [Fact]
    public void Merge_ranks_by_loggedAt_so_backfilled_entries_surface_in_top_three()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Recent Performance", new DateTime(2026, 8, 1)),
            CreateApiEntry(2, "Another Recent", new DateTime(2026, 7, 30)),
            CreateApiEntry(3, "Third Recent", new DateTime(2026, 7, 29)),
        };
        var local = new List<RecentLoggedPerformance>
        {
            new(4, "Backfilled Old Date", "Artist", "Main Stage", new DateTime(2026, 5, 1), null, now)
        };

        var merged = RecentLogsHydrator.Merge(api, local, [], hydratedAt: now.AddHours(-1), now: now);

        Assert.Equal(LogPerformanceLocalStore.MaxRecentLogs, merged.Count);
        Assert.Equal("Backfilled Old Date", merged[0].Title);
    }

    [Fact]
    public void Merge_uses_hydratedAt_without_extending_freshness_to_now()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var cacheHydratedAt = now.AddHours(-3);
        var api = new List<MyPerformanceEntryDto>
        {
            CreateApiEntry(1, "Cached Song", new DateTime(2026, 7, 9))
        };

        var merged = RecentLogsHydrator.Merge(api, [], [], hydratedAt: cacheHydratedAt, now: now);

        Assert.Single(merged);
        Assert.Equal(cacheHydratedAt, merged[0].LoggedAt);
        Assert.True(RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(cacheHydratedAt, now));
        Assert.False(RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(cacheHydratedAt, now.AddHours(2)));
    }

    [Fact]
    public void Merge_dedupes_duplicate_pending_local_entries_by_key()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var performedOn = new DateTime(2026, 7, 10);
        var olderLoggedAt = now.AddMinutes(-20);
        var newerLoggedAt = now.AddMinutes(-5);
        var local = new List<RecentLoggedPerformance>
        {
            new(2, "Offline Song", "Artist", "Offline Venue", performedOn, null, olderLoggedAt),
            new(2, "Offline Song", "Artist", "Offline Venue", performedOn, null, newerLoggedAt)
        };
        var pending = new List<PendingPerformanceEntry>
        {
            new(
                Guid.NewGuid(),
                SingerId: 1,
                SongId: 2,
                VenueId: 9,
                PerformedOn: performedOn,
                KeyChangeSemitones: null,
                Title: "Offline Song",
                ArtistName: "Artist",
                VenueName: "Offline Venue",
                QueuedAt: now)
        };

        var merged = RecentLogsHydrator.Merge([], local, pending, now: now);

        Assert.Single(merged);
        Assert.Equal(newerLoggedAt, merged[0].LoggedAt);
    }

    [Fact]
    public void Merge_dedupes_duplicate_fresh_local_entries_by_key()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var performedOn = now.Date;
        var olderLoggedAt = now.AddMinutes(-20);
        var newerLoggedAt = now.AddMinutes(-5);
        var local = new List<RecentLoggedPerformance>
        {
            new(2, "Just Logged", "Artist", "Main Stage", performedOn, null, olderLoggedAt),
            new(2, "Just Logged", "Artist", "Main Stage", performedOn, null, newerLoggedAt)
        };

        var merged = RecentLogsHydrator.Merge([], local, [], now: now);

        Assert.Single(merged);
        Assert.Equal(newerLoggedAt, merged[0].LoggedAt);
    }

    [Fact]
    public void Merge_prefers_api_venue_when_local_has_same_song_and_date()
    {
        var performedOn = new DateTime(2026, 8, 2);
        var freshLoggedAt = new DateTime(2026, 8, 2, 21, 0, 0);
        var api = new List<MyPerformanceEntryDto>
        {
            new()
            {
                SongId = 1,
                Title = "Tonight Song",
                ArtistName = "Artist",
                VenueName = "Corrected Venue",
                PerformedOn = performedOn
            }
        };
        var local = new List<RecentLoggedPerformance>
        {
            new(1, "Tonight Song", "Artist", "Wrong Venue", performedOn, null, freshLoggedAt)
        };

        var hydratedAt = freshLoggedAt.AddMinutes(5);
        var merged = RecentLogsHydrator.Merge(api, local, [], hydratedAt: hydratedAt);

        Assert.Single(merged);
        Assert.Equal("Corrected Venue", merged[0].VenueName);
        Assert.Equal(hydratedAt, merged[0].LoggedAt);
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
