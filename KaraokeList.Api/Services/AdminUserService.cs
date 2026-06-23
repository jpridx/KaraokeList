using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync();
    Task<(bool Succeeded, string? Error)> UpdateUserAsync(UpdateAdminUserRequest request, string actingUserId);
}

public sealed class AdminUserService(
    UserManager<ApplicationUser> userManager,
    SingerService singerService) : IAdminUserService
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync()
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync();

        var singers = (await singerService.GetSingersAsync())
            .ToDictionary(s => s.Id, s => s.Name);

        var result = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new AdminUserDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                SingerId = user.SingerId,
                SingerName = user.SingerId is int singerId && singers.TryGetValue(singerId, out var name)
                    ? name
                    : null,
                IsAdmin = roles.Contains(KaraokeRoles.Admin)
            });
        }

        return result;
    }

    public async Task<(bool Succeeded, string? Error)> UpdateUserAsync(
        UpdateAdminUserRequest request,
        string actingUserId)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        var isAdmin = await userManager.IsInRoleAsync(user, KaraokeRoles.Admin);

        if (isAdmin && !request.IsAdmin && user.Id == actingUserId)
        {
            return (false, "You cannot remove your own admin role.");
        }

        if (isAdmin && !request.IsAdmin)
        {
            var adminCount = (await userManager.GetUsersInRoleAsync(KaraokeRoles.Admin)).Count;
            if (adminCount <= 1)
            {
                return (false, "At least one admin must remain.");
            }
        }

        if (request.SingerId is int singerId)
        {
            var singers = await singerService.GetSingersAsync();
            if (singers.All(s => s.Id != singerId))
            {
                return (false, "That singer was not found.");
            }

            user.SingerId = singerId;
        }
        else
        {
            user.SingerId = null;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, string.Join(" ", updateResult.Errors.Select(e => e.Description)));
        }

        if (request.IsAdmin && !isAdmin)
        {
            var addResult = await userManager.AddToRoleAsync(user, KaraokeRoles.Admin);
            if (!addResult.Succeeded)
            {
                return (false, string.Join(" ", addResult.Errors.Select(e => e.Description)));
            }
        }
        else if (!request.IsAdmin && isAdmin)
        {
            var removeResult = await userManager.RemoveFromRoleAsync(user, KaraokeRoles.Admin);
            if (!removeResult.Succeeded)
            {
                return (false, string.Join(" ", removeResult.Errors.Select(e => e.Description)));
            }
        }

        return (true, null);
    }
}
