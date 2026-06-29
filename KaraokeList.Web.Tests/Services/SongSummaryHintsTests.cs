using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class SongSummaryHintsTests
{
    [Fact]
    public void Format_returns_first_time_message_when_never_performed()
    {
        var hint = SongSummaryHints.Format(new SongPerformanceSummaryDto { PerformanceCount = 0 });

        Assert.Equal("First time logging this song for you.", hint);
    }

    [Fact]
    public void Format_includes_count_and_last_performance()
    {
        var hint = SongSummaryHints.Format(new SongPerformanceSummaryDto
        {
            PerformanceCount = 3,
            LastPerformedOn = new DateTime(2026, 6, 15),
            LastKeyChangeSemitones = 2
        });

        Assert.Contains("3 time(s)", hint);
        Assert.Contains(new DateTime(2026, 6, 15).ToString("d"), hint);
    }
}

public sealed class CatalogSongMapperCreatedSongTests
{
    [Fact]
    public void FindCreatedPickItem_matches_title_and_artist()
    {
        var items = new List<LogSongPickItem>
        {
            new(1, "Jeopardy", "The Greg Kihn Band", false, false),
            new(2, "Other Song", "Other Artist", false, false)
        };

        var match = CatalogSongMapper.FindCreatedPickItem(items, "Jeopardy", "The Greg Kihn Band");

        Assert.NotNull(match);
        Assert.Equal(1, match.Id);
    }
}
