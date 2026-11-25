using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Data;

namespace NavidGelato_Site.Api;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api")
            .WithTags("Branch Catalog");

        group.MapGet("/catalog", async (
            [FromHeader(Name = "X-Security-Key")] string? securityKey,
            AppDbContext db,
            HttpRequest request,
            HttpResponse response,
            CancellationToken cancellationToken) =>
        {
            response.Headers["Cache-Control"] = "no-store";

            var normalizedKey = securityKey?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
                return Results.Unauthorized();

            var branch = await db.Branches
                .AsNoTracking()
                .Where(x => x.IsActive && x.SecurityKey == normalizedKey)
                .Select(x => new { x.Id, x.Name })
                .FirstOrDefaultAsync(cancellationToken);

            if (branch is null)
                return Results.Unauthorized();

            var categoryIds = await db.BranchMenuCategories
                .AsNoTracking()
                .Where(x => x.BranchId == branch.Id)
                .Select(x => x.MenuCategoryId)
                .ToListAsync(cancellationToken);

            var productIds = await db.BranchMenuItems
                .AsNoTracking()
                .Where(x => x.BranchId == branch.Id)
                .Select(x => x.MenuItemId)
                .ToListAsync(cancellationToken);

            var categoryEntities = await db.MenuCategories
                .AsNoTracking()
                .Where(x => categoryIds.Contains(x.Id))
                .Include(x => x.Items.Where(item => productIds.Contains(item.Id)))
                    .ThenInclude(item => item.Images)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var categories = categoryEntities
                .Select(category => new BranchCategoryResponse(
                    category.GroupCode,
                    category.Title,
                    category.IsActive,
                    category.Items
                        .OrderBy(item => item.ProductCode ?? int.MaxValue)
                        .ThenBy(item => item.SortOrder)
                        .ThenBy(item => item.Id)
                        .Select(item => new BranchProductResponse(
                            item.ProductCode,
                            item.Name,
                            item.Description,
                            item.Price,
                            item.IsActive,
                            category.GroupCode,
                            BuildImageUrl(
                                item.Images
                                    .OrderByDescending(image => image.IsPrimary)
                                    .ThenBy(image => image.SortOrder)
                                    .Select(image => image.FilePath)
                                    .FirstOrDefault(),
                                request)))
                        .ToList()))
                .ToList();

            return Results.Ok(new BranchCatalogResponse(branch.Id, branch.Name, categories));
        })
        .WithName("GetBranchCatalog")
        .Produces<BranchCatalogResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static string? BuildImageUrl(string? filePath, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var value = filePath.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            return absolute.ToString();

        if (value.StartsWith("~/", StringComparison.Ordinal))
            value = value[1..];
        else if (!value.StartsWith("/", StringComparison.Ordinal))
            value = "/" + value;

        return $"{request.Scheme}://{request.Host}{request.PathBase}{value}";
    }
}

public sealed record BranchCatalogResponse(
    int BranchId,
    string BranchName,
    IReadOnlyList<BranchCategoryResponse> Categories);

public sealed record BranchCategoryResponse(
    int CategoryCode,
    string CategoryName,
    bool IsActive,
    IReadOnlyList<BranchProductResponse> Products);

public sealed record BranchProductResponse(
    int? ProductCode,
    string ProductName,
    string? Description,
    long Price,
    bool IsActive,
    int GroupCode,
    string? ImageUrl);
