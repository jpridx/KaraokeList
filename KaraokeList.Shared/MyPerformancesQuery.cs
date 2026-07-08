namespace KaraokeList.Shared;

public static class MyPerformancesQuery
{
    public static List<MyPerformanceEntryDto> Apply(
        IEnumerable<MyPerformanceEntryDto> performances,
        int? venueId,
        string sortDir)
    {
        var filtered = venueId is int id
            ? performances.Where(p => p.VenueId == id).ToList()
            : performances.ToList();

        return string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? filtered.OrderBy(p => p.PerformedOn).ThenBy(p => p.Id).ToList()
            : filtered.OrderByDescending(p => p.PerformedOn).ThenByDescending(p => p.Id).ToList();
    }
}
