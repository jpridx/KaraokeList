namespace KaraokeList.Api.Services.Import;

/// <summary>
/// Resolves column indices from a header row, or uses the default positional order
/// (Title=0, Artist=1, Genre=2, Year=3) when no header is detected.
/// </summary>
internal sealed class ColumnMap
{
    public int TitleIndex { get; }
    public int ArtistIndex { get; }
    public int GenreIndex { get; }
    public int YearIndex { get; }

    private ColumnMap(int title, int artist, int genre, int year)
    {
        TitleIndex = title;
        ArtistIndex = artist;
        GenreIndex = genre;
        YearIndex = year;
    }

    public static readonly ColumnMap Default = new(0, 1, 2, 3);

    /// <summary>Returns true if the cells look like a header row.</summary>
    public static bool IsHeaderRow(IReadOnlyList<string> cells) =>
        cells.Any(c =>
            c.Equals("title", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("song", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("artist", StringComparison.OrdinalIgnoreCase));

    public static ColumnMap FromHeaders(IReadOnlyList<string> headers)
    {
        int title = -1, artist = -1, genre = -1, year = -1;
        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].Trim().ToLowerInvariant();
            switch (h)
            {
                case "title" or "song" or "name": title = i; break;
                case "artist": artist = i; break;
                case "genre" or "genre name": genre = i; break;
                case "year": year = i; break;
            }
        }
        return new ColumnMap(title, artist, genre, year);
    }

    public CatalogImportRow? ToRow(IReadOnlyList<string> cells, int sourceRow)
    {
        var title = GetCell(cells, TitleIndex);
        var artist = GetCell(cells, ArtistIndex);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
            return null; // skip blank rows

        var genreStr = GetCell(cells, GenreIndex);
        var yearStr = GetCell(cells, YearIndex);
        int? year = int.TryParse(yearStr, out var y) ? y : null;

        return new CatalogImportRow(
            title ?? string.Empty,
            artist ?? string.Empty,
            string.IsNullOrWhiteSpace(genreStr) ? null : genreStr,
            year,
            sourceRow);
    }

    private static string? GetCell(IReadOnlyList<string> cells, int index) =>
        index >= 0 && index < cells.Count ? cells[index].Trim() : null;
}
