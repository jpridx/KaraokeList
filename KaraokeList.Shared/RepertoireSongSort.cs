namespace KaraokeList.Shared;

public static class RepertoireSongSort
{
    private static readonly StringComparer Text = StringComparer.OrdinalIgnoreCase;

    public static List<RepertoireSongDto> Apply(
        IEnumerable<RepertoireSongDto> songs,
        string sortBy,
        string sortDir)
    {
        var ascending = sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "artist" => ascending
                ? songs.OrderBy(s => s.ArtistName, Text).ThenBy(s => s.Title, Text).ToList()
                : songs.OrderByDescending(s => s.ArtistName, Text).ThenByDescending(s => s.Title, Text).ToList(),
            "genre" => ascending
                ? songs.OrderBy(s => s.GenreName, Text).ThenBy(s => s.Title, Text).ToList()
                : songs.OrderByDescending(s => s.GenreName, Text).ThenByDescending(s => s.Title, Text).ToList(),
            "lastperformed" => ascending
                ? songs.OrderBy(s => s.LastPerformedOn ?? DateTime.MinValue).ThenBy(s => s.Title, Text).ToList()
                : songs.OrderByDescending(s => s.LastPerformedOn ?? DateTime.MinValue).ThenBy(s => s.Title, Text).ToList(),
            _ => ascending
                ? songs.OrderBy(s => s.Title, Text).ThenBy(s => s.ArtistName, Text).ToList()
                : songs.OrderByDescending(s => s.Title, Text).ThenByDescending(s => s.ArtistName, Text).ToList()
        };
    }
}
