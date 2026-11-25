using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages.Admin;

public class ProductsModel(AppDbContext db, IWebHostEnvironment env, IAdminPermissionService permissions) : PageModel
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public List<SelectListItem> CategoryOptions { get; private set; } = [];

    [BindProperty]
    public ProductForm Form { get; set; } = new();

    public class ProductForm
    {
        public int Id { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int? ProductCode { get; set; }

        [Required, MaxLength(180)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int MenuCategoryId { get; set; }

        [Range(0, long.MaxValue)]
        public long Price { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public List<IFormFile> NewImages { get; set; } = [];
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Products.View)) return Forbid();
        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetListAsync()
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Products.View)) return Forbid();

        var fallback = Url.Content(SiteContentService.LogoImage);
        var items = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.MenuCategory)
            .Include(x => x.Images)
            .OrderBy(x => x.MenuCategory!.SortOrder)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return new JsonResult(new
        {
            data = items.Select(x =>
            {
                var primary = x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault();
                return new
                {
                    id = x.Id,
                    productCode = x.ProductCode,
                    name = x.Name,
                    description = x.Description,
                    categoryName = x.MenuCategory?.Title ?? "—",
                    categoryId = x.MenuCategoryId,
                    price = x.Price,
                    priceText = x.Price.ToString("N0"),
                    sortOrder = x.SortOrder,
                    isFeatured = x.IsFeatured,
                    isActive = x.IsActive,
                    imageCount = x.Images.Count,
                    primaryImage = ToClientPath(primary?.FilePath) ?? fallback
                };
            })
        });
    }

    public async Task<IActionResult> OnGetEditorAsync(int id)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Products.Edit)) return Forbid();

        var entity = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return NotFound();
        return new JsonResult(BuildEditorResponse(entity));
    }

    public async Task<IActionResult> OnPostSaveAjaxAsync()
    {
        var permission = Form.Id == 0 ? AdminPermissions.Products.Create : AdminPermissions.Products.Edit;
        if (!await permissions.HasAsync(User, permission)) return Forbid();
        if (Form.NewImages.Count > 0 && !await permissions.HasAsync(User, AdminPermissions.Products.ManageImages)) return Forbid();

        if (!await db.MenuCategories.AnyAsync(x => x.Id == Form.MenuCategoryId))
            ModelState.AddModelError("Form.MenuCategoryId", "دسته‌بندی معتبر نیست.");

        if (Form.ProductCode.HasValue &&
            await db.MenuItems.AnyAsync(x => x.ProductCode == Form.ProductCode && x.Id != Form.Id))
            ModelState.AddModelError("Form.ProductCode", "این کد کالا قبلاً استفاده شده است.");

        ValidateImages(Form.NewImages);

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ValidationErrors() });

        var entity = Form.Id == 0
            ? new MenuItem()
            : await db.MenuItems.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == Form.Id);

        if (entity is null) return NotFound();

        var isCreate = Form.Id == 0;
        entity.ProductCode = Form.ProductCode;
        entity.Name = Form.Name.Trim();
        entity.MenuCategoryId = Form.MenuCategoryId;
        entity.Price = Form.Price;
        entity.Description = Clean(Form.Description);
        entity.IsFeatured = Form.IsFeatured;
        entity.IsActive = Form.IsActive;
        entity.SortOrder = Form.SortOrder;

        if (isCreate) db.MenuItems.Add(entity);

        await db.SaveChangesAsync();

        if (Form.NewImages.Count > 0)
            await SaveImagesAsync(entity);

        var saved = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Images)
            .FirstAsync(x => x.Id == entity.Id);

        return new JsonResult(new
        {
            success = true,
            message = isCreate ? "کالا با موفقیت ایجاد شد." : "تغییرات کالا با موفقیت ذخیره شد.",
            product = BuildEditorResponse(saved)
        });
    }

    public async Task<IActionResult> OnPostDeleteAjaxAsync(int id)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Products.Delete)) return Forbid();

        var entity = await db.MenuItems
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return NotFound();

        foreach (var image in entity.Images)
            DeletePhysicalImage(image.FilePath);

        db.MenuItems.Remove(entity);
        await db.SaveChangesAsync();

        return new JsonResult(new { success = true, message = "کالا حذف شد." });
    }

    public async Task<IActionResult> OnPostSetPrimaryAjaxAsync(int imageId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Products.ManageImages)) return Forbid();

        var image = await db.MenuItemImages.FindAsync(imageId);
        if (image is null) return NotFound();

        await db.MenuItemImages
            .Where(x => x.MenuItemId == image.MenuItemId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false));

        image.IsPrimary = true;
        await db.SaveChangesAsync();

        var entity = await db.MenuItems.AsNoTracking().Include(x => x.Images).FirstAsync(x => x.Id == image.MenuItemId);
        return new JsonResult(new { success = true, product = BuildEditorResponse(entity) });
    }

    public async Task<IActionResult> OnPostDeleteImageAjaxAsync(int imageId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Products.ManageImages)) return Forbid();

        var image = await db.MenuItemImages.FindAsync(imageId);
        if (image is null) return NotFound();

        var itemId = image.MenuItemId;
        DeletePhysicalImage(image.FilePath);
        db.MenuItemImages.Remove(image);
        await db.SaveChangesAsync();

        var replacement = await db.MenuItemImages
            .Where(x => x.MenuItemId == itemId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .FirstOrDefaultAsync();

        if (replacement is not null &&
            !await db.MenuItemImages.AnyAsync(x => x.MenuItemId == itemId && x.IsPrimary))
        {
            replacement.IsPrimary = true;
            await db.SaveChangesAsync();
        }

        var entity = await db.MenuItems.AsNoTracking().Include(x => x.Images).FirstAsync(x => x.Id == itemId);
        return new JsonResult(new { success = true, product = BuildEditorResponse(entity) });
    }

    private object BuildEditorResponse(MenuItem entity) => new
    {
        id = entity.Id,
        productCode = entity.ProductCode,
        name = entity.Name,
        menuCategoryId = entity.MenuCategoryId,
        price = entity.Price,
        description = entity.Description,
        isFeatured = entity.IsFeatured,
        isActive = entity.IsActive,
        sortOrder = entity.SortOrder,
        images = entity.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => new
            {
                id = x.Id,
                filePath = ToClientPath(x.FilePath),
                altText = x.AltText,
                isPrimary = x.IsPrimary,
                sortOrder = x.SortOrder
            })
            .ToList()
    };

    private async Task LoadCategoriesAsync()
    {
        CategoryOptions = await db.MenuCategories
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new SelectListItem(x.Title, x.Id.ToString()))
            .ToListAsync();
    }

    private async Task SaveImagesAsync(MenuItem entity)
    {
        var root = Path.Combine(env.WebRootPath, "uploads", "products", entity.Id.ToString());
        Directory.CreateDirectory(root);

        var hasPrimary = await db.MenuItemImages.AnyAsync(x => x.MenuItemId == entity.Id && x.IsPrimary);
        var order = await db.MenuItemImages
            .Where(x => x.MenuItemId == entity.Id)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync() ?? 0;

        foreach (var file in Form.NewImages)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = $"{Guid.NewGuid():N}{ext}";

            await using var stream = System.IO.File.Create(Path.Combine(root, name));
            await file.CopyToAsync(stream);

            db.MenuItemImages.Add(new MenuItemImage
            {
                MenuItemId = entity.Id,
                FilePath = $"/uploads/products/{entity.Id}/{name}",
                AltText = $"{entity.Name} نوید ژلاتو",
                IsPrimary = !hasPrimary,
                SortOrder = ++order
            });

            hasPrimary = true;
        }

        await db.SaveChangesAsync();
    }

    private void ValidateImages(IEnumerable<IFormFile> files)
    {
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                ModelState.AddModelError("Form.NewImages", $"فرمت {ext} مجاز نیست.");

            if (file.Length <= 0 || file.Length > MaxImageBytes)
                ModelState.AddModelError("Form.NewImages", "حجم هر تصویر باید حداکثر ۵ مگابایت باشد.");
        }
    }

    private Dictionary<string, string[]> ValidationErrors() => ModelState
        .Where(x => x.Value?.Errors.Count > 0)
        .ToDictionary(
            x => x.Key,
            x => x.Value!.Errors.Select(error => error.ErrorMessage).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray());

    private string? ToClientPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (Uri.TryCreate(path, UriKind.Absolute, out _)) return path;
        return path.StartsWith('/') ? Url.Content($"~{path}") : Url.Content(path);
    }

    private void DeletePhysicalImage(string path)
    {
        if (!path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return;

        var full = Path.GetFullPath(Path.Combine(
            env.WebRootPath,
            path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        var root = Path.GetFullPath(env.WebRootPath);
        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(full))
            System.IO.File.Delete(full);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
