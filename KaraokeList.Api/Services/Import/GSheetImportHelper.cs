namespace KaraokeList.Api.Services.Import;

internal static class GSheetImportHelper
{
    /// <summary>
    /// Converts a Google Sheets sharing URL to a CSV export URL.
    /// Handles /edit, /pub, and plain spreadsheet URLs, preserving the gid (tab id) when present.
    /// </summary>
    public static string? BuildCsvExportUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (!uri.Host.Equals("docs.google.com", StringComparison.OrdinalIgnoreCase))
            return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dIdx = Array.IndexOf(segments, "d");
        if (dIdx < 0 || dIdx + 1 >= segments.Length)
            return null;

        var sheetId = segments[dIdx + 1];
        var gid = ExtractGid(uri.Fragment) ?? ExtractGid(uri.Query);
        var gidParam = gid is not null ? $"&gid={gid}" : string.Empty;

        return $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv{gidParam}";
    }

    private static string? ExtractGid(string haystack)
    {
        if (string.IsNullOrEmpty(haystack)) return null;
        const string prefix = "gid=";
        var idx = haystack.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = idx + prefix.Length;
        var end = haystack.IndexOf('&', start);
        return end < 0 ? haystack[start..] : haystack[start..end];
    }
}
