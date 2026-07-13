namespace KaraokeList.Shared;

public static class RepertoireSearch
{
    public static IEnumerable<RepertoireSongDto> Filter(IEnumerable<RepertoireSongDto> songs, string? searchText)
    {
        var query = FlexibleSearch.Normalize(searchText);
        if (string.IsNullOrEmpty(query))
        {
            return songs;
        }

        return songs.Where(s => Matches(s, query));
    }

    public static bool Matches(RepertoireSongDto song, string query) =>
        FlexibleSearch.Contains(song.Title, query)
        || FlexibleSearch.Contains(song.ArtistName, query)
        || FlexibleSearch.Contains(song.ArtistDisplay, query)
        || FlexibleSearch.Contains(song.GenreName, query);
}
