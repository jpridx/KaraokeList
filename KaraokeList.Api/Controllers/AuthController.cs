using KaraokeList.Api.Services;
using System.Security.Claims;
using KaraokeList.Data;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    SingerService singerService,
    IJwtTokenService jwtTokenService,
    IRegistrationGate registrationGate,
    IAuthRateLimiter authRateLimiter,
    ICurrentUserSingerResolver currentUserSinger) : ControllerBase
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

        var (token, expires) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse { Token = token, Email = request.Email, SingerId = singerId, ExpiresUtc = expires });
    }

    [AllowAnonymous]
    [HttpGet("registration")]
    public ActionResult<RegistrationInfoDto> GetRegistrationInfo() =>
        Ok(new RegistrationInfoDto
        {
            IsRegistrationOpen = registrationGate.IsRegistrationOpen,
            RequiresInviteCode = registrationGate.RequiresInviteCode
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

        var (token, expires) = jwtTokenService.CreateToken(user);
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

        return Ok(new UserProfileDto { Email = user.Email ?? string.Empty, SingerId = user.SingerId });
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
            var (existingToken, existingExpires) = jwtTokenService.CreateToken(user);
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

        var (token, expires) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email ?? string.Empty,
            SingerId = singerId,
            ExpiresUtc = expires
        });
    }
}
