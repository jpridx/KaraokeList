using Bunit;
using KaraokeList.Web.Components;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class SortDirectionToggleTests : BunitTestContext
{
    [Fact]
    public void Renders_label_and_invokes_click()
    {
        var clicked = false;
        var cut = RenderComponent<SortDirectionToggle>(parameters => parameters
            .Add(p => p.Label, "↓ Newest first")
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        Assert.Contains("↓ Newest first", cut.Markup);
        cut.Find("button").Click();
        Assert.True(clicked);
    }
}
