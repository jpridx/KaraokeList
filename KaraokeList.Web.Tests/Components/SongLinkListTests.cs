using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class SongLinkListTests : BunitTestContext
{
    [Fact]
    public void Renders_links_for_each_item()
    {
        var cut = Render<SongLinkList>(parameters => parameters
            .Add(p => p.Items, new[]
            {
                new SongLinkItem(1, "Jeopardy — The Greg Kihn Band", "Never performed")
            }));

        Assert.Contains("href=\"log?songId=1\"", cut.Markup);
        Assert.Contains("Jeopardy — The Greg Kihn Band", cut.Markup);
        Assert.Contains("Never performed", cut.Markup);
    }
}
