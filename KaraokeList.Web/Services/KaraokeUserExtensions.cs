using System.Security.Claims;
using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class KaraokeUserExtensions
{
    public static int? GetSingerId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(KaraokeClaimTypes.SingerId)?.Value;
        return int.TryParse(value, out var singerId) ? singerId : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(KaraokeRoles.Admin);
}
