using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class SongPerformanceSummaryPanelTests : BunitTestContext
{
    [Fact]
    public void Mobile_layout_shows_times_sung()
    {
        var cut = RenderComponent<SongPerformanceSummaryPanel>(parameters => parameters
            .Add(p => p.Summary, new SongPerformanceSummaryDto
            {
                PerformanceCount = 2,
                LastPerformedOn = new DateTime(2026, 6, 15),
                LastVenueName = "Main Stage",
                LastKeyChangeSemitones = null
            })
            .Add(p => p.GenreName, "Rock"));

        Assert.Contains("Times sung", cut.Markup);
        Assert.Contains("Main Stage", cut.Markup);
        Assert.Contains("Rock", cut.Markup);
    }

    [Fact]
    public void Admin_layout_shows_empty_message_when_never_performed()
    {
        var cut = RenderComponent<SongPerformanceSummaryPanel>(parameters => parameters
            .Add(p => p.Summary, new SongPerformanceSummaryDto { PerformanceCount = 0 })
            .Add(p => p.Layout, SongPerformanceSummaryLayout.Admin)
            .Add(p => p.CssClass, string.Empty));

        Assert.Contains("You have not logged any performances of this song yet.", cut.Markup);
    }
}
