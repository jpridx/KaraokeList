using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record CachedListSongsEntry(SingerListKind Kind, IReadOnlyList<RepertoireSongDto> Songs);

public sealed record CachedMySongsLists(
    IReadOnlyList<SingerListDto> Lists,
    IReadOnlyList<CachedListSongsEntry> ListsSongs,
    DateTime CachedAtUtc);

public sealed record MySongsLoadResult(
    IReadOnlyList<SingerListDto> Lists,
    IReadOnlyList<RepertoireSongDto> Songs,
    IReadOnlyList<GenreDto> FilterGenres,
    bool FromCache,
    bool HasCache,
    DateTime? CachedAtUtc,
    string? ErrorMessage,
    bool NeedsSingerLink);

internal static class RepertoireSongSort
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
            "lastPerformed" => ascending
                ? songs.OrderBy(s => s.LastPerformedOn ?? DateTime.MinValue).ThenBy(s => s.Title, Text).ToList()
                : songs.OrderByDescending(s => s.LastPerformedOn ?? DateTime.MinValue).ThenBy(s => s.Title, Text).ToList(),
            _ => ascending
                ? songs.OrderBy(s => s.Title, Text).ToList()
                : songs.OrderByDescending(s => s.Title, Text).ToList()
        };
    }
}
