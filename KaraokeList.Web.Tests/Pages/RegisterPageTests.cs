using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class RegisterPageTests : AuthPageTestContext
{
    [Fact]
    public void Shows_error_when_registration_info_unavailable()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync()).ReturnsAsync((RegistrationInfoDto?)null);

        var cut = RenderComponent<Register>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Could not load registration settings", cut.Markup));
    }

    [Fact]
    public void Shows_closed_message_when_registration_disabled()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto { IsRegistrationOpen = false });

        var cut = RenderComponent<Register>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Registration closed", cut.Markup));
    }

    [Fact]
    public void Shows_invite_field_when_required()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto
            {
                IsRegistrationOpen = true,
                RequiresInviteCode = true
            });

        var cut = RenderComponent<Register>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Invite code", cut.Markup);
            Assert.Contains("Invite-only", cut.Markup);
        });
    }

    [Fact]
    public void Shows_error_when_registration_fails()
    {
        SetupOpenRegistration(requiresInvite: false);
        Api.Setup(client => client.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(AuthResult.Fail("Invalid invite code."));

        var cut = RenderComponent<Register>();
        WaitForForm(cut);
        SubmitRegistration(cut, requiresInvite: false);

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            Assert.Contains("Invalid invite code.", alert.TextContent);
        });
    }

    [Fact]
    public void Shows_transient_warning_when_registration_hits_cold_start()
    {
        SetupOpenRegistration(requiresInvite: false);
        Api.Setup(client => client.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(AuthResult.Fail(ApiTransientFailure.ColdStartMessage, transient: true));

        var cut = RenderComponent<Register>();
        WaitForForm(cut);
        SubmitRegistration(cut, requiresInvite: false);

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-warning");
            Assert.Contains(ApiTransientFailure.ColdStartMessage, alert.TextContent);
        });
    }

    [Fact]
    public async Task Navigates_home_and_stores_token_when_registration_succeeds()
    {
        var token = CreateTestToken();
        SetupOpenRegistration(requiresInvite: false);
        Api.Setup(client => client.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(AuthResult.Ok(new AuthResponse
            {
                Token = token,
                Email = "user@example.com",
                SingerId = 3,
                ExpiresUtc = DateTime.UtcNow.AddHours(1)
            }));

        var cut = RenderComponent<Register>();
        WaitForForm(cut);
        SubmitRegistration(cut, requiresInvite: false);

        cut.WaitForAssertion(() =>
        {
            var nav = Services.GetRequiredService<NavigationManager>();
            Assert.Equal("http://localhost/", nav.Uri);
        });

        var storedToken = await GetStoredTokenAsync();
        Assert.Equal(token, storedToken);
    }

    private void SetupOpenRegistration(bool requiresInvite)
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto
            {
                IsRegistrationOpen = true,
                RequiresInviteCode = requiresInvite
            });
    }

    private static void WaitForForm(IRenderedComponent<Register> cut)
    {
        cut.WaitForAssertion(() => cut.Find("button[type=submit]"));
    }

    private static void SubmitRegistration(IRenderedComponent<Register> cut, bool requiresInvite)
    {
        var fieldIndex = 0;
        cut.FindAll("input.form-control")[fieldIndex++].Change("Test Singer");
        cut.FindAll("input.form-control")[fieldIndex++].Change("user@example.com");
        if (requiresInvite)
        {
            cut.FindAll("input.form-control")[fieldIndex++].Change("invite-code");
        }

        cut.FindAll("input.form-control")[fieldIndex++].Change("TestPassw0rd!23");
        cut.FindAll("input.form-control")[fieldIndex].Change("TestPassw0rd!23");
        cut.Find("button[type=submit]").Click();
    }
}
