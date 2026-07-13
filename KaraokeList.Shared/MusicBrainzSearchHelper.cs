using System.Text.RegularExpressions;

namespace KaraokeList.Shared;

/// <summary>
/// Search query building and release-date helpers for MusicBrainz lookups.
/// </summary>
public static partial class MusicBrainzSearchHelper
{
    private const int MaxSearchQueries = 6;

    /// <summary>
    /// Builds progressively relaxed Lucene queries for MusicBrainz recording search.
    /// </summary>
    public static IReadOnlyList<string> BuildSearchQueries(string title, string artist)
    {
        var queries = new List<string>();
        var trimmedTitle = title.Trim();
        var trimmedArtist = artist.Trim();

        AddQuery(queries, $"\"{EscapeQuery(trimmedTitle)}\" AND artist:\"{EscapeQuery(trimmedArtist)}\"");
        AddQuery(queries, $"{EscapeQuery(trimmedTitle)} AND artist:\"{EscapeQuery(trimmedArtist)}\"");
        AddQuery(queries, $"{EscapeQuery(trimmedTitle)} AND artist:{EscapeQuery(trimmedArtist)}");

        var normalizedTitle = NormalizeSearchTerm(trimmedTitle);
        var normalizedArtist = NormalizeSearchTerm(trimmedArtist);
        if (!string.Equals(normalizedTitle, trimmedTitle, StringComparison.Ordinal)
            || !string.Equals(normalizedArtist, trimmedArtist, StringComparison.Ordinal))
        {
            AddQuery(queries, $"\"{EscapeQuery(normalizedTitle)}\" AND artist:\"{EscapeQuery(normalizedArtist)}\"");
            AddQuery(queries, $"{EscapeQuery(normalizedTitle)} AND artist:{EscapeQuery(normalizedArtist)}");
        }

        var titleWithoutThe = StripLeadingArticle(trimmedTitle);
        var artistWithoutThe = StripLeadingArticle(trimmedArtist);
        if (!string.Equals(titleWithoutThe, trimmedTitle, StringComparison.Ordinal)
            || !string.Equals(artistWithoutThe, trimmedArtist, StringComparison.Ordinal))
        {
            AddQuery(queries, $"\"{EscapeQuery(titleWithoutThe)}\" AND artist:\"{EscapeQuery(artistWithoutThe)}\"");
        }

        return queries;
    }

    /// <summary>
    /// Returns the earliest year found across MusicBrainz date strings.
    /// </summary>
    public static int? ResolveEarliestReleaseYear(IEnumerable<string?> dates)
    {
        int? earliest = null;
        foreach (var date in dates)
        {
            var year = MusicBrainzGenreResolver.ParseReleaseYear(date);
            if (year is int parsed && (earliest is null || parsed < earliest))
            {
                earliest = parsed;
            }
        }

        return earliest;
    }

    /// <summary>
    /// Strips parenthetical suffixes and normalizes apostrophes for broader search matching.
    /// </summary>
    public static string NormalizeSearchTerm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = ParentheticalSuffix().Replace(value.Trim(), string.Empty).Trim();
        normalized = normalized
            .Replace('\u2019', '\'')
            .Replace('`', '\'')
            .Replace("\u2018", "'", StringComparison.Ordinal);

        return CollapseWhitespace().Replace(normalized, " ");
    }

    /// <summary>
    /// Returns a variant with apostrophes removed (e.g. Rockin' → Rockin).
    /// </summary>
    public static string WithoutApostrophes(string value) =>
        value.Replace("'", string.Empty, StringComparison.Ordinal);

    public static string StripLeadingArticle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[4..].Trim();
        }

        return trimmed;
    }

    public static string EscapeQuery(string value) =>
        value.Replace("\"", "\\\"", StringComparison.Ordinal);

    public static bool IsUnwantedSecondaryType(string? secondaryType)
    {
        if (string.IsNullOrWhiteSpace(secondaryType))
        {
            return false;
        }

        return secondaryType.Contains("Live", StringComparison.OrdinalIgnoreCase)
            || secondaryType.Contains("Compilation", StringComparison.OrdinalIgnoreCase)
            || secondaryType.Contains("Remaster", StringComparison.OrdinalIgnoreCase)
            || secondaryType.Contains("Reissue", StringComparison.OrdinalIgnoreCase)
            || secondaryType.Contains("Mix", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddQuery(List<string> queries, string query)
    {
        if (queries.Count >= MaxSearchQueries || queries.Contains(query, StringComparer.Ordinal))
        {
            return;
        }

        queries.Add(query);
    }

    [GeneratedRegex(@"\s*[\(\[].*?[\)\]]", RegexOptions.CultureInvariant)]
    private static partial Regex ParentheticalSuffix();

    [GeneratedRegex(@"\s{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex CollapseWhitespace();
}
