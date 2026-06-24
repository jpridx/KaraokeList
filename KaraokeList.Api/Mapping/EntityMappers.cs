using KaraokeList.Data;
using KaraokeList.Shared;

namespace KaraokeList.Api.Mapping;

public static class EntityMappers
{
    public static VenueDto ToDto(this Venue entity) => new() { Id = entity.Id, VenueName = entity.VenueName };

    public static Venue ToEntity(this VenueDto dto) => new() { Id = dto.Id, VenueName = dto.VenueName };

    public static GenreDto ToDto(this Genre entity) => new() { Id = entity.Id, GenreName = entity.GenreName };

    public static Genre ToEntity(this GenreDto dto) => new() { Id = dto.Id, GenreName = dto.GenreName };

    public static ArtistDto ToDto(this Artist entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SortableName = entity.SortableName,
        MainGenre = entity.MainGenre
    };

    public static Artist ToEntity(this ArtistDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        SortableName = dto.SortableName,
        MainGenre = dto.MainGenre
    };

    public static SingerDto ToDto(this Singer entity) => new() { Id = entity.Id, Name = entity.Name };

    public static Singer ToEntity(this SingerDto dto) => new() { Id = dto.Id, Name = dto.Name };

    public static SongDto ToDto(this Song entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Artist = entity.Artist,
        Genre = entity.Genre,
        Year = entity.Year,
        SecondaryArtist = entity.SecondaryArtist
    };

    public static Song ToEntity(this SongDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Artist = dto.Artist,
        Genre = dto.Genre,
        Year = dto.Year,
        SecondaryArtist = dto.SecondaryArtist
    };

    public static ArtistLookupDto ToDto(this ArtistLookup entity) => new() { Id = entity.Id, Name = entity.Name };

    public static PerformanceDto ToDto(this Performance entity) => new()
    {
        Id = entity.Id,
        Singer = entity.Singer,
        Song = entity.Song,
        Venue = entity.Venue,
        PerformedOn = entity.PerformedOn,
        KeyChangeSemitones = entity.KeyChangeSemitones
    };

    public static Performance ToEntity(this PerformanceDto dto) => new()
    {
        Id = dto.Id,
        Singer = dto.Singer ?? 0,
        Song = dto.Song ?? 0,
        Venue = dto.Venue,
        PerformedOn = dto.PerformedOn,
        KeyChangeSemitones = dto.KeyChangeSemitones
    };

    public static RepertoireSongDto ToDto(this RepertoireSong song) => new()
    {
        SongId = song.SongId,
        Title = song.Title,
        ArtistName = song.ArtistName,
        GenreId = song.GenreId,
        GenreName = song.GenreName,
        LastPerformedOn = song.LastPerformedOn,
        PerformanceCount = song.PerformanceCount
    };

    public static SongPerformanceSummaryDto ToDto(this SongPerformanceSummary summary) => new()
    {
        SongId = summary.SongId,
        PerformanceCount = summary.PerformanceCount,
        LastKeyChangeSemitones = summary.LastKeyChangeSemitones,
        LastPerformedOn = summary.LastPerformedOn,
        LastVenueName = summary.LastVenueName,
        History = summary.History.Select(h => new PerformanceHistoryEntryDto
        {
            Id = h.Id,
            PerformedOn = h.PerformedOn,
            VenueId = h.VenueId,
            VenueName = h.VenueName,
            KeyChangeSemitones = h.KeyChangeSemitones
        }).ToList()
    };

    public static MyPerformanceEntryDto ToDto(this MyPerformanceEntry entry) => new()
    {
        Id = entry.Id,
        SongId = entry.SongId,
        Title = entry.Title,
        ArtistName = entry.ArtistName,
        PerformedOn = entry.PerformedOn,
        VenueId = entry.VenueId,
        VenueName = entry.VenueName,
        KeyChangeSemitones = entry.KeyChangeSemitones
    };

    public static StaleSongDto ToDto(this StaleSong song, DateTime today)
    {
        var lastPerformed = song.LastPerformedOn.Date;
        return new StaleSongDto
        {
            SongId = song.SongId,
            Title = song.Title,
            ArtistName = song.ArtistName,
            LastPerformedOn = lastPerformed,
            PerformanceCount = song.PerformanceCount,
            DaysSinceLastPerformed = Math.Max(0, (today - lastPerformed).Days)
        };
    }

    public static SingerStatsDto ToDto(this SingerStats stats, DateTime today)
    {
        int? daysSince = stats.LastPerformedOn is DateTime last
            ? Math.Max(0, (today - last.Date).Days)
            : null;

        return new SingerStatsDto
        {
            TotalPerformances = stats.TotalPerformances,
            UniqueSongs = stats.UniqueSongs,
            LastPerformedOn = stats.LastPerformedOn?.Date,
            LastVenueName = stats.LastVenueName,
            DaysSinceLastPerformance = daysSince,
            PerformancesThisMonth = stats.PerformancesThisMonth,
            PerformancesThisYear = stats.PerformancesThisYear,
            TopVenues = stats.TopVenues.Select(v => new VenueStatDto
            {
                VenueName = v.VenueName,
                PerformanceCount = v.PerformanceCount
            }).ToList()
        };
    }
}
