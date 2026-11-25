using Microsoft.EntityFrameworkCore;

namespace NavidGelato_Site.Data;

public static class ProductCodeBackfill
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = await db.MenuItems
            .Where(x => x.ProductCode == null)
            .OrderBy(x => x.Id)
            .ToListAsync();

        if (items.Count == 0) return;

        foreach (var item in items)
            item.ProductCode = item.Id;

        await db.SaveChangesAsync();
    }
}
