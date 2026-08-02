namespace KaraokeList.Shared;

public static class StaleSongsComputer
{
    public static IReadOnlyList<RepertoireSongDto> GetCandidates(
        IReadOnlyList<RepertoireSongDto> repertoire,
        IReadOnlySet<int> excludedSongIds,
        TicklerSettingsDto settings,
        DateTime asOfDate)
    {
        var cutoff = PerformanceRelativeDate.StaleCutoff(settings.StaleAfterDays, asOfDate);
        return repertoire
            .Where(song => !excludedSongIds.Contains(song.SongId))
            .Where(song => song.LastPerformedOn is null || song.LastPerformedOn.Value.Date <= cutoff.Date)
            .ToList();
    }

    public static StaleSongsResponseDto Compute(
        IReadOnlyList<RepertoireSongDto> repertoire,
        IReadOnlySet<int> excludedSongIds,
        TicklerSettingsDto settings,
        DateTime asOfDate,
        Random? random = null)
    {
        var candidates = GetCandidates(repertoire, excludedSongIds, settings, asOfDate);
        var rng = random ?? Random.Shared;
        var sampled = candidates
            .OrderBy(_ => rng.Next())
            .Take(settings.SongLimit)
            .Select(song => ToStaleSongDto(song, asOfDate))
            .ToList();

        return new StaleSongsResponseDto
        {
            StaleAfterDays = settings.StaleAfterDays,
            Songs = sampled
        };
    }

    private static StaleSongDto ToStaleSongDto(RepertoireSongDto song, DateTime asOfDate)
    {
        var lastPerformed = song.LastPerformedOn?.Date;
        return new StaleSongDto
        {
            SongId = song.SongId,
            Title = song.Title,
            ArtistName = song.ArtistName,
            ArtistDisplay = string.IsNullOrWhiteSpace(song.ArtistDisplay) ? song.ArtistName : song.ArtistDisplay,
            LastPerformedOn = lastPerformed,
            PerformanceCount = song.PerformanceCount,
            DaysSinceLastPerformed = PerformanceRelativeDate.DaysSince(lastPerformed, asOfDate) ?? 0
        };
    }
}
