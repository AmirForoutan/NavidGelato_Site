using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Pages.Admin;

/// <summary>
/// One-time bootstrap available exclusively in Development from the loopback interface.
/// Production deployments must supply NAVID_GELATO_ADMIN_PASSWORD securely at startup.
/// </summary>
public class SetupModel(
    UserManager<ApplicationUser> users,
    IWebHostEnvironment environment,
    ILogger<SetupModel> logger) : PageModel
{
    [BindProperty] public SetupInput Input { get; set; } = new();

    public sealed class SetupInput
    {
        [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
        [MinLength(8, ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر داشته باشد.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور را وارد کنید.")]
        [Compare(nameof(Password), ErrorMessage = "تکرار رمز عبور مطابقت ندارد.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    private bool IsLocalDevelopment()
    {
        var remote = HttpContext.Connection.RemoteIpAddress;
        if (remote?.IsIPv4MappedToIPv6 == true)
            remote = remote.MapToIPv4();

        return environment.IsDevelopment() && remote is not null && IPAddress.IsLoopback(remote);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsLocalDevelopment())
            return NotFound();

        if (await users.FindByNameAsync(DatabaseSeeder.DefaultAdminUserName) is not null)
            return RedirectToPage("/Admin/Login");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!IsLocalDevelopment())
            return NotFound();

        if (await users.FindByNameAsync(DatabaseSeeder.DefaultAdminUserName) is not null)
            return RedirectToPage("/Admin/Login");

        if (!ModelState.IsValid)
            return Page();

        var admin = new ApplicationUser
        {
            UserName = DatabaseSeeder.DefaultAdminUserName,
            DisplayName = "مدیر اصلی",
            IsEnabled = true,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await users.CreateAsync(admin, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        var added = await users.AddToRoleAsync(admin, DatabaseSeeder.SuperAdminRole);
        if (!added.Succeeded)
        {
            logger.LogError("Initial admin was created but SuperAdmin role assignment failed.");
            ModelState.AddModelError(string.Empty, "حساب ایجاد شد ولی تخصیص نقش مدیر ناموفق بود. برنامه را دوباره اجرا کنید.");
            return Page();
        }

        logger.LogInformation("Initial administrator was created via local development setup.");
        return RedirectToPage("/Admin/Login");
    }
}
