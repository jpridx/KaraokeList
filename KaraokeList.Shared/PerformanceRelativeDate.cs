namespace KaraokeList.Shared;

/// <summary>
/// Relative performance dates using the caller's local calendar (typically browser DateTime.Today on WASM).
/// </summary>
public static class PerformanceRelativeDate
{
    public static DateTime ResolveAsOfDate(DateTime? asOfDate) =>
        (asOfDate ?? DateTime.Today).Date;

    public static bool TryParseAsOfDate(string? value, out DateTime asOfDate)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            asOfDate = DateTime.Today;
            return true;
        }

        if (DateOnly.TryParse(value, out var date))
        {
            asOfDate = date.ToDateTime(TimeOnly.MinValue);
            return true;
        }

        asOfDate = default;
        return false;
    }

    public static string ToQueryDate(DateTime today) => today.ToString("yyyy-MM-dd");

    /// <summary>
    /// Latest performance date that still qualifies as stale for the given threshold.
    /// Performances on or before this date are stale; later dates are not.
    /// </summary>
    public static DateTime StaleCutoff(int staleAfterDays, DateTime today) =>
        today.Date.AddDays(-staleAfterDays);

    public static int? DaysSince(DateTime? lastPerformedOn, DateTime today)
    {
        if (lastPerformedOn is not DateTime last)
        {
            return null;
        }

        return Math.Max(0, (today.Date - last.Date).Days);
    }

    public static string FormatDaysSince(int days) =>
        days switch
        {
            0 => "today",
            1 => "1 day ago",
            _ => $"{days} days ago"
        };

    public static string FormatLastSang(DateTime date, DateTime today, string? venueName)
    {
        var daysSince = DaysSince(date, today);
        var when = daysSince switch
        {
            0 => "today",
            1 => "yesterday",
            int d => $"{d} days ago",
            _ => date.ToString("MMM d, yyyy")
        };

        return string.IsNullOrWhiteSpace(venueName)
            ? $"{when} ({date:M/d/yyyy})"
            : $"{when} at {venueName} ({date:M/d/yyyy})";
    }
}
