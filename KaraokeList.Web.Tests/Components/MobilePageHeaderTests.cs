using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class MobilePageHeaderTests : BunitTestContext
{
    [Fact]
    public void Renders_title_and_subtitle()
    {
        var cut = Render<MobilePageHeader>(parameters => parameters
            .Add(p => p.Title, "My Songs")
            .Add(p => p.Subtitle, builder => builder.AddMarkupContent(0, "Browse your lists.")));

        Assert.Contains("My Songs", cut.Markup);
        Assert.Contains("Browse your lists.", cut.Markup);
        Assert.Contains("mobile-page-header", cut.Markup);
    }

    [Fact]
    public void Renders_leading_content_before_title()
    {
        var cut = Render<MobilePageHeader>(parameters => parameters
            .Add(p => p.Title, "Song title")
            .Add(p => p.TitleClass, "mb-0")
            .Add(p => p.LeadingContent, builder => builder.AddMarkupContent(0, "<button>Back</button>"))
            .Add(p => p.Subtitle, builder => builder.AddMarkupContent(0, "Artist name")));

        Assert.Contains("Back", cut.Markup);
        Assert.Contains("Song title", cut.Markup);
        Assert.Contains("Artist name", cut.Markup);
    }

    [Fact]
    public void Renders_back_link_when_back_href_set()
    {
        var cut = Render<MobilePageHeader>(parameters => parameters
            .Add(p => p.Title, "Preferences")
            .Add(p => p.BackHref, "more")
            .Add(p => p.BackText, "← Back to More"));

        Assert.Contains("href=\"more\"", cut.Markup);
        Assert.Contains("← Back to More", cut.Markup);
    }
}
