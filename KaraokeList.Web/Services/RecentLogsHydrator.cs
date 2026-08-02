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

        var apiEntries = apiPerformances
            .Select(performance => FromApi(performance, hydratedAt.Value))
            .Select(entry => localByKey.TryGetValue(Key(entry), out var local)
                ? entry with { LoggedAt = local.LoggedAt }
                : entry)
            .ToList();
        var apiKeys = apiEntries.Select(Key).ToHashSet();

        var pendingKeys = pending
            .Select(entry => (entry.SongId, entry.PerformedOn.Date, entry.VenueName))
            .ToHashSet();

        var pendingLocals = localLogs
            .Where(log => pendingKeys.Contains((log.SongId, log.PerformedOn.Date, log.VenueName)))
            .Where(log => !apiKeys.Contains(Key(log)))
            .ToList();

        var freshLocalOnly = localLogs
            .Where(log => !apiKeys.Contains(Key(log)))
            .Where(log => !pendingKeys.Contains((log.SongId, log.PerformedOn.Date, log.VenueName)))
            .Where(log => RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(log.LoggedAt, now))
            .ToList();

        return pendingLocals
            .Concat(freshLocalOnly)
            .Concat(apiEntries)
            .OrderByDescending(log => log.LoggedAt)
            .ThenByDescending(log => log.PerformedOn)
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
            loggedAt);

    private static (int SongId, DateTime Date, string VenueName) Key(RecentLoggedPerformance log) =>
        (log.SongId, log.PerformedOn.Date, log.VenueName);

    private static Dictionary<(int SongId, DateTime Date, string VenueName), RecentLoggedPerformance> IndexLocalLogsByKey(
        IReadOnlyList<RecentLoggedPerformance> localLogs) =>
        localLogs
            .GroupBy(Key)
            .ToDictionary(group => group.Key, group => group.MaxBy(log => log.LoggedAt)!);
}
