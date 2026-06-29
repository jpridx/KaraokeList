using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class MobileBackLinkTests : BunitTestContext
{
    [Fact]
    public void Renders_default_back_to_more_link()
    {
        var cut = RenderComponent<MobileBackLink>();

        Assert.Contains("Back to More", cut.Markup);
        Assert.Contains("href=\"more\"", cut.Markup);
        Assert.Contains("more-link", cut.Markup);
    }

    [Fact]
    public void Renders_custom_href_and_text()
    {
        var cut = RenderComponent<MobileBackLink>(parameters => parameters
            .Add(p => p.Href, "my-songs")
            .Add(p => p.Text, "← My Songs"));

        Assert.Contains("href=\"my-songs\"", cut.Markup);
        Assert.Contains("← My Songs", cut.Markup);
    }
}
