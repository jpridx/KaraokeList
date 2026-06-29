using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class MyPerformancesPageTests : AuthPageTestContext
{
    public MyPerformancesPageTests()
    {
        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });
    }

    [Fact]
    public void Does_not_show_literal_loadError_when_performances_load()
    {
        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .ReturnsAsync(MyPerformancesResult.Ok(
            [
                new MyPerformanceEntryDto
                {
                    Id = 1,
                    SongId = 10,
                    Title = "Ticks",
                    ArtistName = "Brad Paisley",
                    PerformedOn = DateTime.Today
                }
            ]));

        var cut = RenderComponent<MyPerformances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ticks", cut.Markup);
            Assert.DoesNotContain(">loadError<", cut.Markup);
        });
    }
}
