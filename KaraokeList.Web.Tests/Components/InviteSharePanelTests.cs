using Bunit;
using KaraokeList.Web.Components;
using KaraokeList.Web.Tests.Pages;

namespace KaraokeList.Web.Tests.Components;

public sealed class InviteSharePanelTests : AuthPageTestContext
{
    public InviteSharePanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Full_layout_renders_copy_actions()
    {
        var cut = RenderComponent<InviteSharePanel>(parameters => parameters
            .Add(p => p.Layout, InviteSharePanelLayout.Full)
            .Add(p => p.RegisterUrl, "https://example.com/register?invite=abc")
            .Add(p => p.ShareMessage, "Join me at KaraokeList"));

        Assert.Contains("Copy link", cut.Markup);
        Assert.Contains("Copy message", cut.Markup);
        Assert.Contains("https://example.com/register?invite=abc", cut.Markup);
    }

    [Fact]
    public void Banner_layout_renders_link_and_invite_page()
    {
        var cut = RenderComponent<InviteSharePanel>(parameters => parameters
            .Add(p => p.Layout, InviteSharePanelLayout.Banner)
            .Add(p => p.RegisterUrl, "https://example.com/register?invite=abc")
            .Add(p => p.ShareMessage, "Join me"));

        Assert.Contains("Copy link", cut.Markup);
        Assert.Contains("href=\"invite-friends\"", cut.Markup);
    }
}
