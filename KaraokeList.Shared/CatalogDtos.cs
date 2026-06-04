namespace KaraokeList.Shared;

public class VenueDto
{
    public int Id { get; set; }
    public string VenueName { get; set; } = string.Empty;
}

public class GenreDto
{
    public int Id { get; set; }
    public string GenreName { get; set; } = string.Empty;
}

public class ArtistDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SortableName { get; set; }
    public int? MainGenre { get; set; }
}

public class SingerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SongDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Artist { get; set; }
    public int? Genre { get; set; }
    public int? Year { get; set; }
    public int? SecondaryArtist { get; set; }
}

public class ArtistLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PerformanceDto
{
    public int Id { get; set; }
    public int? Singer { get; set; }
    public int? Song { get; set; }
    public int? Venue { get; set; }
    public DateTime PerformedOn { get; set; } = DateTime.Today;
    public int? KeyChangeSemitones { get; set; }
}

public class PerformanceHistoryEntryDto
{
    public DateTime PerformedOn { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? KeyChangeSemitones { get; set; }
}

public class SongPerformanceSummaryDto
{
    public int SongId { get; set; }
    public int PerformanceCount { get; set; }
    public int? LastKeyChangeSemitones { get; set; }
    public DateTime? LastPerformedOn { get; set; }
    public string? LastVenueName { get; set; }
    public List<PerformanceHistoryEntryDto> History { get; set; } = [];
}

public class RepertoireSongDto
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public int? GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public DateTime LastPerformedOn { get; set; }
    public int PerformanceCount { get; set; }
}

public static class KeyChangeFormatting
{
    public static string Describe(int? semitones) => semitones switch
    {
        null or 0 => "Original key",
        > 0 => $"Up {semitones} half-step{(semitones == 1 ? "" : "s")}",
        < 0 => $"Down {Math.Abs(semitones.Value)} half-step{(semitones == -1 ? "" : "s")}"
    };
}
