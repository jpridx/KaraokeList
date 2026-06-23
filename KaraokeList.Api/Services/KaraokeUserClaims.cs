using System.Security.Claims;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Identity;

namespace KaraokeList.Api.Services;

public static class KaraokeUserClaims
{
    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(KaraokeRoles.Admin);

    public static async Task<bool> IsAdminAsync(
        this UserManager<ApplicationUser> userManager,
        ApplicationUser user) =>
        await userManager.IsInRoleAsync(user, KaraokeRoles.Admin);
}
