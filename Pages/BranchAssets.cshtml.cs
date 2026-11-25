using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;

namespace NavidGelato_Site.Pages;

public sealed class BranchAssetsModel(
    AppDbContext db,
    IWebHostEnvironment env) : PageModel
{
    public List<ProductAssetRow> Products { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SetPrivateDiscoveryHeaders();

        var products = await db.MenuItems
            .AsNoTracking()
            .Where(x => x.Images.Any(i => i.FilePath != ""))
            .OrderBy(x => x.MenuCategory!.SortOrder)
            .ThenBy(x => x.MenuCategory!.Title)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ProductAssetRow
            {
                Id = x.Id,
                Name = x.Name,
                ProductCode = x.ProductCode,
                CategoryName = x.MenuCategory != null ? x.MenuCategory.Title : "بدون دسته‌بندی",
                IsActive = x.IsActive,
                Images = x.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => new ProductAssetImage
                    {
                        Id = i.Id,
                        FilePath = i.FilePath,
                        IsPrimary = i.IsPrimary
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.Images = product.Images
                .Where(image => IsExistingProductImage(image.FilePath))
                .ToList();
        }

        Products = products
            .Where(product => product.Images.Count > 0)
            .ToList();
    }

    public string GetImageUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        if (Uri.TryCreate(filePath, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        var normalized = "/" + filePath.Trim().TrimStart('~', '/');
        return Url.Content($"~{normalized}");
    }

    private bool IsExistingProductImage(string filePath)
    {
        var localPath = NormalizeProductImagePath(filePath);
        if (localPath is null)
            return false;

        var uploadsRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", "products"));
        var fullPath = Path.GetFullPath(Path.Combine(
            env.WebRootPath,
            localPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        var rootWithSeparator = uploadsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
               System.IO.File.Exists(fullPath);
    }

    private static string? NormalizeProductImagePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || Uri.TryCreate(filePath, UriKind.Absolute, out _))
            return null;

        var normalized = "/" + filePath.Trim().TrimStart('~', '/');
        return normalized.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : null;
    }

    private void SetPrivateDiscoveryHeaders()
    {
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet";
        Response.Headers["Referrer-Policy"] = "no-referrer";
    }

    public sealed class ProductAssetRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? ProductCode { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public List<ProductAssetImage> Images { get; set; } = [];
    }

    public sealed class ProductAssetImage
    {
        public int Id { get; init; }
        public string FilePath { get; init; } = string.Empty;
        public bool IsPrimary { get; init; }
    }
}
