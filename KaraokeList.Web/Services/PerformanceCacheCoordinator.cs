using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record PerformanceEditSnapshot(
    int PerformanceId,
    int SongId,
    string Title,
    string ArtistName,
    string? ArtistDisplay,
    DateTime PerformedOn,
    int? VenueId,
    string VenueName,
    int? KeyChangeSemitones);

public interface IPerformanceCacheCoordinator
{
    event Action? RecentLogsChanged;

    Task PatchAfterUpdateAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after);

    Task PatchAfterDeleteAsync(PerformanceEditSnapshot deleted);

    Task RebuildRecentLogsFromPerformancesAsync();
}

public sealed class PerformanceCacheCoordinator(
    IMyPerformancesLoader performancesLoader,
    ILogPerformanceLocalStore logStore,
    IMySongsLoader mySongsLoader) : IPerformanceCacheCoordinator
{
    public event Action? RecentLogsChanged;

    public async Task PatchAfterUpdateAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after)
    {
        await PatchMyPerformancesCacheAsync(before, after);
        await PatchRecentLogAsync(before, after);
        await PatchMySongsIfNeededAsync(before, after);
        NotifyRecentLogsChanged();
    }

    public async Task PatchAfterDeleteAsync(PerformanceEditSnapshot deleted)
    {
        await performancesLoader.RemovePerformanceAsync(deleted.PerformanceId);
        await logStore.RemoveRecentLogAsync(deleted.SongId, deleted.PerformedOn);
        NotifyRecentLogsChanged();
    }

    public async Task RebuildRecentLogsFromPerformancesAsync()
    {
        var loaded = await performancesLoader.TryGetCachedAsync();
        if (loaded is null || loaded.Performances.Count == 0)
        {
            return;
        }

        var hydratedAt = loaded.CachedAtUtc ?? DateTime.UtcNow;
        var localTime = hydratedAt.Kind == DateTimeKind.Utc
            ? hydratedAt.ToLocalTime()
            : hydratedAt;

        var rebuilt = loaded.Performances
            .Take(LogPerformanceLocalStore.MaxRecentLogs)
            .Select(p => RecentLogsHydrator.FromApi(p, localTime))
            .ToList();

        if (rebuilt.Count == 0)
        {
            return;
        }

        await logStore.ReplaceRecentLogsAsync(rebuilt);
        NotifyRecentLogsChanged();
    }

    private async Task PatchMyPerformancesCacheAsync(
        PerformanceEditSnapshot before,
        PerformanceEditSnapshot after)
    {
        var cached = await performancesLoader.TryGetCachedAsync();
        if (cached is null)
        {
            return;
        }

        var existing = cached.Performances.FirstOrDefault(p => p.Id == before.PerformanceId);
        if (existing is null)
        {
            return;
        }

        await performancesLoader.PatchPerformanceAsync(new MyPerformanceEntryDto
        {
            Id = existing.Id,
            SongId = after.SongId,
            Title = after.Title,
            ArtistName = after.ArtistName,
            ArtistDisplay = after.ArtistDisplay ?? after.ArtistName,
            PerformedOn = after.PerformedOn,
            VenueId = after.VenueId,
            VenueName = after.VenueName,
            KeyChangeSemitones = after.KeyChangeSemitones == 0 ? null : after.KeyChangeSemitones,
            OtherPerformers = existing.OtherPerformers
        });
    }

    private async Task PatchRecentLogAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after)
    {
        var recentLogs = await logStore.GetRecentLogsAsync();
        var matching = recentLogs.FirstOrDefault(l =>
            l.SongId == before.SongId && l.PerformedOn.Date == before.PerformedOn.Date);
        if (matching is null)
        {
            return;
        }

        await logStore.PatchRecentLogAsync(
            before.SongId,
            before.PerformedOn,
            new RecentLoggedPerformance(
                after.SongId,
                after.Title,
                after.ArtistName,
                after.VenueName,
                after.PerformedOn,
                after.KeyChangeSemitones == 0 ? null : after.KeyChangeSemitones,
                matching.LoggedAt));
    }

    private async Task PatchMySongsIfNeededAsync(
        PerformanceEditSnapshot before,
        PerformanceEditSnapshot after)
    {
        if (before.PerformedOn.Date == after.PerformedOn.Date)
        {
            return;
        }

        await mySongsLoader.PatchSongPerformanceAsync(
            after.SongId,
            after.Title,
            after.ArtistName,
            after.ArtistDisplay ?? after.ArtistName,
            after.PerformedOn);
    }

    private void NotifyRecentLogsChanged() => RecentLogsChanged?.Invoke();
}
