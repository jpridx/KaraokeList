namespace KaraokeList.Shared;

public static class MyPerformancesSearch
{
    public static IEnumerable<MyPerformanceEntryDto> Filter(IEnumerable<MyPerformanceEntryDto> performances, string? searchText)
    {
        var query = FlexibleSearch.Normalize(searchText);
        if (string.IsNullOrEmpty(query))
        {
            return performances;
        }

        return performances.Where(p => Matches(p, query));
    }

    public static bool Matches(MyPerformanceEntryDto performance, string query) =>
        FlexibleSearch.Contains(performance.Title, query)
        || FlexibleSearch.Contains(performance.ArtistName, query)
        || FlexibleSearch.Contains(performance.VenueName, query);
}
