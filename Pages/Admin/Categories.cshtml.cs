using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;

namespace NavidGelato_Site.Pages.Admin;

public class CategoriesModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    public List<MenuCategory> Items { get; private set; } = [];
    [BindProperty] public CategoryForm Form { get; set; } = new();
    public class CategoryForm { public int Id { get; set; } [Required,Range(1,int.MaxValue)] public int? GroupCode { get; set; } [Required,MaxLength(120)] public string Title { get; set; }=string.Empty; [Required,MaxLength(120)] public string Slug { get; set; }=string.Empty; [MaxLength(500)] public string? Subtitle { get; set; } public int SortOrder { get; set; } public bool IsActive { get; set; }=true; }

    public async Task<IActionResult> OnGetAsync(int? editId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Categories.View)) return Forbid();
        await LoadAsync();
        if(editId is >0){ if(!await permissions.HasAsync(User,AdminPermissions.Categories.Edit)) return Forbid(); var entity=await db.MenuCategories.FindAsync(editId.Value); if(entity is not null) Form=new(){Id=entity.Id,GroupCode=entity.GroupCode,Title=entity.Title,Slug=entity.Slug,Subtitle=entity.Subtitle,SortOrder=entity.SortOrder,IsActive=entity.IsActive}; }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var permission=Form.Id==0?AdminPermissions.Categories.Create:AdminPermissions.Categories.Edit; if(!await permissions.HasAsync(User,permission)) return Forbid();
        Form.Slug=(Form.Slug??string.Empty).Trim().ToLowerInvariant().Replace(" ","-");
        if(await db.MenuCategories.AnyAsync(x=>x.Slug==Form.Slug&&x.Id!=Form.Id)) ModelState.AddModelError("Form.Slug","این Slug قبلاً استفاده شده است.");
        if(Form.GroupCode.HasValue && await db.MenuCategories.AnyAsync(x=>x.GroupCode==Form.GroupCode.Value&&x.Id!=Form.Id)) ModelState.AddModelError("Form.GroupCode","این کد گروه قبلاً استفاده شده است.");
        if(!ModelState.IsValid){await LoadAsync();return Page();}
        var entity=Form.Id==0?new MenuCategory():await db.MenuCategories.FindAsync(Form.Id); if(entity is null)return NotFound(); entity.GroupCode=Form.GroupCode!.Value;entity.Title=Form.Title.Trim();entity.Slug=Form.Slug;entity.Subtitle=Form.Subtitle?.Trim();entity.SortOrder=Form.SortOrder;entity.IsActive=Form.IsActive;if(Form.Id==0)db.MenuCategories.Add(entity);await db.SaveChangesAsync();TempData["Success"]="دسته‌بندی ذخیره شد.";return RedirectToPage();
    }
    private async Task LoadAsync()=>Items=await db.MenuCategories.AsNoTracking().OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id).ToListAsync();
}
