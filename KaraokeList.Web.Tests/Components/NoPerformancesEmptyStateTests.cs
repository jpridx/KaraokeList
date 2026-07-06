using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class NoPerformancesEmptyStateTests : BunitTestContext
{
    [Fact]
    public void Renders_default_log_action()
    {
        var cut = Render<NoPerformancesEmptyState>();

        Assert.Contains("No performances logged yet.", cut.Markup);
        Assert.Contains("Log a performance", cut.Markup);
        Assert.Contains("href=\"log\"", cut.Markup);
    }

    [Fact]
    public void Renders_custom_action_text()
    {
        var cut = Render<NoPerformancesEmptyState>(parameters => parameters
            .Add(p => p.ActionText, "Log your first song"));

        Assert.Contains("Log your first song", cut.Markup);
    }
}
