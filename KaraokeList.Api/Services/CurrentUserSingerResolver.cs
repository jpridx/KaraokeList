using System.Security.Claims;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Identity;

namespace KaraokeList.Api.Services;

public interface ICurrentUserSingerResolver
{
    Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal);
    Task<int?> GetSingerIdAsync(ClaimsPrincipal principal);
}

public sealed class CurrentUserSingerResolver(UserManager<ApplicationUser> userManager) : ICurrentUserSingerResolver
{
    public async Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        return await userManager.GetUserAsync(principal);
    }

    public async Task<int?> GetSingerIdAsync(ClaimsPrincipal principal)
    {
        if (int.TryParse(principal.FindFirstValue(KaraokeClaimTypes.SingerId), out var fromClaim))
        {
            return fromClaim;
        }

        var user = await GetUserAsync(principal);
        return user?.SingerId;
    }
}
