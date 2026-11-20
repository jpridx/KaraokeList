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

    event Action? RepertoireStatsChanged;

    Task PatchAfterUpdateAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after);

    Task PatchAfterDeleteAsync(PerformanceEditSnapshot deleted);

    Task PatchAfterCreateAsync(MyPerformanceEntryDto created);

    Task RebuildRecentLogsFromPerformancesAsync();
}

public sealed class PerformanceCacheCoordinator(
    IMyPerformancesLoader performancesLoader,
    ILogPerformanceLocalStore logStore,
    IMySongsLoader mySongsLoader) : IPerformanceCacheCoordinator
{
    public event Action? RecentLogsChanged;

    public event Action? RepertoireStatsChanged;

    public async Task PatchAfterUpdateAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after)
    {
        await PatchMyPerformancesCacheAsync(before, after);
        await PatchRecentLogAsync(before, after);
        await PatchMySongsIfNeededAsync(before, after);

        if (before.SongId != after.SongId || before.PerformedOn.Date != after.PerformedOn.Date)
        {
            await SyncRepertoireStatsForSongAsync(before.SongId);
            if (after.SongId != before.SongId)
            {
                await SyncRepertoireStatsForSongAsync(after.SongId);
            }

            NotifyRepertoireStatsChanged();
        }

        NotifyRecentLogsChanged();
    }

    public async Task PatchAfterCreateAsync(MyPerformanceEntryDto created)
    {
        await performancesLoader.AddPerformanceAsync(created);
        NotifyRecentLogsChanged();
    }

    public async Task PatchAfterDeleteAsync(PerformanceEditSnapshot deleted)
    {
        await performancesLoader.RemovePerformanceAsync(deleted.PerformanceId);
        await logStore.RemoveRecentLogAsync(deleted.SongId, deleted.PerformedOn);
        await SyncRepertoireStatsForSongAsync(deleted.SongId);
        NotifyRepertoireStatsChanged();
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

        var sameSong = existing.SongId == after.SongId;
        var artistName = PreferNonBlank(after.ArtistName, sameSong ? existing.ArtistName : null);
        await performancesLoader.PatchPerformanceAsync(new MyPerformanceEntryDto
        {
            Id = existing.Id,
            SongId = after.SongId,
            Title = PreferNonBlank(after.Title, sameSong ? existing.Title : null),
            ArtistName = artistName,
            ArtistDisplay = PreferNonBlank(
                after.ArtistDisplay,
                sameSong ? existing.ArtistDisplay : null,
                artistName),
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
                PreferNonBlank(after.Title, after.SongId == matching.SongId ? matching.Title : null),
                PreferNonBlank(after.ArtistName, after.SongId == matching.SongId ? matching.ArtistName : null),
                after.VenueName,
                after.PerformedOn,
                after.KeyChangeSemitones == 0 ? null : after.KeyChangeSemitones,
                matching.LoggedAt));
    }

    private async Task PatchMySongsIfNeededAsync(
        PerformanceEditSnapshot before,
        PerformanceEditSnapshot after)
    {
        if (before.PerformedOn.Date == after.PerformedOn.Date && before.SongId == after.SongId)
        {
            return;
        }

        if (before.SongId != after.SongId)
        {
            await SyncMySongsPerformanceForSongAsync(before.SongId);
        }

        await SyncMySongsPerformanceForSongAsync(after.SongId);
    }

    private async Task SyncMySongsPerformanceForSongAsync(int songId)
    {
        var cached = await performancesLoader.TryGetCachedAsync();
        if (cached is null)
        {
            return;
        }

        var remaining = cached.Performances
            .Where(p => p.SongId == songId)
            .OrderByDescending(p => p.PerformedOn)
            .ToList();

        if (remaining.Count == 0)
        {
            await mySongsLoader.SetSongPerformanceStatsAsync(
                songId,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                0);
            return;
        }

        var latest = remaining[0];
        await mySongsLoader.SetSongPerformanceStatsAsync(
            songId,
            latest.Title,
            latest.ArtistName,
            latest.ArtistDisplay ?? latest.ArtistName,
            latest.PerformedOn,
            remaining.Count);
    }

    private async Task SyncRepertoireStatsForSongAsync(int songId)
    {
        var logCached = await logStore.GetCachedCatalogAsync();
        if (logCached?.RepertoireStats is not { Count: > 0 } stats)
        {
            return;
        }

        var statsList = stats.ToList();
        var existingIndex = statsList.FindIndex(s => s.SongId == songId);
        if (existingIndex < 0)
        {
            return;
        }

        var performancesCached = await performancesLoader.TryGetCachedAsync();
        var songPerformances = performancesCached?.Performances
            .Where(p => p.SongId == songId)
            .OrderByDescending(p => p.PerformedOn)
            .ToList() ?? [];

        var existing = statsList[existingIndex];
        if (songPerformances.Count == 0)
        {
            statsList[existingIndex] = existing with
            {
                LastPerformedOn = null,
                PerformanceCount = 0
            };
        }
        else
        {
            var latest = songPerformances[0];
            statsList[existingIndex] = existing with
            {
                Title = latest.Title,
                ArtistName = latest.ArtistName,
                ArtistDisplay = latest.ArtistDisplay ?? latest.ArtistName,
                LastPerformedOn = latest.PerformedOn.Date,
                PerformanceCount = songPerformances.Count
            };
        }

        await logStore.SaveCachedCatalogAsync(logCached with
        {
            RepertoireStats = statsList,
            CachedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Edit snapshots from song-detail history rows have no title/artist; keep the cached values
    /// rather than blanking them (issue #347).
    /// </summary>
    private static string PreferNonBlank(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? string.Empty;

    private void NotifyRecentLogsChanged() => RecentLogsChanged?.Invoke();

    private void NotifyRepertoireStatsChanged() => RepertoireStatsChanged?.Invoke();
}
