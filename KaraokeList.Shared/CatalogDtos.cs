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

public class CoPerformerDto
{
    public int? SingerId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CoPerformerInputDto
{
    public int? SingerId { get; set; }
    public string? DisplayName { get; set; }

    public CoPerformerInputDto Clone() => new() { SingerId = SingerId, DisplayName = DisplayName };

    public static List<CoPerformerInputDto> CloneList(IEnumerable<CoPerformerInputDto> performers) =>
        performers.Select(p => p.Clone()).ToList();
}

public class PerformanceDto
{
    public int Id { get; set; }
    public int? Singer { get; set; }
    public int? Song { get; set; }
    public int? Venue { get; set; }
    public DateTime PerformedOn { get; set; } = DateTime.Today;
    public int? KeyChangeSemitones { get; set; }
    public List<CoPerformerInputDto>? OtherPerformers { get; set; }
    public List<CoPerformerDto> CoPerformers { get; set; } = [];
}

public class PerformanceHistoryEntryDto
{
    public int Id { get; set; }
    public DateTime PerformedOn { get; set; }
    public int? VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? KeyChangeSemitones { get; set; }
    public List<CoPerformerDto> OtherPerformers { get; set; } = [];
}

public class MyPerformanceEntryDto
{
    public int Id { get; set; }
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public DateTime PerformedOn { get; set; }
    public int? VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? KeyChangeSemitones { get; set; }
    public List<CoPerformerDto> OtherPerformers { get; set; } = [];
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
    public DateTime? LastPerformedOn { get; set; }
    public int PerformanceCount { get; set; }
}

public class StaleSongDto
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public DateTime? LastPerformedOn { get; set; }
    public int PerformanceCount { get; set; }
    public int DaysSinceLastPerformed { get; set; }
}

public class StaleSongsResponseDto
{
    public int StaleAfterDays { get; set; }
    public List<StaleSongDto> Songs { get; set; } = [];
}

public class VenueStatDto
{
    public string VenueName { get; set; } = string.Empty;
    public int PerformanceCount { get; set; }
}

public class SongStatDto
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public int PerformanceCount { get; set; }
}

public class ArtistStatDto
{
    public int ArtistId { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public int PerformanceCount { get; set; }
}

public class NewRepertoireSongDto
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public DateTime FirstPerformedOn { get; set; }
}

public class SingerStatsDto
{
    public int TotalPerformances { get; set; }
    public int UniqueSongs { get; set; }
    public DateTime? LastPerformedOn { get; set; }
    public string? LastVenueName { get; set; }
    public int? DaysSinceLastPerformance { get; set; }
    public int PerformancesThisMonth { get; set; }
    public int PerformancesThisYear { get; set; }
    public List<VenueStatDto> TopVenues { get; set; } = [];
    public List<SongStatDto> TopSongs { get; set; } = [];
    public List<ArtistStatDto> TopArtists { get; set; } = [];
    public List<NewRepertoireSongDto> NewRepertoireSongs { get; set; } = [];
    public int NewRepertoireDays { get; set; }
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

public static class ShowHostMessageFormatting
{
    /// <summary>Title - Artist, optional (with …) co-performers, and (±N) key change suffix.</summary>
    public static string Format(
        string title,
        string artistName,
        int? keyChangeSemitones,
        IReadOnlyList<string>? coPerformers = null)
    {
        var trimmedTitle = title.Trim();
        var message = string.IsNullOrWhiteSpace(artistName)
            ? trimmedTitle
            : $"{trimmedTitle} - {artistName.Trim()}";

        var coPerformerNames = coPerformers?
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToList();
        if (coPerformerNames is { Count: > 0 })
        {
            message += $" (with {string.Join(" & ", coPerformerNames)})";
        }

        if (keyChangeSemitones is int key && key != 0)
        {
            message += key > 0
                ? $" (Up {key})"
                : $" (Down {Math.Abs(key)})";
        }

        return message;
    }
}

public static class CoPerformerFormatting
{
    public static string GetDisplayName(CoPerformerInputDto performer, IReadOnlyList<SingerDto> singers)
    {
        if (performer.SingerId is int singerId)
        {
            return singers.FirstOrDefault(s => s.Id == singerId)?.Name ?? string.Empty;
        }

        return performer.DisplayName?.Trim() ?? string.Empty;
    }

    public static List<string> GetDisplayNames(
        IReadOnlyList<CoPerformerInputDto> performers,
        IReadOnlyList<SingerDto> singers) =>
        performers
            .Select(p => GetDisplayName(p, singers))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
}

public static class CoPerformerValidation
{
    public const int MaxDisplayNameLength = 128;

    public static string? ValidateInputs(
        IReadOnlyList<CoPerformerInputDto> performers,
        int primarySingerId,
        Func<int, bool> singerExists)
    {
        var seenSingerIds = new HashSet<int>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var performer in performers)
        {
            var displayName = performer.DisplayName?.Trim() ?? string.Empty;
            if (performer.SingerId is int singerId)
            {
                if (!string.IsNullOrEmpty(displayName))
                {
                    return "Each co-performer must be either a registered singer or a guest name, not both.";
                }

                if (singerId == primarySingerId)
                {
                    return "You are already logged as the primary performer.";
                }

                if (!singerExists(singerId))
                {
                    return "A co-performer singer was not found.";
                }

                if (!seenSingerIds.Add(singerId))
                {
                    return "Duplicate co-performers are not allowed.";
                }
            }
            else if (!string.IsNullOrEmpty(displayName))
            {
                if (displayName.Length > MaxDisplayNameLength)
                {
                    return $"Guest names must be {MaxDisplayNameLength} characters or fewer.";
                }

                if (!seenNames.Add(displayName))
                {
                    return "Duplicate co-performers are not allowed.";
                }
            }
            else
            {
                return "Each co-performer needs a registered singer or a guest name.";
            }
        }

        return null;
    }
}
