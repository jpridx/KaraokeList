namespace KaraokeList.Shared;

internal static class FlexibleSearch
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var buffer = new char[text.Length];
        var index = 0;
        foreach (var character in text)
        {
            if (!char.IsLetterOrDigit(character))
            {
                continue;
            }

            buffer[index++] = char.ToLowerInvariant(character);
        }

        return new string(buffer, 0, index);
    }

    public static bool Contains(string candidate, string normalizedQuery) =>
        Normalize(candidate).Contains(normalizedQuery, StringComparison.Ordinal);
}
