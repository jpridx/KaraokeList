using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class RecentLogsHydrator
{
    public static IReadOnlyList<RecentLoggedPerformance> Merge(
        IReadOnlyList<MyPerformanceEntryDto> apiPerformances,
        IReadOnlyList<RecentLoggedPerformance> localLogs,
        IReadOnlyList<PendingPerformanceEntry> pending,
        int maxCount = LogPerformanceLocalStore.MaxRecentLogs)
    {
        var apiEntries = apiPerformances.Select(FromApi).ToList();
        var apiKeys = apiEntries.Select(Key).ToHashSet();

        var pendingKeys = pending
            .Select(entry => (entry.SongId, entry.PerformedOn.Date, entry.VenueName))
            .ToHashSet();

        var pendingLocals = localLogs
            .Where(log => pendingKeys.Contains((log.SongId, log.PerformedOn.Date, log.VenueName)))
            .Where(log => !apiKeys.Contains(Key(log)))
            .ToList();

        return pendingLocals
            .Concat(apiEntries)
            .OrderByDescending(log => log.PerformedOn)
            .ThenByDescending(log => log.LoggedAt)
            .Take(maxCount)
            .ToList();
    }

    public static RecentLoggedPerformance FromApi(MyPerformanceEntryDto performance) =>
        new(
            performance.SongId,
            performance.Title,
            performance.ArtistName,
            performance.VenueName,
            performance.PerformedOn,
            performance.KeyChangeSemitones,
            performance.PerformedOn);

    private static (int SongId, DateTime Date, string VenueName) Key(RecentLoggedPerformance log) =>
        (log.SongId, log.PerformedOn.Date, log.VenueName);
}
