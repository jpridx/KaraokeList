namespace KaraokeList.Shared;

public static class SortableNameFormatting
{
    private static readonly string[] LeadingArticles = ["The", "A", "An"];

    /// <summary>
    /// Builds a sort key by moving a leading article to the end (e.g. "The Birds" → "Birds, The").
    /// Returns null when no article prefix applies.
    /// </summary>
    public static string? FromDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var trimmed = displayName.Trim();
        foreach (var article in LeadingArticles)
        {
            var prefix = article + " ";
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remainder = trimmed[prefix.Length..].Trim();
            if (remainder.Length == 0)
            {
                return null;
            }

            var articleText = trimmed[..article.Length];
            return $"{remainder}, {articleText}";
        }

        return null;
    }
}
