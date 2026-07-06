using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class LoginPageTests : AuthPageTestContext
{
    [Fact]
    public void Renders_sign_in_form()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto());

        var cut = Render<Login>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sign in", cut.Markup);
            cut.Find("button[type=submit]");
        });
    }

    [Fact]
    public void Shows_forgot_password_link_when_recovery_allowed()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto { IsPasswordRecoveryAllowed = true });

        var cut = Render<Login>();

        cut.WaitForAssertion(() =>
        {
            var link = cut.Find("a[href='forgot-password']");
            Assert.Contains("Forgot your password?", link.TextContent);
        });
    }

    [Fact]
    public void Shows_error_when_login_fails()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto());
        Api.Setup(client => client.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(AuthResult.Fail("Invalid login attempt."));

        var cut = Render<Login>();
        SubmitLogin(cut, "user@example.com", "wrong-password");

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            Assert.Contains("Invalid login attempt.", alert.TextContent);
        });
    }

    [Fact]
    public void Shows_transient_warning_when_cold_start_fails()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto());
        Api.Setup(client => client.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(AuthResult.Fail(ApiTransientFailure.ColdStartMessage, transient: true));

        var cut = Render<Login>();
        SubmitLogin(cut, "user@example.com", "TestPassw0rd!23");

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-warning");
            Assert.Contains(ApiTransientFailure.ColdStartMessage, alert.TextContent);
        });
    }

    [Fact]
    public async Task Navigates_home_and_stores_token_when_login_succeeds()
    {
        Api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto());
        var token = CreateTestToken();
        Api.Setup(client => client.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(AuthResult.Ok(new AuthResponse
            {
                Token = token,
                Email = "user@example.com",
                ExpiresUtc = DateTime.UtcNow.AddHours(1)
            }));

        var cut = Render<Login>();
        SubmitLogin(cut, "user@example.com", "TestPassw0rd!23");

        cut.WaitForAssertion(() =>
        {
            var nav = Services.GetRequiredService<NavigationManager>();
            Assert.Equal("http://localhost/", nav.Uri);
        });

        var storedToken = await GetStoredTokenAsync();
        Assert.Equal(token, storedToken);
    }

    private static void SubmitLogin(IRenderedComponent<Login> cut, string email, string password)
    {
        cut.FindAll("input.form-control")[0].Change(email);
        cut.FindAll("input.form-control")[1].Change(password);
        cut.Find("button[type=submit]").Click();
    }
}
