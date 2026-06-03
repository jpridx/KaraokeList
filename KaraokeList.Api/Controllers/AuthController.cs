using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IRegistrationGate registrationGate,
    IAuthRateLimiter authRateLimiter) : ControllerBase
{
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

        var (token, expires) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse { Token = token, Email = request.Email, ExpiresUtc = expires });
    }

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
        return Ok(new AuthResponse { Token = token, Email = request.Email, ExpiresUtc = expires });
    }
}
