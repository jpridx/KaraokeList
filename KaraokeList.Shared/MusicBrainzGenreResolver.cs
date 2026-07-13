namespace KaraokeList.Shared;

/// <summary>
/// Maps MusicBrainz genre/tag labels to KaraokeList catalog genre names.
/// Like Room 222's Alice Johnson sorting students into the right class — only for rock, pop, and country.
/// </summary>
public static class MusicBrainzGenreResolver
{
    private static readonly Dictionary<string, string> CatalogNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alternative Rock"] = "Alternative Rock",
        ["Arena Rock"] = "Arena Rock",
        ["Blues Rock"] = "Blues Rock",
        ["Classic Rock"] = "Classic Rock",
        ["Folk Rock"] = "Folk Rock",
        ["Glam Rock"] = "Glam Rock",
        ["Hair Metal"] = "Hair Metal",
        ["Hard Rock"] = "Hard Rock",
        ["New Wave"] = "New Wave",
        ["Rock"] = "Rock",
        ["Rockabilly"] = "Rockabilly",
        ["Soft Rock"] = "Soft Rock",
        ["Southern Rock"] = "Southern Rock",
        ["Country Rock"] = "Country Rock",
        ["Pop Rock"] = "Pop Rock",
        ["Country"] = "Country",
        ["Outlaw Country"] = "Outlaw Country",
        ["Country Pop"] = "Country Pop",
        ["Adult Contemporary"] = "Adult Contemporary",
        ["Easy Listening"] = "Easy Listening",
        ["Pop"] = "Pop",
        ["Synth-Pop"] = "Synth-Pop",
        ["Disco"] = "Disco",
        ["R&B"] = "R&B",
        ["Soul"] = "Soul",
        ["Show Tunes"] = "Show Tunes",
        ["Unclassified"] = "Unclassified",
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aor"] = "Arena Rock",
        ["album rock"] = "Classic Rock",
        ["arena rock"] = "Arena Rock",
        ["alternative rock"] = "Alternative Rock",
        ["blues rock"] = "Blues Rock",
        ["classic rock"] = "Classic Rock",
        ["folk rock"] = "Folk Rock",
        ["glam rock"] = "Glam Rock",
        ["hair metal"] = "Hair Metal",
        ["hard rock"] = "Hard Rock",
        ["new wave"] = "New Wave",
        ["rock"] = "Rock",
        ["rockabilly"] = "Rockabilly",
        ["soft rock"] = "Soft Rock",
        ["southern rock"] = "Southern Rock",
        ["country rock"] = "Country Rock",
        ["pop rock"] = "Pop Rock",
        ["country"] = "Country",
        ["outlaw country"] = "Outlaw Country",
        ["country pop"] = "Country Pop",
        ["adult contemporary"] = "Adult Contemporary",
        ["easy listening"] = "Easy Listening",
        ["pop"] = "Pop",
        ["synth-pop"] = "Synth-Pop",
        ["synthpop"] = "Synth-Pop",
        ["disco"] = "Disco",
        ["r&b"] = "R&B",
        ["rnb"] = "R&B",
        ["rhythm and blues"] = "R&B",
        ["soul"] = "Soul",
        ["show tunes"] = "Show Tunes",
        ["musical theatre"] = "Show Tunes",
        ["musical theater"] = "Show Tunes",
        ["standards"] = "Show Tunes",
    };

    /// <summary>Generic labels that should lose to more specific subgenres.</summary>
    private static readonly HashSet<string> GenericCatalogNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rock",
        "Pop",
        "Country",
        "Unclassified",
    };

    public static string? MapToCatalogGenre(string? musicBrainzLabel)
    {
        if (string.IsNullOrWhiteSpace(musicBrainzLabel))
        {
            return null;
        }

        var trimmed = musicBrainzLabel.Trim();
        if (CatalogNames.TryGetValue(trimmed, out var direct))
        {
            return direct;
        }

        return Aliases.TryGetValue(trimmed, out var mapped) ? mapped : null;
    }

    public static string? ResolveBestGenre(IEnumerable<(string Name, int Count)> candidates)
    {
        var scored = candidates
            .Select(c => (Catalog: MapToCatalogGenre(c.Name), c.Count))
            .Where(c => c.Catalog is not null)
            .GroupBy(c => c.Catalog!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                CatalogName = g.Key,
                TotalCount = g.Sum(x => x.Count),
                IsGeneric = GenericCatalogNames.Contains(g.Key)
            })
            .OrderByDescending(x => x.IsGeneric ? 0 : 1)
            .ThenByDescending(x => x.TotalCount)
            .ThenBy(x => x.CatalogName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return scored.FirstOrDefault()?.CatalogName;
    }

    public static int? ParseReleaseYear(string? firstReleaseDate)
    {
        if (string.IsNullOrWhiteSpace(firstReleaseDate))
        {
            return null;
        }

        var trimmed = firstReleaseDate.Trim();
        if (trimmed.Length >= 4 && int.TryParse(trimmed.AsSpan(0, 4), out var year) && year is >= 1900 and <= 2100)
        {
            return year;
        }

        return null;
    }
}
