using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;
using NavidGelato_Site.Security;
using NavidGelato_Site.Services;

namespace NavidGelato_Site.Pages.Admin;

public class BranchesModel(AppDbContext db, IAdminPermissionService permissions) : PageModel
{
    public List<Branch> Items { get; private set; } = [];
    public List<CatalogCategoryNode> CatalogTree { get; private set; } = [];

    [BindProperty]
    public BranchForm Form { get; set; } = new();

    public sealed class CatalogCategoryNode
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public List<CatalogProductNode> Products { get; init; } = [];
    }

    public sealed class CatalogProductNode
    {
        public int Id { get; init; }
        public int? ProductCode { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    public class BranchForm
    {
        public int Id { get; set; }
        [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string City { get; set; } = string.Empty;
        [Required, MaxLength(700)] public string Address { get; set; } = string.Empty;
        [MaxLength(128)] public string? SecurityKey { get; set; }
        [MaxLength(26)] public string? Iban { get; set; }
        public string? PhoneNumbers { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? NeshanUrl { get; set; }
        public string? BaladUrl { get; set; }
        public string? WazeUrl { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public bool IsCentral { get; set; }
        public bool IsActive { get; set; } = true;
        public BranchStatus Status { get; set; } = BranchStatus.Open;
        public string? OpeningDateTimeText { get; set; }
        public int SortOrder { get; set; }
        public List<int> CategoryIds { get; set; } = [];
        public List<int> ProductIds { get; set; } = [];
    }

    public async Task<IActionResult> OnGetAsync(int? editId)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Branches.View)) return Forbid();
        await LoadAsync();

        if (editId is > 0)
        {
            if (!await permissions.HasAsync(User, AdminPermissions.Branches.Edit)) return Forbid();

            var entity = await db.Branches
                .Include(x => x.Phones)
                .FirstOrDefaultAsync(x => x.Id == editId);

            if (entity is not null)
            {
                var categoryIds = await db.BranchMenuCategories
                    .AsNoTracking()
                    .Where(x => x.BranchId == entity.Id)
                    .Select(x => x.MenuCategoryId)
                    .ToListAsync();

                var productIds = await db.BranchMenuItems
                    .AsNoTracking()
                    .Where(x => x.BranchId == entity.Id)
                    .Select(x => x.MenuItemId)
                    .ToListAsync();

                Form = new BranchForm
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    City = entity.City,
                    Address = entity.Address,
                    SecurityKey = entity.SecurityKey,
                    Iban = entity.Iban,
                    PhoneNumbers = string.Join(Environment.NewLine, entity.Phones.OrderBy(x => x.SortOrder).Select(x => x.PhoneNumber)),
                    Latitude = entity.Latitude?.ToString(CultureInfo.InvariantCulture),
                    Longitude = entity.Longitude?.ToString(CultureInfo.InvariantCulture),
                    NeshanUrl = entity.NeshanUrl,
                    BaladUrl = entity.BaladUrl,
                    WazeUrl = entity.WazeUrl,
                    GoogleMapsUrl = entity.GoogleMapsUrl,
                    IsCentral = entity.IsCentral,
                    IsActive = entity.IsActive,
                    Status = entity.Status,
                    OpeningDateTimeText = FormatPersianDateTime(entity.OpeningDateTime),
                    SortOrder = entity.SortOrder,
                    CategoryIds = categoryIds,
                    ProductIds = productIds
                };
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var permission = Form.Id == 0 ? AdminPermissions.Branches.Create : AdminPermissions.Branches.Edit;
        if (!await permissions.HasAsync(User, permission)) return Forbid();

        decimal? lat = ParseCoordinate(Form.Latitude, "Form.Latitude");
        decimal? lng = ParseCoordinate(Form.Longitude, "Form.Longitude");

        var submittedSecurityKey = Clean(Form.SecurityKey);
        var iban = NormalizeIban(Form.Iban);
        if (!string.IsNullOrWhiteSpace(Form.Iban) && iban is null)
            ModelState.AddModelError("Form.Iban", "شماره شبا باید با IR شروع شود و بعد از آن ۲۴ رقم داشته باشد.");
        Form.Iban = iban;
        string? existingSecurityKey = null;
        if (Form.Id > 0)
        {
            existingSecurityKey = await db.Branches
                .AsNoTracking()
                .Where(x => x.Id == Form.Id)
                .Select(x => x.SecurityKey)
                .FirstOrDefaultAsync();
        }

        var securityKey = Form.Id > 0 && submittedSecurityKey is null
            ? existingSecurityKey
            : submittedSecurityKey;

        // Keep the resolved key in the bound form as well, so validation errors
        // never redisplay the editor with an empty key and accidentally lose it.
        Form.SecurityKey = securityKey;

        if (securityKey is not null &&
            await db.Branches.AnyAsync(x => x.SecurityKey == securityKey && x.Id != Form.Id))
            ModelState.AddModelError("Form.SecurityKey", "این کلید امنیتی قبلاً برای شعبه دیگری ثبت شده است.");

        var phones = (Form.PhoneNumbers ?? string.Empty)
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        DateTime? openingDateTime = null;
        if (Form.Status == BranchStatus.OpeningSoon)
        {
            openingDateTime = ParsePersianDateTime(Form.OpeningDateTimeText);
            if (openingDateTime.HasValue && openingDateTime.Value <= IranClock.Now)
                ModelState.AddModelError("Form.OpeningDateTimeText", "تاریخ و ساعت افتتاح باید در آینده باشد.");
        }

        if (Form.Status == BranchStatus.Open && phones.Count == 0)
            ModelState.AddModelError("Form.PhoneNumbers", "برای شعبه دایر حداقل یک شماره تماس وارد کنید.");

        var categoryIds = Form.CategoryIds.Distinct().ToList();
        var validCategoryIds = await db.MenuCategories
            .Where(x => categoryIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var productIds = Form.ProductIds.Distinct().ToList();
        var validProductIds = await db.MenuItems
            .Where(x => productIds.Contains(x.Id) && validCategoryIds.Contains(x.MenuCategoryId))
            .Select(x => x.Id)
            .ToListAsync();

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var entity = Form.Id == 0
            ? new Branch()
            : await db.Branches.Include(x => x.Phones).FirstOrDefaultAsync(x => x.Id == Form.Id);

        if (entity is null) return NotFound();

        var isCreate = Form.Id == 0;
        entity.Name = Form.Name.Trim();
        entity.City = Form.City.Trim();
        entity.Address = Form.Address.Trim();
        entity.SecurityKey = securityKey;
        entity.Iban = iban;
        entity.Latitude = lat;
        entity.Longitude = lng;
        entity.NeshanUrl = Clean(Form.NeshanUrl);
        entity.BaladUrl = Clean(Form.BaladUrl);
        entity.WazeUrl = Clean(Form.WazeUrl);
        entity.GoogleMapsUrl = Clean(Form.GoogleMapsUrl);
        entity.IsCentral = Form.IsCentral;
        entity.IsActive = Form.IsActive;
        entity.Status = Form.Status;
        entity.OpeningDateTime = Form.Status == BranchStatus.OpeningSoon ? openingDateTime : null;
        entity.SortOrder = Form.SortOrder;

        if (isCreate)
            db.Branches.Add(entity);
        else
            db.BranchPhones.RemoveRange(entity.Phones);

        entity.Phones = phones.Select((phone, index) => new BranchPhone
        {
            PhoneNumber = phone,
            SortOrder = index + 1
        }).ToList();

        await db.SaveChangesAsync();

        if (!isCreate)
        {
            await db.BranchMenuCategories.Where(x => x.BranchId == entity.Id).ExecuteDeleteAsync();
            await db.BranchMenuItems.Where(x => x.BranchId == entity.Id).ExecuteDeleteAsync();
        }

        db.BranchMenuCategories.AddRange(validCategoryIds.Select(categoryId => new BranchMenuCategory
        {
            BranchId = entity.Id,
            MenuCategoryId = categoryId
        }));

        db.BranchMenuItems.AddRange(validProductIds.Select(productId => new BranchMenuItem
        {
            BranchId = entity.Id,
            MenuItemId = productId
        }));

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = isCreate ? "شعبه با موفقیت ایجاد شد." : "تغییرات شعبه با موفقیت ذخیره شد.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await permissions.HasAsync(User, AdminPermissions.Branches.Delete)) return Forbid();
        var entity = await db.Branches.FindAsync(id);
        if (entity is null) return NotFound();

        if (await db.BranchPaymentLinks.AnyAsync(x => x.BranchId == id))
        {
            TempData["Error"] = "این شعبه سابقه لینک پرداخت دارد و برای حفظ سوابق مالی قابل حذف نیست.";
            return RedirectToPage();
        }

        db.Branches.Remove(entity);
        await db.SaveChangesAsync();
        TempData["Success"] = "شعبه حذف شد.";
        return RedirectToPage();
    }

    private DateTime? ParsePersianDateTime(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            ModelState.AddModelError("Form.OpeningDateTimeText", "تاریخ و ساعت افتتاح را مشخص کنید.");
            return null;
        }

        var normalized = NormalizeDigits(input);
        var numbers = Regex.Matches(normalized, @"\d+")
            .Select(x => int.Parse(x.Value, CultureInfo.InvariantCulture))
            .ToArray();

        if (numbers.Length < 5)
        {
            ModelState.AddModelError("Form.OpeningDateTimeText", "تاریخ و ساعت شمسی معتبر وارد کنید.");
            return null;
        }

        try
        {
            var calendar = new PersianCalendar();
            var second = numbers.Length > 5 ? numbers[5] : 0;
            return calendar.ToDateTime(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], second, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            ModelState.AddModelError("Form.OpeningDateTimeText", "تاریخ و ساعت شمسی معتبر وارد کنید.");
            return null;
        }
    }

    private static string? FormatPersianDateTime(DateTime? value)
    {
        if (!value.HasValue) return null;
        var calendar = new PersianCalendar();
        var date = value.Value;
        return $"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00} {date:HH:mm}";
    }

    private decimal? ParseCoordinate(string? input, string key)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (decimal.TryParse(input.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return value;
        ModelState.AddModelError(key, "مختصات را با اعداد انگلیسی وارد کنید.");
        return null;
    }

    private static string NormalizeDigits(string value)
    {
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";
        for (var i = 0; i < 10; i++)
            value = value.Replace(persian[i], (char)('0' + i)).Replace(arabic[i], (char)('0' + i));
        return value;
    }

    private static string? NormalizeIban(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = NormalizeDigits(value).ToUpperInvariant();
        normalized = Regex.Replace(normalized, @"[\s-]", string.Empty);
        if (Regex.IsMatch(normalized, @"^\d{24}$")) normalized = "IR" + normalized;
        return Regex.IsMatch(normalized, @"^IR\d{24}$") ? normalized : null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task LoadAsync()
    {
        Items = await db.Branches
            .AsNoTracking()
            .Include(x => x.Phones)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var categories = await db.MenuCategories
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        CatalogTree = categories.Select(x => new CatalogCategoryNode
        {
            Id = x.Id,
            Title = x.Title,
            IsActive = x.IsActive,
            Products = x.Items
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id)
                .Select(item => new CatalogProductNode
                {
                    Id = item.Id,
                    ProductCode = item.ProductCode,
                    Name = item.Name,
                    IsActive = item.IsActive
                })
                .ToList()
        }).ToList();
    }
}
