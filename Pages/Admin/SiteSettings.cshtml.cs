using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;

namespace NavidGelato_Site.Pages.Admin;

public sealed class SiteSettingsModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    [BindProperty]
    public SettingsForm Form { get; set; } = new();

    public sealed class SettingsForm
    {
        [MaxLength(12000, ErrorMessage = "کد اینماد بیش از حد طولانی است.")]
        public string? EnamadHtml { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.SiteSettings.View)) return Forbid();
        var settings = await db.SiteSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
        Form.EnamadHtml = settings?.EnamadHtml;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.SiteSettings.Edit)) return Forbid();
        if (!ModelState.IsValid) return Page();

        var settings = await db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SiteSettings();
            db.SiteSettings.Add(settings);
        }

        settings.EnamadHtml = string.IsNullOrWhiteSpace(Form.EnamadHtml) ? null : Form.EnamadHtml.Trim();
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = string.IsNullOrWhiteSpace(settings.EnamadHtml)
            ? "کد اینماد پاک شد و چیزی در فوتر نمایش داده نمی‌شود."
            : "کد اینماد ذخیره شد.";
        return RedirectToPage();
    }
}
