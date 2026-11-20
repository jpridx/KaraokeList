using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class RecentLogsHydrator
{
    public static IReadOnlyList<RecentLoggedPerformance> Merge(
        IReadOnlyList<MyPerformanceEntryDto> apiPerformances,
        IReadOnlyList<RecentLoggedPerformance> localLogs,
        IReadOnlyList<PendingPerformanceEntry> pending,
        int maxCount = LogPerformanceLocalStore.MaxRecentLogs,
        DateTime? hydratedAt = null,
        DateTime? now = null)
    {
        now ??= DateTime.Now;
        hydratedAt ??= now;
        var localByKey = IndexLocalLogsByKey(localLogs);
        var dedupedLocalLogs = localByKey.Values;

        // Rank server rows newest performance first, matching My Performances (date desc, id desc).
        var apiEntries = MyPerformancesQuery.Apply(apiPerformances, venueId: null, sortDir: "desc")
            .Select((performance, rank) =>
            {
                var entry = FromApi(performance, hydratedAt.Value);
                // A matching device log keeps its real log time and device-log status.
                if (localByKey.TryGetValue(Key(entry), out var local))
                {
                    entry = entry with { LoggedAt = local.LoggedAt, FromServer = local.FromServer };
                }

                return (Entry: entry, Rank: rank);
            })
            .ToList();
        var apiKeys = apiEntries.Select(e => Key(e.Entry)).ToHashSet();
        var apiSongDates = apiPerformances
            .Select(p => (p.SongId, p.PerformedOn.Date))
            .ToHashSet();

        var pendingKeys = pending
            .Select(entry => (entry.SongId, entry.PerformedOn.Date, entry.VenueName))
            .ToHashSet();

        var pendingLocals = dedupedLocalLogs
            .Where(log => pendingKeys.Contains((log.SongId, log.PerformedOn.Date, log.VenueName)))
            .Where(log => !apiKeys.Contains(Key(log)))
            .ToList();

        // Rows hydrated from the server that are no longer on the server are dropped, not kept as "local".
        var freshLocalOnly = dedupedLocalLogs
            .Where(log => !log.FromServer)
            .Where(log => !apiKeys.Contains(Key(log)))
            .Where(log => !apiSongDates.Contains((log.SongId, log.PerformedOn.Date)))
            .Where(log => !pendingKeys.Contains((log.SongId, log.PerformedOn.Date, log.VenueName)))
            .Where(log => RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(log.LoggedAt, now))
            .ToList();

        // Device logs first (newest log first), then server-filled rows by performance order.
        // Server rows carry a hydration timestamp, not a real log time, so it must not decide order
        // (issues #335 / #347: last week's rows outranked and then evicted tonight's logs).
        var deviceLogs = pendingLocals
            .Concat(freshLocalOnly)
            .Concat(apiEntries.Where(e => !e.Entry.FromServer).Select(e => e.Entry))
            .OrderByDescending(log => log.LoggedAt)
            .ThenByDescending(log => log.PerformedOn);
        var serverRows = apiEntries
            .Where(e => e.Entry.FromServer)
            .OrderBy(e => e.Rank)
            .Select(e => e.Entry);

        return deviceLogs
            .Concat(serverRows)
            .Take(maxCount)
            .ToList();
    }

    public static RecentLoggedPerformance FromApi(MyPerformanceEntryDto performance, DateTime loggedAt) =>
        new(
            performance.SongId,
            performance.Title,
            performance.ArtistName,
            performance.VenueName,
            performance.PerformedOn,
            performance.KeyChangeSemitones,
            loggedAt,
            FromServer: true);

    private static (int SongId, DateTime Date, string VenueName) Key(RecentLoggedPerformance log) =>
        (log.SongId, log.PerformedOn.Date, log.VenueName);

    private static Dictionary<(int SongId, DateTime Date, string VenueName), RecentLoggedPerformance> IndexLocalLogsByKey(
        IReadOnlyList<RecentLoggedPerformance> localLogs) =>
        localLogs
            .GroupBy(Key)
            .ToDictionary(group => group.Key, group => group.MaxBy(log => log.LoggedAt)!);
}
