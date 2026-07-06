using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class MobileEmptyStateTests : BunitTestContext
{
    [Fact]
    public void Renders_message_and_action_link()
    {
        var cut = Render<MobileEmptyState>(parameters => parameters
            .Add(p => p.Message, "No performances logged yet.")
            .Add(p => p.ActionHref, "log")
            .Add(p => p.ActionText, "Log a performance"));

        Assert.Contains("empty-state", cut.Markup);
        Assert.Contains("No performances logged yet.", cut.Markup);
        Assert.Contains("href=\"log\"", cut.Markup);
        Assert.Contains("Log a performance", cut.Markup);
    }

    [Fact]
    public void Renders_child_content()
    {
        var cut = Render<MobileEmptyState>(parameters => parameters
            .Add(p => p.Message, "No songs yet.")
            .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<button>Add song</button>")));

        Assert.Contains("No songs yet.", cut.Markup);
        Assert.Contains("Add song", cut.Markup);
    }

    [Fact]
    public void Applies_custom_container_class()
    {
        var cut = Render<MobileEmptyState>(parameters => parameters
            .Add(p => p.Message, "Empty")
            .Add(p => p.CssClass, "mobile-panel mt-3"));

        Assert.Contains("empty-state mobile-panel mt-3", cut.Markup);
    }
}
