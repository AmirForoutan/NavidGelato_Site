using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Models;
namespace NavidGelato_Site.Data;
public static class DatabaseSeeder {
public const string SuperAdminRole="SuperAdmin";
public const string DefaultAdminUserName="admin";
public static async Task InitializeAsync(IServiceProvider services) {
using var scope=services.CreateScope();
var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
if(!await roles.RoleExistsAsync(SuperAdminRole)) await roles.CreateAsync(new IdentityRole(SuperAdminRole));
var admin=await users.FindByNameAsync(DefaultAdminUserName);
if(admin is null) {
var password=Environment.GetEnvironmentVariable("NAVID_GELATO_ADMIN_PASSWORD");
if(string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Set NAVID_GELATO_ADMIN_PASSWORD before first startup.");
admin=new ApplicationUser {UserName=DefaultAdminUserName,DisplayName="مدیر اصلی",IsEnabled=true,MustChangePassword=true,CreatedAtUtc=DateTime.UtcNow,EmailConfirmed=true};
var created=await users.CreateAsync(admin,password);
if(!created.Succeeded) throw new InvalidOperationException(string.Join(" | ",created.Errors.Select(x=>x.Description)));
}
if(!await users.IsInRoleAsync(admin,SuperAdminRole)) await users.AddToRoleAsync(admin,SuperAdminRole);
if(!await db.SmsSettings.AnyAsync()){db.SmsSettings.Add(new SmsSettings{Enabled=false,UpdatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();}
}
}