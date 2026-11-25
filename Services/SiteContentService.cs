using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Services;

public sealed class SiteContentService(AppDbContext db)
{
    public const string LogoImage = "~/img/logo.svg";
    public const string BrandHeroImage = "~/img/gelato-hero.svg";
    public const string BrandBannerImage = "~/img/logo.svg";
    public const string StoreImage = "~/img/gelato-banner.svg";
    public const string ArcherImage = "~/img/gelato-hero.svg";
    public const string ClanImage = "~/img/gelato-hero.svg";

    public Task<SiteSettings?> GetSiteSettingsAsync() => db.SiteSettings
        .AsNoTracking()
        .OrderBy(x => x.Id)
        .FirstOrDefaultAsync();

    public Task<List<Branch>> GetBranchesAsync() => db.Branches
        .AsNoTracking()
        .Where(x => x.IsActive)
        .Include(x => x.Phones.OrderBy(p => p.SortOrder))
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.Id)
        .ToListAsync();

    public Task<Branch?> GetUpcomingOpeningBranchAsync()
    {
        var now = IranClock.Now;
        return db.Branches
            .AsNoTracking()
            .Where(x => x.IsActive &&
                        x.Status == BranchStatus.OpeningSoon &&
                        x.OpeningDateTime.HasValue &&
                        x.OpeningDateTime.Value > now)
            .Include(x => x.Phones.OrderBy(p => p.SortOrder))
            .OrderBy(x => x.OpeningDateTime)
            .ThenBy(x => x.SortOrder)
            .FirstOrDefaultAsync();
    }

    public Task<List<MenuCategory>> GetMenuCategoriesAsync() => db.MenuCategories
        .AsNoTracking()
        .Where(x => x.IsActive)
        .Include(x => x.Items.Where(i => i.IsActive).OrderBy(i => i.SortOrder))
            .ThenInclude(x => x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder))
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.Id)
        .ToListAsync();
}
