using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Moq;
using Microsoft.Extensions.DependencyInjection;

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

        var cut = Render<MyPerformances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ticks", cut.Markup);
            Assert.DoesNotContain(">loadError<", cut.Markup);
        });
    }

    [Fact]
    public void Shows_slow_api_notice_while_loading()
    {
        var tcs = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(tcs.Task);

        var notifier = Services.GetRequiredService<ApiSlowRequestNotifier>();
        using var tracker = notifier.TrackRequest();
        var cut = Render<MyPerformances>();

        tracker.MarkSlow();
        cut.Render();

        Assert.Contains(ApiTransientFailure.ColdStartInProgressMessage, cut.Markup);
    }
}
