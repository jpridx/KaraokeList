using System.Net;
using System.Net.Http.Json;
using KaraokeList.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class PasswordRecoveryIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task ForgotPassword_ForUnknownEmail_ReturnsNoContent()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest
        {
            Email = $"missing-{Guid.NewGuid():N}@example.com"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [SkippableFact]
    public async Task ForgotPassword_ForRegisteredUser_SendsResetLink()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var registerClient = factory.CreateClient();
        var email = $"recover-{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndGetTokenAsync(registerClient, email);

        using var scope = factory.Services.CreateScope();
        var emailSender = scope.ServiceProvider.GetRequiredService<CapturingAccountEmailSender>();

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = email });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.Equal(email, emailSender.LastEmail);
        Assert.NotNull(emailSender.LastResetLink);
        Assert.Contains("/reset-password", emailSender.LastResetLink, StringComparison.Ordinal);
        Assert.Contains("code=", emailSender.LastResetLink, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task ResetPassword_Succeeds_AndNewPasswordWorksForLogin()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var registerClient = factory.CreateClient();
        var email = $"recover-{Guid.NewGuid():N}@example.com";
        await IntegrationAuthHelper.RegisterAndGetTokenAsync(registerClient, email);

        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = email });

        using var scope = factory.Services.CreateScope();
        var emailSender = scope.ServiceProvider.GetRequiredService<CapturingAccountEmailSender>();
        Assert.NotNull(emailSender.LastResetLink);

        var uri = new Uri(emailSender.LastResetLink);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var code = query["code"].ToString();
        Assert.False(string.IsNullOrEmpty(code));

        const string newPassword = "NewPassw0rd!99";
        var resetResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Code = code!,
            Password = newPassword,
            ConfirmPassword = newPassword
        });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = IntegrationAuthHelper.TestPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }
}
