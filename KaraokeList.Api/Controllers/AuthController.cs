using KaraokeList.Api.Services;
using System.Security.Claims;
using System.Text;
using KaraokeList.Data;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    SingerService singerService,
    SingerListService singerListService,
    IJwtTokenService jwtTokenService,
    IRegistrationGate registrationGate,
    IAuthRateLimiter authRateLimiter,
    ICurrentUserSingerResolver currentUserSinger,
    IAccountEmailSender accountEmailSender,
    IOptions<AppSettings> appSettings) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var clientKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!authRateLimiter.AllowAttempt("register", clientKey, AuthRateLimitPolicies.RegisterMaxAttempts, AuthRateLimitPolicies.RegisterWindow))
        {
            return BadRequest(new ApiErrorResponse { Message = "Too many registration attempts. Try again later." });
        }

        var gateResult = registrationGate.ValidateRegistration(request.Email, request.InviteCode, request.Website);
        if (!gateResult.Allowed)
        {
            return BadRequest(new ApiErrorResponse { Message = gateResult.ErrorMessage });
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", result.Errors.Select(e => e.Description))
            });
        }

        int singerId;
        try
        {
            singerId = await singerService.AddSingerAsync(new Singer { Name = request.Name.Trim() });
            await singerListService.EnsureSystemListsAsync(singerId);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            return BadRequest(new ApiErrorResponse { Message = "Could not create your singer profile. Try again." });
        }

        user.SingerId = singerId;
        var linkResult = await userManager.UpdateAsync(user);
        if (!linkResult.Succeeded)
        {
            await singerService.DeleteSingerAsync(singerId);
            await userManager.DeleteAsync(user);
            return BadRequest(new ApiErrorResponse { Message = "Could not link your account to your singer profile. Try again." });
        }

        var (token, expires) = await CreateAuthTokenAsync(user);
        return Ok(new AuthResponse { Token = token, Email = request.Email, SingerId = singerId, ExpiresUtc = expires });
    }

    [AllowAnonymous]
    [HttpGet("registration")]
    public ActionResult<RegistrationInfoDto> GetRegistrationInfo() =>
        Ok(new RegistrationInfoDto
        {
            IsRegistrationOpen = registrationGate.IsRegistrationOpen,
            RequiresInviteCode = registrationGate.RequiresInviteCode,
            IsPasswordRecoveryAllowed = registrationGate.IsPasswordRecoveryAllowed
        });

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var clientKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!authRateLimiter.AllowAttempt("login", clientKey, AuthRateLimitPolicies.LoginMaxAttempts, AuthRateLimitPolicies.LoginWindow))
        {
            return BadRequest(new ApiErrorResponse { Message = "Too many login attempts. Try again later." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new ApiErrorResponse { Message = "Invalid login attempt." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new ApiErrorResponse { Message = "Invalid login attempt." });
        }

        var (token, expires) = await CreateAuthTokenAsync(user);
        return Ok(new AuthResponse { Token = token, Email = request.Email, SingerId = user.SingerId, ExpiresUtc = expires });
    }

    [Authorize]
    [HttpGet("invite-share")]
    public ActionResult<InviteShareDto> GetInviteShare()
    {
        var availability = registrationGate.GetInviteShareAvailability();
        return Ok(new InviteShareDto
        {
            CanShare = availability.CanShare,
            UnavailableReason = availability.UnavailableReason,
            InviteCode = availability.InviteCode
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var user = await currentUserSinger.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserProfileDto
        {
            Email = user.Email ?? string.Empty,
            SingerId = user.SingerId,
            IsAdmin = await userManager.IsInRoleAsync(user, KaraokeRoles.Admin)
        });
    }

    [Authorize]
    [HttpGet("tickler-settings")]
    public async Task<ActionResult<TicklerSettingsDto>> GetTicklerSettings()
    {
        var user = await currentUserSinger.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(TicklerSettingsResolver.ToDto(user));
    }

    [Authorize]
    [HttpPut("tickler-settings")]
    public async Task<IActionResult> UpdateTicklerSettings([FromBody] UpdateTicklerSettingsRequest request)
    {
        var user = await currentUserSinger.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        user.StaleSongAfterDays = request.StaleAfterDays;
        user.StaleSongLimit = request.SongLimit;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", result.Errors.Select(e => e.Description))
            });
        }

        return NoContent();
    }

    [Authorize]
    [HttpPost("link-singer")]
    public async Task<ActionResult<AuthResponse>> LinkSinger([FromBody] LinkSingerRequest request)
    {
        var user = await currentUserSinger.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (user.SingerId is int existingId)
        {
            var (existingToken, existingExpires) = await CreateAuthTokenAsync(user);
            return Ok(new AuthResponse
            {
                Token = existingToken,
                Email = user.Email ?? string.Empty,
                SingerId = existingId,
                ExpiresUtc = existingExpires
            });
        }

        int singerId;
        if (request.SingerId is int selectedId)
        {
            if (!User.IsAdmin())
            {
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Only admins can link to an existing singer. Enter your name to create a new profile."
                });
            }

            var singers = await singerService.GetSingersAsync();
            if (singers.All(s => s.Id != selectedId))
            {
                return BadRequest(new ApiErrorResponse { Message = "That singer was not found." });
            }

            singerId = selectedId;
        }
        else if (!string.IsNullOrWhiteSpace(request.Name))
        {
            singerId = await singerService.AddSingerAsync(new Singer { Name = request.Name.Trim() });
        }
        else
        {
            return BadRequest(new ApiErrorResponse { Message = "Choose an existing singer or enter your name." });
        }

        user.SingerId = singerId;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new ApiErrorResponse { Message = "Could not link your singer profile." });
        }

        await singerListService.EnsureSystemListsAsync(singerId);

        var (token, expires) = await CreateAuthTokenAsync(user);
        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email ?? string.Empty,
            SingerId = singerId,
            ExpiresUtc = expires
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var clientKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!authRateLimiter.AllowAttempt(
                "change-password",
                clientKey,
                AuthRateLimitPolicies.ChangePasswordMaxAttempts,
                AuthRateLimitPolicies.ChangePasswordWindow))
        {
            return BadRequest(new ApiErrorResponse { Message = "Too many password change attempts. Try again later." });
        }

        var user = await currentUserSinger.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await userManager.HasPasswordAsync(user))
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Your account does not use a password. Contact the site owner for help."
            });
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return BadRequest(new ApiErrorResponse { Message = "Current password is incorrect." });
        }

        var changeResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", changeResult.Errors.Select(e => e.Description))
            });
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!registrationGate.IsPasswordRecoveryAllowed)
        {
            return NotFound(new ApiErrorResponse { Message = "Password recovery is not available." });
        }

        var clientKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!authRateLimiter.AllowAttempt(
                "forgot-password",
                clientKey,
                AuthRateLimitPolicies.ForgotPasswordMaxAttempts,
                AuthRateLimitPolicies.ForgotPasswordWindow))
        {
            return BadRequest(new ApiErrorResponse { Message = "Too many reset requests. Try again later." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && await userManager.HasPasswordAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl = appSettings.Value.WebBaseUrl.TrimEnd('/');
            var resetLink = QueryHelpers.AddQueryString(
                $"{baseUrl}/reset-password",
                new Dictionary<string, string?>
                {
                    ["code"] = encodedCode,
                    ["email"] = request.Email
                });

            await accountEmailSender.SendPasswordResetLinkAsync(user, request.Email, resetLink);
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!registrationGate.IsPasswordRecoveryAllowed)
        {
            return NotFound(new ApiErrorResponse { Message = "Password recovery is not available." });
        }

        var clientKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!authRateLimiter.AllowAttempt(
                "reset-password",
                clientKey,
                AuthRateLimitPolicies.ResetPasswordMaxAttempts,
                AuthRateLimitPolicies.ResetPasswordWindow))
        {
            return BadRequest(new ApiErrorResponse { Message = "Too many reset attempts. Try again later." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid reset request." });
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid reset request." });
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = string.Join(" ", result.Errors.Select(e => e.Description))
            });
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        return NoContent();
    }

    private async Task<(string Token, DateTime ExpiresUtc)> CreateAuthTokenAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return jwtTokenService.CreateToken(user, roles);
    }
}
