using System.Security.Claims;
using KaraokeList.Data;
using KaraokeList.Security;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;

namespace KaraokeList.Api.Services;

public sealed record ExternalAuthProcessResult(
    bool Succeeded,
    ApplicationUser? User,
    string? ErrorMessage)
{
    public static ExternalAuthProcessResult Ok(ApplicationUser user) => new(true, user, null);

    public static ExternalAuthProcessResult Fail(string message) => new(false, null, message);
}

public interface IExternalAuthService
{
    Task<ExternalAuthProcessResult> ProcessExternalLoginAsync(ExternalLoginInfo loginInfo, string? inviteCode);
}

public sealed class ExternalAuthService(
    UserManager<ApplicationUser> userManager,
    SingerService singerService,
    SingerListService singerListService,
    IRegistrationGate registrationGate) : IExternalAuthService
{
    public async Task<ExternalAuthProcessResult> ProcessExternalLoginAsync(
        ExternalLoginInfo loginInfo,
        string? inviteCode)
    {
        var email = GetEmail(loginInfo);
        if (string.IsNullOrWhiteSpace(email))
        {
            return ExternalAuthProcessResult.Fail("Could not read an email address from your sign-in provider.");
        }

        email = email.Trim();

        var user = await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
        if (user is not null)
        {
            return ExternalAuthProcessResult.Ok(user);
        }

        user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            if (!IsEmailVerified(loginInfo))
            {
                return ExternalAuthProcessResult.Fail(
                    "Your provider email is not verified. Sign in with your password or verify your email with the provider.");
            }

            var linkResult = await userManager.AddLoginAsync(user, loginInfo);
            if (!linkResult.Succeeded)
            {
                return ExternalAuthProcessResult.Fail(
                    string.Join(" ", linkResult.Errors.Select(error => error.Description)));
            }

            return ExternalAuthProcessResult.Ok(user);
        }

        var gateResult = registrationGate.ValidateRegistration(email, inviteCode, honeypot: null);
        if (!gateResult.Allowed)
        {
            return ExternalAuthProcessResult.Fail(gateResult.ErrorMessage ?? "Registration is not allowed.");
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = IsEmailVerified(loginInfo)
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return ExternalAuthProcessResult.Fail(
                string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
        if (!addLoginResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ExternalAuthProcessResult.Fail(
                string.Join(" ", addLoginResult.Errors.Select(error => error.Description)));
        }

        var displayName = GetDisplayName(loginInfo) ?? email.Split('@')[0];
        int singerId;
        try
        {
            singerId = await singerService.AddSingerAsync(new Singer { Name = displayName.Trim() });
            await singerListService.EnsureSystemListsAsync(singerId);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            return ExternalAuthProcessResult.Fail("Could not create your singer profile. Try again.");
        }

        user.SingerId = singerId;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await singerService.DeleteSingerAsync(singerId);
            await userManager.DeleteAsync(user);
            return ExternalAuthProcessResult.Fail("Could not link your account to your singer profile. Try again.");
        }

        return ExternalAuthProcessResult.Ok(user);
    }

    internal static string? GetEmail(ExternalLoginInfo loginInfo)
    {
        var principal = loginInfo.Principal;
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue("preferred_username");
    }

    internal static bool IsEmailVerified(ExternalLoginInfo loginInfo)
    {
        var verified = loginInfo.Principal.FindFirstValue("email_verified");
        if (!string.IsNullOrWhiteSpace(verified))
        {
            return string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(loginInfo.LoginProvider, MicrosoftAccountDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return !string.IsNullOrWhiteSpace(GetEmail(loginInfo));
        }

        return false;
    }

    internal static string? GetDisplayName(ExternalLoginInfo loginInfo) =>
        loginInfo.Principal.FindFirstValue(ClaimTypes.Name)
        ?? loginInfo.Principal.FindFirstValue("name");
}
