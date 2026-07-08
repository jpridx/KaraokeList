using KaraokeList.Shared;

namespace KaraokeList.Web.Tests;

public sealed class MyPerformancesQueryTests
{
    private static readonly MyPerformanceEntryDto Older = new()
    {
        Id = 1,
        SongId = 10,
        Title = "Older",
        PerformedOn = new DateTime(2024, 1, 1),
        VenueId = 1,
        VenueName = "Venue A"
    };

    private static readonly MyPerformanceEntryDto Newer = new()
    {
        Id = 2,
        SongId = 11,
        Title = "Newer",
        PerformedOn = new DateTime(2024, 6, 1),
        VenueId = 2,
        VenueName = "Venue B"
    };

    [Fact]
    public void Apply_filters_by_venue()
    {
        var results = MyPerformancesQuery.Apply([Older, Newer], venueId: 1, sortDir: "desc");

        Assert.Single(results);
        Assert.Equal("Older", results[0].Title);
    }

    [Fact]
    public void Apply_sorts_newest_first_by_default()
    {
        var results = MyPerformancesQuery.Apply([Older, Newer], venueId: null, sortDir: "desc");

        Assert.Equal(2, results.Count);
        Assert.Equal("Newer", results[0].Title);
        Assert.Equal("Older", results[1].Title);
    }

    [Fact]
    public void Apply_sorts_oldest_first_when_asc()
    {
        var results = MyPerformancesQuery.Apply([Older, Newer], venueId: null, sortDir: "asc");

        Assert.Equal("Older", results[0].Title);
        Assert.Equal("Newer", results[1].Title);
    }
}
