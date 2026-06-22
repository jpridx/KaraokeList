using KaraokeList.Shared;

namespace KaraokeList.Web.Tests;

public sealed class MyPerformancesSearchTests
{
    private static readonly MyPerformanceEntryDto Sample = new()
    {
        Id = 1,
        SongId = 10,
        Title = "Sweet Caroline",
        ArtistName = "Neil Diamond",
        PerformedOn = DateTime.Today,
        VenueName = "Main Stage"
    };

    [Fact]
    public void Filter_empty_query_returns_all()
    {
        var results = MyPerformancesSearch.Filter([Sample], "  ").ToList();

        Assert.Single(results);
    }

    [Theory]
    [InlineData("sweet")]
    [InlineData("diamond")]
    [InlineData("main")]
    public void Filter_matches_title_artist_or_venue(string query)
    {
        var results = MyPerformancesSearch.Filter([Sample], query).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void Filter_no_match_returns_empty()
    {
        var results = MyPerformancesSearch.Filter([Sample], "jazz").ToList();

        Assert.Empty(results);
    }
}
