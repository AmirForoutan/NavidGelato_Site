using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;

namespace NavidGelato_Site.Pages.Admin;

public class UsersModel(UserManager<ApplicationUser> userManager, IAdminPermissionService permissions) : PageModel
{
    public List<ApplicationUser> Items { get; private set; } = [];
    public HashSet<string> SelectedPermissions { get; private set; } = [];
    public IReadOnlyList<AdminPermissionDefinition> PermissionDefinitions => AdminPermissions.All;
    public bool EditingSuperAdmin { get; private set; }

    [BindProperty] public UserForm Form { get; set; } = new();
    [BindProperty] public List<string> GrantedPermissions { get; set; } = [];

    public sealed class UserForm
    {
        public string? Id { get; set; }
        [Required, MaxLength(80)] public string UserName { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string DisplayName { get; set; } = string.Empty;
        [Phone, MaxLength(30)] public string? PhoneNumber { get; set; }
        public bool IsEnabled { get; set; } = true;
        [DataType(DataType.Password)] public string? Password { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? editId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Users.View)) return Forbid();
        await LoadAsync();
        if (!string.IsNullOrWhiteSpace(editId))
        {
            var user = await userManager.FindByIdAsync(editId);
            if (user is null || IsProtectedAdmin(user)) return NotFound();
            EditingSuperAdmin = await userManager.IsInRoleAsync(user, DatabaseSeeder.SuperAdminRole);
            SelectedPermissions = await permissions.GetPermissionsAsync(user);
            Form = new UserForm { Id = user.Id, UserName = user.UserName ?? string.Empty, DisplayName = user.DisplayName, PhoneNumber = user.PhoneNumber, IsEnabled = user.IsEnabled };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var isCreate = string.IsNullOrWhiteSpace(Form.Id);
        var requiredPermission = isCreate ? AdminPermissions.Users.Create : AdminPermissions.Users.Edit;
        if (!await permissions.HasAsync(User, requiredPermission)) return Forbid();
        if (isCreate && string.IsNullOrWhiteSpace(Form.Password)) ModelState.AddModelError("Form.Password", "برای کاربر جدید رمز عبور وارد کنید.");

        GrantedPermissions = AdminPermissions.Normalize(GrantedPermissions).ToList();
        if (!ModelState.IsValid) { await LoadAsync(); SelectedPermissions = GrantedPermissions.ToHashSet(StringComparer.Ordinal); return Page(); }

        ApplicationUser user;
        if (isCreate)
        {
            if (string.Equals(Form.UserName.Trim(), DatabaseSeeder.DefaultAdminUserName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Form.UserName", "این نام کاربری رزرو شده است.");
                await LoadAsync();
                SelectedPermissions = GrantedPermissions.ToHashSet(StringComparer.Ordinal);
                return Page();
            }

            user = new ApplicationUser { UserName = Form.UserName.Trim(), DisplayName = Form.DisplayName.Trim(), PhoneNumber = Clean(Form.PhoneNumber), IsEnabled = Form.IsEnabled, MustChangePassword = true, CreatedAtUtc = DateTime.UtcNow };
            var result = await userManager.CreateAsync(user, Form.Password!);
            if (!result.Succeeded) return await IdentityErrorsAsync(result);
        }
        else
        {
            user = await userManager.FindByIdAsync(Form.Id!) ?? throw new InvalidOperationException("User not found.");
            if (IsProtectedAdmin(user)) return NotFound();
            var superAdmin = await userManager.IsInRoleAsync(user, DatabaseSeeder.SuperAdminRole);
            user.DisplayName = Form.DisplayName.Trim();
            user.PhoneNumber = Clean(Form.PhoneNumber);
            if (!superAdmin) { user.UserName = Form.UserName.Trim(); user.IsEnabled = Form.IsEnabled; }
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return await IdentityErrorsAsync(result);
        }

        if (await permissions.HasAsync(User, AdminPermissions.Users.ManagePermissions)) await permissions.ReplacePermissionsAsync(user, GrantedPermissions);
        TempData["Success"] = "اطلاعات کاربر و دسترسی‌ها ذخیره شد.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string newPassword)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Users.ResetPassword)) return Forbid();
        if (string.IsNullOrWhiteSpace(newPassword)) { TempData["Error"] = "رمز جدید را وارد کنید."; return RedirectToPage(new { editId = userId }); }
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || IsProtectedAdmin(user)) return NotFound();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded) { TempData["Error"] = string.Join(" | ", result.Errors.Select(x => x.Description)); return RedirectToPage(new { editId = userId }); }
        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);
        await userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = "رمز کاربر تغییر کرد و در ورود بعدی باید رمز جدیدی انتخاب کند.";
        return RedirectToPage(new { editId = userId });
    }

    public async Task<IActionResult> OnPostUnlockAsync(string userId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Users.Unlock)) return Forbid();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || IsProtectedAdmin(user)) return NotFound();
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = $"قفل حساب {user.UserName} باز شد و شمارنده ورودهای ناموفق هم صفر شد.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Users.Delete)) return Forbid();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || IsProtectedAdmin(user)) return NotFound();
        if (await userManager.IsInRoleAsync(user, DatabaseSeeder.SuperAdminRole)) { TempData["Error"] = "مدیر اصلی سیستم قابل حذف نیست."; return RedirectToPage(); }
        if (userManager.GetUserId(User) == user.Id) { TempData["Error"] = "نمی‌توانید حسابی که با آن وارد شده‌اید حذف کنید."; return RedirectToPage(); }
        await userManager.DeleteAsync(user);
        TempData["Success"] = "کاربر حذف شد.";
        return RedirectToPage();
    }

    public bool IsLocked(ApplicationUser user) => user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
    private static bool IsProtectedAdmin(ApplicationUser user) => string.Equals(user.UserName, DatabaseSeeder.DefaultAdminUserName, StringComparison.OrdinalIgnoreCase);

    private async Task LoadAsync() => Items = await userManager.Users.AsNoTracking()
        .Where(x => x.UserName != DatabaseSeeder.DefaultAdminUserName)
        .OrderBy(x => x.UserName)
        .ToListAsync();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<IActionResult> IdentityErrorsAsync(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        await LoadAsync(); SelectedPermissions = GrantedPermissions.ToHashSet(StringComparer.Ordinal); return Page();
    }
}
