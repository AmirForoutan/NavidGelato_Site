using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Security;

public interface IAdminPermissionService
{
    Task<bool> HasAsync(ClaimsPrincipal principal, string permission);
    Task<HashSet<string>> GetPermissionsAsync(ApplicationUser user);
    Task ReplacePermissionsAsync(ApplicationUser user, IEnumerable<string> permissions);
}

public sealed class AdminPermissionService(UserManager<ApplicationUser> userManager) : IAdminPermissionService
{
    public Task<bool> HasAsync(ClaimsPrincipal principal, string permission)
    {
        if (principal.Identity?.IsAuthenticated != true) return Task.FromResult(false);
        if (principal.IsInRole(DatabaseSeeder.SuperAdminRole)) return Task.FromResult(true);
        return Task.FromResult(principal.Claims.Any(x => x.Type == AdminPermissions.ClaimType && x.Value == permission));
    }

    public async Task<HashSet<string>> GetPermissionsAsync(ApplicationUser user)
    {
        if (await userManager.IsInRoleAsync(user, DatabaseSeeder.SuperAdminRole))
            return AdminPermissions.AllKeys.ToHashSet(StringComparer.Ordinal);

        var claims = await userManager.GetClaimsAsync(user);
        return claims.Where(x => x.Type == AdminPermissions.ClaimType)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task ReplacePermissionsAsync(ApplicationUser user, IEnumerable<string> permissions)
    {
        if (await userManager.IsInRoleAsync(user, DatabaseSeeder.SuperAdminRole)) return;

        var current = (await userManager.GetClaimsAsync(user)).Where(x => x.Type == AdminPermissions.ClaimType).ToList();
        if (current.Count > 0) await userManager.RemoveClaimsAsync(user, current);

        var allowed = permissions.Intersect(AdminPermissions.AllKeys, StringComparer.Ordinal).Distinct(StringComparer.Ordinal);
        foreach (var permission in allowed)
            await userManager.AddClaimAsync(user, new Claim(AdminPermissions.ClaimType, permission));

        await userManager.UpdateSecurityStampAsync(user);
    }
}
