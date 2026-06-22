namespace KaraokeList.Shared;

public static class MyPerformancesSearch
{
    public static IEnumerable<MyPerformanceEntryDto> Filter(IEnumerable<MyPerformanceEntryDto> performances, string? searchText)
    {
        var query = searchText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            return performances;
        }

        return performances.Where(p => Matches(p, query));
    }

    public static bool Matches(MyPerformanceEntryDto performance, string query) =>
        performance.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
        || performance.ArtistName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || performance.VenueName.Contains(query, StringComparison.OrdinalIgnoreCase);
}
