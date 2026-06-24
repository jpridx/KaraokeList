using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SingerStatsTeaserTests : AuthPageTestContext
{
    [Fact]
    public void Renders_nothing_when_no_performances()
    {
        Api.Setup(client => client.GetMySingerStatsAsync(0, 0, 0, 0))
            .ReturnsAsync(SingerStatsResult.Ok(new SingerStatsDto()));

        var cut = RenderComponent<SingerStatsTeaser>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("View all stats", cut.Markup));
    }

    [Fact]
    public void Shows_summary_and_link_when_stats_available()
    {
        Api.Setup(client => client.GetMySingerStatsAsync(0, 0, 0, 0))
            .ReturnsAsync(SingerStatsResult.Ok(new SingerStatsDto
            {
                TotalPerformances = 127,
                UniqueSongs = 48,
                PerformancesThisMonth = 4,
                PerformancesThisYear = 22
            }));

        var cut = RenderComponent<SingerStatsTeaser>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("127", cut.Markup);
            Assert.Contains("48", cut.Markup);
            Assert.Contains("View all stats", cut.Markup);
            Assert.Contains("href=\"my-stats\"", cut.Markup);
        });
    }
}

public sealed class MyStatsPageTests : AuthPageTestContext
{
    [Fact]
    public void Shows_extended_sections_when_stats_available()
    {
        Api.Setup(client => client.GetMySingerStatsAsync(5, 10, 10, 30))
            .ReturnsAsync(SingerStatsResult.Ok(new SingerStatsDto
            {
                TotalPerformances = 10,
                UniqueSongs = 5,
                PerformancesThisMonth = 2,
                PerformancesThisYear = 10,
                TopVenues = [new VenueStatDto { VenueName = "The Pub", PerformanceCount = 6 }],
                TopSongs =
                [
                    new SongStatDto { SongId = 1, Title = "Bohemian Rhapsody", ArtistName = "Queen", PerformanceCount = 4 }
                ],
                TopArtists = [new ArtistStatDto { ArtistId = 2, ArtistName = "Queen", PerformanceCount = 4 }],
                NewRepertoireSongs =
                [
                    new NewRepertoireSongDto
                    {
                        SongId = 3,
                        Title = "New Song",
                        ArtistName = "Artist",
                        FirstPerformedOn = DateTime.Today
                    }
                ],
                NewRepertoireDays = 30
            }));

        var cut = RenderComponent<KaraokeList.Web.Pages.MyStats>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("My stats", cut.Markup);
            Assert.Contains("Most performed songs", cut.Markup);
            Assert.Contains("Bohemian Rhapsody", cut.Markup);
            Assert.Contains("Most performed artists", cut.Markup);
            Assert.Contains("New to your repertoire", cut.Markup);
        });
    }
}
