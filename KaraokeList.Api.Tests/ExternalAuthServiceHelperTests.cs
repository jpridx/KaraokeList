using System.Security.Claims;
using KaraokeList.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace KaraokeList.Api.Tests;

public sealed class ExternalAuthServiceHelperTests
{
    [Fact]
    public void IsEmailVerified_returns_true_for_google_when_email_present_without_verified_claim()
    {
        var loginInfo = CreateLoginInfo(GoogleDefaults.AuthenticationScheme, "user@gmail.com", includeEmailVerified: false);

        Assert.True(ExternalAuthService.IsEmailVerified(loginInfo));
    }

    [Fact]
    public void IsEmailVerified_returns_false_for_google_when_email_verified_is_false()
    {
        var loginInfo = CreateLoginInfo(
            GoogleDefaults.AuthenticationScheme,
            "user@gmail.com",
            includeEmailVerified: true,
            emailVerifiedValue: "false");

        Assert.False(ExternalAuthService.IsEmailVerified(loginInfo));
    }

    [Fact]
    public void GetEmail_uses_upn_when_email_claim_missing()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Upn, "user@outlook.com"),
            new(ClaimTypes.Name, "Test User")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Microsoft"));
        var loginInfo = new ExternalLoginInfo(principal, "Microsoft", "key", "Microsoft");

        Assert.Equal("user@outlook.com", ExternalAuthService.GetEmail(loginInfo));
    }

    [Fact]
    public void GetEmail_ignores_preferred_username_without_at_sign()
    {
        var claims = new List<Claim>
        {
            new("preferred_username", "not-an-email"),
            new(ClaimTypes.Name, "Test User")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Microsoft"));
        var loginInfo = new ExternalLoginInfo(principal, "Microsoft", "key", "Microsoft");

        Assert.Null(ExternalAuthService.GetEmail(loginInfo));
    }

    private static ExternalLoginInfo CreateLoginInfo(
        string provider,
        string email,
        bool includeEmailVerified,
        string emailVerifiedValue = "true")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, "Test User")
        };

        if (includeEmailVerified)
        {
            claims.Add(new Claim("email_verified", emailVerifiedValue));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: provider));
        return new ExternalLoginInfo(principal, provider, $"key-{email}", provider);
    }
}
