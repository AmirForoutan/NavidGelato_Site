using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages.Admin;

public sealed class ContactErrorsModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    public List<ContactErrorLog> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.ContactErrors.View)) return Forbid();
        Items = await db.ContactErrorLogs.AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(250)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.ContactErrors.Delete)) return Forbid();
        var item = await db.ContactErrorLogs.FindAsync(id);
        if (item is null) return NotFound();
        db.ContactErrorLogs.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "خطای ثبت‌شده حذف شد.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.ContactErrors.Delete)) return Forbid();
        await db.ContactErrorLogs.ExecuteDeleteAsync();
        TempData["Success"] = "همه خطاهای ثبت‌شده پاک شدند.";
        return RedirectToPage();
    }

    public string FormatOccurredAt(DateTime utc)
    {
        var value = IranClock.FromUtc(utc);
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00} - {value:HH:mm:ss}";
    }
}
