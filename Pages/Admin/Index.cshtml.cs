using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Security;

namespace NavidGelato_Site.Pages.Admin;

public class IndexModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    public int ProductCount { get; private set; }
    public int ActiveProductCount { get; private set; }
    public int CategoryCount { get; private set; }
    public int BranchCount { get; private set; }
    public int ActiveBranchCount { get; private set; }
    public int ImageCount { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Dashboard.View)) return Forbid();
        ProductCount = await db.MenuItems.CountAsync();
        ActiveProductCount = await db.MenuItems.CountAsync(x => x.IsActive);
        CategoryCount = await db.MenuCategories.CountAsync();
        BranchCount = await db.Branches.CountAsync();
        ActiveBranchCount = await db.Branches.CountAsync(x => x.IsActive);
        ImageCount = await db.MenuItemImages.CountAsync();
        return Page();
    }
}
