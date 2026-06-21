using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class InviteFriendsPageTests : AuthPageTestContext
{
    public InviteFriendsPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Shows_unavailable_message_when_invite_not_required()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = false,
                UnavailableReason = "Registration does not require an invite code."
            });

        var cut = RenderComponent<InviteFriends>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Registration does not require an invite code.", cut.Markup));
    }

    [Fact]
    public void Shows_copy_actions_when_invite_can_be_shared()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = true,
                InviteCode = "secret-code"
            });

        var cut = RenderComponent<InviteFriends>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Copy link", cut.Markup);
            Assert.Contains("Copy message", cut.Markup);
            Assert.Contains("/register?invite=secret-code", cut.Markup);
            Assert.Contains("Invite code: secret-code", cut.Markup);
        });
    }

    [Fact]
    public void Copies_message_when_copy_message_clicked()
    {
        Api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = true,
                InviteCode = "abc"
            });

        var cut = RenderComponent<InviteFriends>();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll("button").Count >= 2));
        var expectedMessage = InviteShareFormatting.BuildShareMessage(
            InviteShareFormatting.BuildRegisterUrl(Services.GetRequiredService<NavigationManager>().BaseUri, "abc"),
            "abc");
        JSInterop.SetupVoid("copyTextToClipboard", expectedMessage);

        cut.FindAll("button")[1].Click();

        JSInterop.VerifyInvoke("copyTextToClipboard");
    }
}
