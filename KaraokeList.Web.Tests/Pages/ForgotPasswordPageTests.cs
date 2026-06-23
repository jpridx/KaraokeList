using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class ForgotPasswordPageTests : AuthPageTestContext
{
    [Fact]
    public void Renders_form_when_recovery_allowed()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto { IsPasswordRecoveryAllowed = true });

        var cut = RenderComponent<ForgotPassword>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Forgot your password?", cut.Markup);
            cut.Find("button[type=submit]");
        });
    }

    [Fact]
    public void Navigates_to_confirmation_when_request_succeeds()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto { IsPasswordRecoveryAllowed = true });
        Api.Setup(client => client.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>()))
            .ReturnsAsync(PasswordRecoveryResult.Ok());

        var cut = RenderComponent<ForgotPassword>();
        cut.Find("input.form-control").Change("user@example.com");
        cut.Find("button[type=submit]").Click();

        cut.WaitForAssertion(() =>
        {
            var nav = cut.Services.GetRequiredService<NavigationManager>();
            Assert.Equal("http://localhost/forgot-password-confirmation", nav.Uri);
        });
    }
}
