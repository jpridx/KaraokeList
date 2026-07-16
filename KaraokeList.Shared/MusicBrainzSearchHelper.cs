using System.Text.RegularExpressions;

namespace KaraokeList.Shared;

/// <summary>
/// Search query building, title matching, and release-date helpers for MusicBrainz lookups.
/// </summary>
public static partial class MusicBrainzSearchHelper
{
    private const int MaxSearchQueries = 10;

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

        var hyphenAsSpaceTitle = trimmedTitle.Replace('-', ' ');
        if (!string.Equals(hyphenAsSpaceTitle, trimmedTitle, StringComparison.Ordinal))
        {
            AddQuery(queries, $"\"{EscapeQuery(hyphenAsSpaceTitle)}\" AND artist:\"{EscapeQuery(trimmedArtist)}\"");
        }

        var andTitle = trimmedTitle.Replace(" & ", " and ", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(andTitle, trimmedTitle, StringComparison.OrdinalIgnoreCase))
        {
            AddQuery(queries, $"\"{EscapeQuery(andTitle)}\" AND artist:\"{EscapeQuery(trimmedArtist)}\"");
        }

        var andArtist = trimmedArtist.Replace(" & ", " and ", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(andArtist, trimmedArtist, StringComparison.OrdinalIgnoreCase))
        {
            AddQuery(queries, $"\"{EscapeQuery(trimmedTitle)}\" AND artist:\"{EscapeQuery(andArtist)}\"");
            AddQuery(queries, $"{EscapeQuery(trimmedTitle)} AND artist:{EscapeQuery(andArtist)}");
        }

        var artistWithoutFeat = StripFeaturingSuffix(trimmedArtist);
        if (!string.Equals(artistWithoutFeat, trimmedArtist, StringComparison.OrdinalIgnoreCase))
        {
            AddQuery(queries, $"\"{EscapeQuery(trimmedTitle)}\" AND artist:\"{EscapeQuery(artistWithoutFeat)}\"");
        }

        return queries;
    }

    /// <summary>
    /// Lucene queries that exclude live recordings so studio originals surface (e.g. KISS "Strutter" 1974).
    /// </summary>
    public static IReadOnlyList<string> BuildStudioSearchQueries(string title, string artist)
    {
        var queries = new List<string>();
        var trimmedTitle = title.Trim();
        var trimmedArtist = artist.Trim();

        AddQuery(queries, $"{EscapeQuery(trimmedTitle)} AND artist:{EscapeQuery(trimmedArtist)} AND NOT live");
        AddQuery(queries, $"\"{EscapeQuery(trimmedTitle)}\" AND artist:\"{EscapeQuery(trimmedArtist)}\" AND NOT live");

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

    public static string StripFeaturingSuffix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var featIndex = trimmed.IndexOf(" feat.", StringComparison.OrdinalIgnoreCase);
        if (featIndex > 0)
        {
            return trimmed[..featIndex].Trim();
        }

        featIndex = trimmed.IndexOf(" ft.", StringComparison.OrdinalIgnoreCase);
        if (featIndex > 0)
        {
            return trimmed[..featIndex].Trim();
        }

        return trimmed;
    }

    public static string EscapeQuery(string value) =>
        value.Replace("\"", "\\\"", StringComparison.Ordinal);

    /// <summary>
    /// True when a recording title matches the user's search title (punctuation-insensitive).
    /// </summary>
    public static bool TitleMatchesSearch(string recordingTitle, string searchTitle) =>
        string.Equals(
            FlexibleSearch.Normalize(recordingTitle),
            FlexibleSearch.Normalize(searchTitle),
            StringComparison.Ordinal);

    /// <summary>
    /// True when catalog and MusicBrainz artist credits refer to the same act (e.g. Hall &amp; Oates vs Daryl Hall &amp; John Oates).
    /// </summary>
    public static bool ArtistMatchesSearch(string catalogArtist, string matchArtistDisplay)
    {
        if (string.IsNullOrWhiteSpace(catalogArtist) || string.IsNullOrWhiteSpace(matchArtistDisplay))
        {
            return false;
        }

        var catalog = StripFeaturingSuffix(catalogArtist.Trim());
        var match = StripFeaturingSuffix(matchArtistDisplay.Trim());
        if (string.Equals(catalog, match, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(NormalizeArtistComparable(catalog), NormalizeArtistComparable(match), StringComparison.Ordinal))
        {
            return true;
        }

        var catalogSurnames = ExtractDuoSurnames(catalog);
        var matchSurnames = ExtractDuoSurnames(match);
        return catalogSurnames.Count >= 2
            && matchSurnames.Count >= 2
            && catalogSurnames.SetEquals(matchSurnames);
    }

    /// <summary>
    /// True when catalog title/artist match a MusicBrainz suggestion (punctuation-insensitive on title).
    /// </summary>
    public static bool NamesMatchCatalog(string catalogTitle, string catalogArtist, CanonicalMatchDto match) =>
        TitleMatchesSearch(catalogTitle, match.Title)
        && ArtistMatchesSearch(catalogArtist, match.ArtistCreditDisplay);

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

    /// <summary>
    /// Ranks MusicBrainz match suggestions for karaoke cataloging: oldest exact-title studio original first.
    /// </summary>
    public static List<CanonicalMatchDto> RankMatches(
        IEnumerable<CanonicalMatchDto> matches,
        string searchTitle,
        Func<CanonicalMatchDto, bool>? isSoftUnwanted = null,
        string? searchArtist = null)
    {
        var list = matches.ToList();
        var oldestExactTitleYear = list
            .Where(m => TitleMatchesSearch(m.Title, searchTitle) && m.Year is int)
            .Select(m => m.Year!.Value)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        return list
            .OrderBy(m => GetClearlyUnwantedRank(m))
            .ThenBy(m => TitleMatchesSearch(m.Title, searchTitle) ? 0 : 1)
            .ThenBy(m => GetArtistMatchRank(m, searchArtist))
            .ThenBy(m => GetSoftUnwantedRank(m, searchTitle, oldestExactTitleYear, isSoftUnwanted))
            .ThenBy(m => m.Year ?? int.MaxValue)
            .ThenByDescending(m => m.Score)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Sorts MusicBrainz match suggestions oldest release year first (undated and likely reissues last).
    /// </summary>
    public static List<CanonicalMatchDto> SortMatchesOldestFirst(
        IEnumerable<CanonicalMatchDto> matches,
        string? searchTitle = null) =>
        string.IsNullOrWhiteSpace(searchTitle)
            ? RankMatches(matches, string.Empty)
            : RankMatches(matches, searchTitle);

    public static bool IsLikelyReissueOrLive(CanonicalMatchDto match)
    {
        if (string.IsNullOrWhiteSpace(match.Disambiguation))
        {
            return false;
        }

        var disambiguation = match.Disambiguation;
        return disambiguation.Contains("live", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("compilation", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("remaster", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("reissue", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsClearlyUnwantedDisambiguation(string? disambiguation)
    {
        if (string.IsNullOrWhiteSpace(disambiguation))
        {
            return false;
        }

        return disambiguation.Contains("live", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("demo", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("interview", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("remix", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("dj-mix", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("dj mix", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("mastermix", StringComparison.OrdinalIgnoreCase)
            || disambiguation.Contains("5.1 mix", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Oldest release year among exact-title matches that are not live/demo/interview/remix.
    /// </summary>
    public static int? GetOldestCredibleYear(IEnumerable<CanonicalMatchDto> matches, string searchTitle)
    {
        var years = matches
            .Where(m => TitleMatchesSearch(m.Title, searchTitle))
            .Where(m => !IsClearlyUnwantedDisambiguation(m.Disambiguation))
            .Where(m => m.Year is int)
            .Select(m => m.Year!.Value)
            .ToList();

        return years.Count > 0 ? years.Min() : null;
    }

    /// <summary>
    /// Picks the best karaoke catalog suggestion from a lookup pool — oldest credible studio-style original.
    /// Ignores any catalog year; title and artist matching only.
    /// </summary>
    public static CanonicalMatchDto? SelectBestCredibleSuggestion(
        IEnumerable<CanonicalMatchDto> matches,
        string searchTitle,
        string? searchArtist = null)
    {
        var pool = matches.Where(m => m.Found).ToList();
        if (pool.Count == 0)
        {
            return null;
        }

        var credible = pool
            .Where(m => TitleMatchesSearch(m.Title, searchTitle))
            .Where(m => !IsClearlyUnwantedDisambiguation(m.Disambiguation))
            .ToList();

        var scoped = credible.Count > 0 ? credible : pool;
        if (!string.IsNullOrWhiteSpace(searchArtist))
        {
            var exactArtist = scoped
                .Where(m => GetArtistMatchRank(m, searchArtist) == 0)
                .ToList();
            if (exactArtist.Count > 0)
            {
                scoped = exactArtist;
            }
            else
            {
                var equivalentArtist = scoped
                    .Where(m => GetArtistMatchRank(m, searchArtist) == 1)
                    .ToList();
                if (equivalentArtist.Count > 0)
                {
                    scoped = equivalentArtist;
                }
            }
        }

        var oldestYear = scoped
            .Where(m => m.Year is int)
            .Select(m => m.Year!.Value)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        if (oldestYear < int.MaxValue)
        {
            var oldestMatches = scoped
                .Where(m => m.Year == oldestYear)
                .OrderByDescending(m => m.Score)
                .ToList();

            if (oldestMatches.Count > 0)
            {
                return oldestMatches[0];
            }
        }

        return RankMatches(pool, searchTitle, searchArtist: searchArtist).FirstOrDefault();
    }

    public static bool IsBestCredibleMatch(
        CanonicalMatchDto match,
        string searchTitle,
        IEnumerable<CanonicalMatchDto> pool,
        string? searchArtist = null)
    {
        var best = SelectBestCredibleSuggestion(pool, searchTitle, searchArtist);
        return best is not null
            && string.Equals(best.RecordingMbid, match.RecordingMbid, StringComparison.Ordinal);
    }

    private static int GetClearlyUnwantedRank(CanonicalMatchDto match) =>
        IsClearlyUnwantedDisambiguation(match.Disambiguation) ? 1 : 0;

    private static int GetArtistMatchRank(CanonicalMatchDto match, string? searchArtist)
    {
        if (string.IsNullOrWhiteSpace(searchArtist))
        {
            return 0;
        }

        var catalog = StripFeaturingSuffix(searchArtist.Trim());
        var display = StripFeaturingSuffix(match.ArtistCreditDisplay.Trim());
        if (string.Equals(catalog, display, StringComparison.OrdinalIgnoreCase)
            || string.Equals(NormalizeArtistComparable(catalog), NormalizeArtistComparable(display), StringComparison.Ordinal))
        {
            return 0;
        }

        return ArtistMatchesSearch(searchArtist, match.ArtistCreditDisplay) ? 1 : 2;
    }

    private static string NormalizeArtistComparable(string artist) =>
        FlexibleSearch.Normalize(artist.Replace(" and ", " & ", StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> ExtractDuoSurnames(string artist)
    {
        var surnames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in SplitArtistParts(artist))
        {
            var tokens = part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0)
            {
                surnames.Add(tokens[^1]);
            }
        }

        return surnames;
    }

    private static List<string> SplitArtistParts(string artist)
    {
        var normalized = artist.Replace(" and ", " & ", StringComparison.OrdinalIgnoreCase);
        return normalized
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();
    }

    private static int GetSoftUnwantedRank(
        CanonicalMatchDto match,
        string searchTitle,
        int oldestExactTitleYear,
        Func<CanonicalMatchDto, bool>? isSoftUnwanted)
    {
        var isExactTitle = TitleMatchesSearch(match.Title, searchTitle);
        var isOldestExact = isExactTitle
            && match.Year == oldestExactTitleYear
            && oldestExactTitleYear < int.MaxValue;

        if (isOldestExact)
        {
            return 0;
        }

        if (IsLikelyReissueOrLive(match) || isSoftUnwanted?.Invoke(match) == true)
        {
            return 1;
        }

        return 0;
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
