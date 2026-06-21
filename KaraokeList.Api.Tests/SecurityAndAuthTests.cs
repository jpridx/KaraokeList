using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KaraokeList.Api.Tests;

public class RegistrationGateTests
{
    private static RegistrationGate CreateGate(RegistrationSettings settings) =>
        new(Options.Create(settings));

    [Fact]
    public void ValidateRegistration_WhenClosed_ReturnsNotAccepted()
    {
        var gate = CreateGate(new RegistrationSettings { AllowRegistration = false });

        var result = gate.ValidateRegistration("user@example.com", null, null);

        Assert.False(result.Allowed);
        Assert.Equal("New accounts are not being accepted.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRegistration_WhenHoneypotFilled_ReturnsGenericError()
    {
        var gate = CreateGate(new RegistrationSettings { RequireInviteCode = false });

        var result = gate.ValidateRegistration("user@example.com", null, "https://spam.example");

        Assert.False(result.Allowed);
        Assert.Equal("Unable to create account. Please try again later.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRegistration_WhenInviteRequiredAndMissing_ReturnsInvalidInvite()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            RequireInviteCode = true,
            InviteCode = "secret-code"
        });

        var result = gate.ValidateRegistration("user@example.com", null, null);

        Assert.False(result.Allowed);
        Assert.Equal("Invalid invite code.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRegistration_WhenInviteWrong_ReturnsInvalidInvite()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            RequireInviteCode = true,
            InviteCode = "secret-code"
        });

        var result = gate.ValidateRegistration("user@example.com", "wrong", null);

        Assert.False(result.Allowed);
        Assert.Equal("Invalid invite code.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRegistration_WhenInviteRequiredButNotConfigured_ReturnsNotConfigured()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            RequireInviteCode = true,
            InviteCode = ""
        });

        var result = gate.ValidateRegistration("user@example.com", "anything", null);

        Assert.False(result.Allowed);
        Assert.Equal("Registration is not configured. Contact the site owner.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRegistration_WhenInviteMatches_ReturnsAllowed()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            RequireInviteCode = true,
            InviteCode = "secret-code"
        });

        var result = gate.ValidateRegistration("user@example.com", "  secret-code  ", null);

        Assert.True(result.Allowed);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateRegistration_WhenEmailDomainNotAllowed_ReturnsDomainError()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            RequireInviteCode = false,
            AllowedEmailDomains = ["gmail.com"]
        });

        var result = gate.ValidateRegistration("user@outlook.com", null, null);

        Assert.False(result.Allowed);
        Assert.Equal("That email domain is not allowed to register.", result.ErrorMessage);
    }

    [Fact]
    public void RequiresInviteCode_WhenRegistrationOpenAndRequired_IsTrue()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            AllowRegistration = true,
            RequireInviteCode = true
        });

        Assert.True(gate.RequiresInviteCode);
    }

    [Fact]
    public void RequiresInviteCode_WhenRegistrationClosed_IsFalse()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            AllowRegistration = false,
            RequireInviteCode = true
        });

        Assert.False(gate.RequiresInviteCode);
    }

    [Fact]
    public void GetInviteShareAvailability_WhenConfigured_ReturnsInviteCode()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            AllowRegistration = true,
            RequireInviteCode = true,
            InviteCode = "secret-code"
        });

        var availability = gate.GetInviteShareAvailability();

        Assert.True(availability.CanShare);
        Assert.Equal("secret-code", availability.InviteCode);
        Assert.Null(availability.UnavailableReason);
    }

    [Fact]
    public void GetInviteShareAvailability_WhenRegistrationClosed_ReturnsReason()
    {
        var gate = CreateGate(new RegistrationSettings { AllowRegistration = false });

        var availability = gate.GetInviteShareAvailability();

        Assert.False(availability.CanShare);
        Assert.Contains("Registration is closed", availability.UnavailableReason);
    }

    [Fact]
    public void GetInviteShareAvailability_WhenInviteNotRequired_ReturnsReason()
    {
        var gate = CreateGate(new RegistrationSettings
        {
            AllowRegistration = true,
            RequireInviteCode = false
        });

        var availability = gate.GetInviteShareAvailability();

        Assert.False(availability.CanShare);
        Assert.Contains("does not require an invite code", availability.UnavailableReason);
    }
}

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() =>
        new(Options.Create(new JwtSettings
        {
            Issuer = "KaraokeList.Test",
            Audience = "KaraokeList.Web.Test",
            Key = "UNIT_TEST_JWT_SIGNING_KEY_32_CHARS!",
            ExpirationHours = 2
        }));

    [Fact]
    public void CreateToken_IncludesStandardClaims()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "singer@example.com",
            Email = "singer@example.com"
        };

        var (token, expires) = CreateService().CreateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expires > DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("KaraokeList.Test", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "KaraokeList.Web.Test");
        Assert.Equal("user-1", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("singer@example.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
    }

    [Fact]
    public void CreateToken_WithSingerId_IncludesSingerClaim()
    {
        var user = new ApplicationUser
        {
            Id = "user-2",
            UserName = "singer@example.com",
            Email = "singer@example.com",
            SingerId = 42
        };

        var (token, _) = CreateService().CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == KaraokeClaimTypes.SingerId).Value);
    }
}

public class AuthRateLimiterTests
{
    [Fact]
    public void AllowAttempt_UnderLimit_ReturnsTrue()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var limiter = new AuthRateLimiter(cache);

        Assert.True(limiter.AllowAttempt("login", "client-a", 3, TimeSpan.FromMinutes(1)));
        Assert.True(limiter.AllowAttempt("login", "client-a", 3, TimeSpan.FromMinutes(1)));
        Assert.True(limiter.AllowAttempt("login", "client-a", 3, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void AllowAttempt_AtLimit_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var limiter = new AuthRateLimiter(cache);

        Assert.True(limiter.AllowAttempt("register", "client-b", 2, TimeSpan.FromMinutes(1)));
        Assert.True(limiter.AllowAttempt("register", "client-b", 2, TimeSpan.FromMinutes(1)));
        Assert.False(limiter.AllowAttempt("register", "client-b", 2, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void AllowAttempt_DifferentClients_AreIndependent()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var limiter = new AuthRateLimiter(cache);

        Assert.True(limiter.AllowAttempt("login", "client-c", 1, TimeSpan.FromMinutes(1)));
        Assert.False(limiter.AllowAttempt("login", "client-c", 1, TimeSpan.FromMinutes(1)));
        Assert.True(limiter.AllowAttempt("login", "client-d", 1, TimeSpan.FromMinutes(1)));
    }
}
