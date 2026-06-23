using KaraokeList.Api.Services;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = KaraokeRoles.Admin)]
public class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAll() =>
        Ok((await adminUserService.GetUsersAsync()).ToList());

    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(string userId, [FromBody] UpdateAdminUserRequest request)
    {
        if (!string.Equals(userId, request.UserId, StringComparison.Ordinal))
        {
            return BadRequest(new ApiErrorResponse { Message = "User id mismatch." });
        }

        var actingUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(actingUserId))
        {
            return Unauthorized();
        }

        var (succeeded, error) = await adminUserService.UpdateUserAsync(request, actingUserId);
        if (!succeeded)
        {
            return BadRequest(new ApiErrorResponse { Message = error });
        }

        return NoContent();
    }
}
