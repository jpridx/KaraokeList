using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class LogSongPickItemTests
{
    private static readonly LogSongPickItem DontStopBelievin = new(1, "Don't Stop Believin'", "Journey", true);

    [Theory]
    [InlineData("dont")]
    [InlineData("dontstop")]
    [InlineData("journey")]
    public void MatchesSearch_uses_normalized_search_key(string searchText)
    {
        var normalized = FlexibleSearch.Normalize(searchText);

        Assert.True(DontStopBelievin.MatchesSearch(normalized));
    }

    [Fact]
    public void MatchesSearch_returns_false_when_query_not_found()
    {
        var normalized = FlexibleSearch.Normalize("zzzz");

        Assert.False(DontStopBelievin.MatchesSearch(normalized));
    }
}
