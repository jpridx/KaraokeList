namespace KaraokeList.Shared;

public static class RepertoireSearch
{
    public static IEnumerable<RepertoireSongDto> Filter(IEnumerable<RepertoireSongDto> songs, string? searchText)
    {
        var query = searchText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            return songs;
        }

        return songs.Where(s => Matches(s, query));
    }

    public static bool Matches(RepertoireSongDto song, string query) =>
        song.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
        || song.ArtistName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || song.GenreName.Contains(query, StringComparison.OrdinalIgnoreCase);
}
