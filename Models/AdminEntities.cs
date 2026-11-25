using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace NavidGelato_Site.Models;

public sealed class AdminUser
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MenuCategory
{
    public int Id { get; set; }

    public int GroupCode { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Subtitle { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
    public ICollection<BranchMenuCategory> BranchLinks { get; set; } = new List<BranchMenuCategory>();
}

public sealed class MenuItem
{
    public int Id { get; set; }

    public int? ProductCode { get; set; }

    public int MenuCategoryId { get; set; }
    public MenuCategory? MenuCategory { get; set; }

    [MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public long Price { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public ICollection<MenuItemImage> Images { get; set; } = new List<MenuItemImage>();
    public ICollection<BranchMenuItem> BranchLinks { get; set; } = new List<BranchMenuItem>();

    [NotMapped]
    public string PriceText => Price.ToString("N0", CultureInfo.InvariantCulture);

    [NotMapped]
    public string? PrimaryImagePath => Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => x.FilePath).FirstOrDefault();
}

public sealed class MenuItemImage
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public enum BranchStatus
{
    Open = 0,
    SettingUp = 1,
    OpeningSoon = 2
}

public sealed class Branch
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(700)]
    public string Address { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [MaxLength(1000)]
    public string? NeshanUrl { get; set; }

    [MaxLength(1000)]
    public string? BaladUrl { get; set; }

    [MaxLength(1000)]
    public string? WazeUrl { get; set; }

    [MaxLength(1000)]
    public string? GoogleMapsUrl { get; set; }

    public bool IsCentral { get; set; }
    public bool IsActive { get; set; } = true;
    public BranchStatus Status { get; set; } = BranchStatus.Open;
    public DateTime? OpeningDateTime { get; set; }
    public int SortOrder { get; set; }

    [MaxLength(128)]
    public string? SecurityKey { get; set; }

    [MaxLength(26)]
    public string? Iban { get; set; }

    public ICollection<BranchPhone> Phones { get; set; } = new List<BranchPhone>();
    public ICollection<BranchMenuCategory> MenuCategoryLinks { get; set; } = new List<BranchMenuCategory>();
    public ICollection<BranchMenuItem> MenuItemLinks { get; set; } = new List<BranchMenuItem>();
    public ICollection<BranchPaymentLink> PaymentLinks { get; set; } = new List<BranchPaymentLink>();

    [NotMapped]
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    public string GetGoogleMapsUrl() => !string.IsNullOrWhiteSpace(GoogleMapsUrl) ? GoogleMapsUrl : BuildGoogleUrl();
    public string GetWazeUrl() => !string.IsNullOrWhiteSpace(WazeUrl) ? WazeUrl : BuildWazeUrl();
    public string GetNeshanUrl() => !string.IsNullOrWhiteSpace(NeshanUrl) ? NeshanUrl : $"https://neshan.org/maps/search/{Uri.EscapeDataString(SearchText())}";
    public string GetBaladUrl() => !string.IsNullOrWhiteSpace(BaladUrl) ? BaladUrl : $"https://balad.ir/search?query={Uri.EscapeDataString(SearchText())}";

    public string? GetOpeningDateTimeText()
    {
        if (!OpeningDateTime.HasValue) return null;
        var value = OpeningDateTime.Value;
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00} ساعت {value:HH:mm}";
    }

    public string? GetCoordinateText() => HasCoordinates
        ? $"{Latitude!.Value.ToString(CultureInfo.InvariantCulture)}, {Longitude!.Value.ToString(CultureInfo.InvariantCulture)}"
        : null;

    private string SearchText() => $"نوید ژلاتو {Name} {Address}";

    private string BuildGoogleUrl()
    {
        var query = HasCoordinates
            ? $"{Latitude!.Value.ToString(CultureInfo.InvariantCulture)},{Longitude!.Value.ToString(CultureInfo.InvariantCulture)}"
            : SearchText();
        return $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(query)}";
    }

    private string BuildWazeUrl()
    {
        if (HasCoordinates)
            return $"https://www.waze.com/ul?ll={Latitude!.Value.ToString(CultureInfo.InvariantCulture)}%2C{Longitude!.Value.ToString(CultureInfo.InvariantCulture)}&navigate=yes";

        return $"https://www.waze.com/ul?q={Uri.EscapeDataString(SearchText())}&navigate=yes";
    }
}

public sealed class BranchPhone
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public sealed class BranchMenuCategory
{
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int MenuCategoryId { get; set; }
    public MenuCategory? MenuCategory { get; set; }
}

public sealed class BranchMenuItem
{
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
}

public sealed class SmsSettings
{
    public const string DefaultMessageFilterTerms =
        "احمق\nبی شعور\nبیشعور\nبی ناموس\nبیناموس\nحرامزاده\nحرومزاده\nمادر جنده\nمادرجنده\nقحبه\nجنده\nفاحشه\nکصخل\nکسخل\nگوه\nگوه خور\nگه خور\nآشغال";

    public int Id { get; set; }
    public bool Enabled { get; set; }

    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string EncryptedPassword { get; set; } = string.Empty;

    [MaxLength(50)]
    public string From { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string RecipientNumbers { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string PaymentLinkTemplate { get; set; } =
        "نوید ژلاتو\nلینک پرداخت {amount} ریال:\n{link}\nمهلت پرداخت: ۱۵ دقیقه پس از اولین باز شدن لینک";

    public bool MessageFilterEnabled { get; set; } = true;

    [MaxLength(12000)]
    public string MessageFilterTerms { get; set; } = DefaultMessageFilterTerms;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
