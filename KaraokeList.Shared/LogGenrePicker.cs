namespace KaraokeList.Shared;

public static class LogGenrePicker
{
    public static int? ResolveGenreId(string? genreName, IEnumerable<GenreDto> genres)
    {
        if (string.IsNullOrWhiteSpace(genreName))
        {
            return null;
        }

        var trimmed = genreName.Trim();
        return genres.FirstOrDefault(g =>
            g.GenreName.Equals(trimmed, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public static bool NeedsNewGenre(string? genreName, IEnumerable<GenreDto> genres) =>
        !string.IsNullOrWhiteSpace(genreName) && ResolveGenreId(genreName, genres) is null;
}
