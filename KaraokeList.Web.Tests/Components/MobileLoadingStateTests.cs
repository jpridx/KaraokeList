using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class MobileLoadingStateTests : BunitTestContext
{
    [Fact]
    public void Shows_loading_message_when_loading()
    {
        var cut = RenderComponent<MobileLoadingState>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.Message, "Loading stats…"));

        Assert.Contains("Loading stats…", cut.Markup);
        Assert.Contains("text-muted", cut.Markup);
    }

    [Fact]
    public void Renders_child_content_when_not_loading()
    {
        var cut = RenderComponent<MobileLoadingState>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.ChildContent, builder => builder.AddMarkupContent(0, "<p>Ready</p>")));

        Assert.Contains("Ready", cut.Markup);
        Assert.DoesNotContain("Loading...", cut.Markup);
    }

    [Fact]
    public void Uses_custom_css_class_for_loading_message()
    {
        var cut = RenderComponent<MobileLoadingState>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.CssClass, "custom-loading"));

        Assert.Contains("custom-loading", cut.Markup);
    }
}
