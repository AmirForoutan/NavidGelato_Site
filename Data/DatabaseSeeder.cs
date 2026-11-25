using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Data;

public static class DatabaseSeeder
{
    public const string SuperAdminRole = "SuperAdmin";
    public const string DefaultAdminUserName = "admin";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NavidGelato.AdminBootstrap");

        if (!await roles.RoleExistsAsync(SuperAdminRole))
        {
            var roleResult = await roles.CreateAsync(new IdentityRole(SuperAdminRole));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Could not create SuperAdmin role: " +
                    string.Join(" | ", roleResult.Errors.Select(x => x.Description)));
        }

        var admin = await users.FindByNameAsync(DefaultAdminUserName);
        if (admin is null)
        {
            var password = Environment.GetEnvironmentVariable("NAVID_GELATO_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(password))
            {
                // Site startup must not fail just because the optional admin bootstrap
                // password was not configured. Never create a predictable password.
                // Local developers can create the initial admin at /Admin/Setup.
                logger.LogWarning(
                    "No initial admin exists. In Development, open /Admin/Setup from localhost to create one, " +
                    "or set NAVID_GELATO_ADMIN_PASSWORD and restart the application.");
            }
            else
            {
                admin = new ApplicationUser
                {
                    UserName = DefaultAdminUserName,
                    DisplayName = "مدیر اصلی",
                    IsEnabled = true,
                    MustChangePassword = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                var created = await users.CreateAsync(admin, password);
                if (!created.Succeeded)
                    throw new InvalidOperationException("Admin bootstrap password is invalid: " +
                        string.Join(" | ", created.Errors.Select(x => x.Description)));
                logger.LogInformation("Initial admin account created from configured bootstrap password.");
            }
        }

        if (admin is not null && !await users.IsInRoleAsync(admin, SuperAdminRole))
        {
            var added = await users.AddToRoleAsync(admin, SuperAdminRole);
            if (!added.Succeeded)
                throw new InvalidOperationException("Could not assign SuperAdmin role: " +
                    string.Join(" | ", added.Errors.Select(x => x.Description)));
        }

        if (!await db.SmsSettings.AnyAsync())
        {
            db.SmsSettings.Add(new SmsSettings { Enabled = false, UpdatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
    }
}
