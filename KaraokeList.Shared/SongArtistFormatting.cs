namespace KaraokeList.Shared;

public class SongArtistDto
{
    public int ArtistId { get; set; }
    public int DisplayOrder { get; set; }
    public string Name { get; set; } = string.Empty;
}

public static class SongArtistFormatting
{
    public static string FormatDisplay(string? artistCreditDisplay, IEnumerable<string> artistNames)
    {
        if (!string.IsNullOrWhiteSpace(artistCreditDisplay))
        {
            return artistCreditDisplay.Trim();
        }

        return string.Join(", ", artistNames.Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    public static string PrimaryArtistName(IEnumerable<SongArtistDto> artists) =>
        artists
            .OrderBy(a => a.DisplayOrder)
            .Select(a => a.Name)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty;

    public static int? PrimaryArtistId(IEnumerable<SongArtistDto> artists) =>
        artists
            .OrderBy(a => a.DisplayOrder)
            .Select(a => (int?)a.ArtistId)
            .FirstOrDefault(id => id is > 0);
}
