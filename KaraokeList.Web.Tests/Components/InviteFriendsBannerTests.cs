using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class InviteFriendsBannerTests : AuthPageTestContext
{
    public InviteFriendsBannerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_nothing_when_invite_sharing_unavailable_and_registration_closed()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = false,
                UnavailableReason = "Registration is closed."
            });
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto { IsRegistrationOpen = false });

        var cut = RenderComponent<InviteFriendsBanner>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("invite-friends-banner", cut.Markup));
    }

    [Fact]
    public void Shows_invite_banner_when_sharing_is_available()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = true,
                InviteCode = "secret-code"
            });

        var cut = RenderComponent<InviteFriendsBanner>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Invite a friend", cut.Markup);
            Assert.Contains("Copy link", cut.Markup);
            Assert.Contains("invite-friends", cut.Markup);
        });
    }

    [Fact]
    public void Copies_registration_link_when_copy_clicked()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = true,
                InviteCode = "abc"
            });

        var cut = RenderComponent<InviteFriendsBanner>();
        var expectedUrl = InviteShareFormatting.BuildRegisterUrl(
            cut.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().BaseUri,
            "abc");
        JSInterop.SetupVoid("copyTextToClipboard", expectedUrl);

        cut.WaitForAssertion(() => cut.Find("button"));

        cut.Find("button").Click();

        JSInterop.VerifyInvoke("copyTextToClipboard");
    }

    [Fact]
    public void Shows_open_registration_banner_when_invite_not_required()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = false,
                UnavailableReason = "Registration does not require an invite code."
            });
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto
            {
                IsRegistrationOpen = true,
                RequiresInviteCode = false
            });

        var cut = RenderComponent<InviteFriendsBanner>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Registration is open", cut.Markup);
            Assert.Contains("href=\"register\"", cut.Markup);
        });
    }
}
