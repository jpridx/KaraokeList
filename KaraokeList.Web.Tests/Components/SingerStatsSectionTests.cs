using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SingerStatsSectionTests : AuthPageTestContext
{
    [Fact]
    public void Renders_nothing_when_no_performances()
    {
        Api.Setup(client => client.GetMySingerStatsAsync(3))
            .ReturnsAsync(SingerStatsResult.Ok(new SingerStatsDto()));

        var cut = RenderComponent<SingerStatsSection>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("Your stats", cut.Markup));
    }

    [Fact]
    public void Shows_summary_when_stats_available()
    {
        Api.Setup(client => client.GetMySingerStatsAsync(3))
            .ReturnsAsync(SingerStatsResult.Ok(new SingerStatsDto
            {
                TotalPerformances = 127,
                UniqueSongs = 48,
                PerformancesThisMonth = 4,
                PerformancesThisYear = 22,
                LastPerformedOn = DateTime.Today,
                DaysSinceLastPerformance = 0,
                LastVenueName = "The Pub",
                TopVenues =
                [
                    new VenueStatDto { VenueName = "The Pub", PerformanceCount = 42 }
                ]
            }));

        var cut = RenderComponent<SingerStatsSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Your stats", cut.Markup);
            Assert.Contains("127", cut.Markup);
            Assert.Contains("48", cut.Markup);
            Assert.Contains("The Pub", cut.Markup);
            Assert.Contains("Top venues", cut.Markup);
        });
    }
}
