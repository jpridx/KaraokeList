namespace KaraokeList.Shared;

/// <summary>
/// Relative performance dates using the caller's local calendar (typically browser DateTime.Today on WASM).
/// </summary>
public static class PerformanceRelativeDate
{
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
