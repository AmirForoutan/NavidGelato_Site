using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Data;

public static class SeedCompletenessRepair
{
    private const string BarbarImage = "/img/barbar.png";
    private const string GiantImage = "/img/giant.png";
    private const string FallbackImage = "/img/logo.png";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await RepairCatalogAsync(db);
        await RepairBranchesAsync(db);
    }

    private static async Task RepairCatalogAsync(AppDbContext db)
    {
        if (await db.MenuItems.CountAsync() != 8 || await db.MenuCategories.CountAsync() != 3)
            return;

        var categoryList = await db.MenuCategories.ToListAsync();
        var categories = categoryList.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
        if (!categories.TryGetValue("cold", out var cold) ||
            !categories.TryGetValue("hot", out _) ||
            !categories.TryGetValue("drinks", out var drinks))
            return;

        var existingNames = (await db.MenuItems.Select(x => x.Name).ToListAsync()).ToHashSet();
        var knownIncompleteSeed = new[] { "آرچر", "آرچر پر سس", "بربر", "نوستالژی پرو", "مینیون هات‌داگ گرم", "والکری گوشت گرم", "آب معدنی کوچک", "نوشابه قوطی" };
        if (knownIncompleteSeed.Any(name => !existingNames.Contains(name)))
            return;

        var nostalgia = await db.MenuItems.FirstAsync(x => x.Name == "نوستالژی پرو");
        nostalgia.SortOrder = 5;
        var sodaCan = await db.MenuItems.FirstAsync(x => x.Name == "نوشابه قوطی");
        sodaCan.SortOrder = 8;

        AddItem(db, cold, "بربر پر سس", 157000, BarbarImage, "نسخه پرسس بربر", false, 4);
        AddItem(db, cold, "جاینت پرو", 437000, GiantImage, "سایز جاینت برای اشتهای جدی", false, 6);
        AddItem(db, cold, "جاینت اسموکی پرو", 447000, GiantImage, "طعم اسموکی در ابعاد جاینت", true, 7);
        AddItem(db, cold, "جاینت چیز پرو", 497000, GiantImage, "نسخه پنیری جاینت پرو", false, 8);
        AddItem(db, cold, "جاینت پرومکس", 627000, GiantImage, "نسخه پرومکس جاینت", false, 9);
        AddItem(db, cold, "دراگون پرو", 437000, FallbackImage, "یکی از کاراکترهای محبوب منوی نوید ژلاتو", true, 10);
        AddItem(db, cold, "دراگون پرومکس", 637000, FallbackImage, "دراگون با سس بیشتر و حجم پرو", false, 11);
        AddItem(db, cold, "هیلر پرومکس", 417000, FallbackImage, "یک انتخاب پرسس از خانواده کلن", false, 12);
        AddItem(db, cold, "هوگ پر سس", 577000, FallbackImage, "ساندویچ حجیم با سس بیشتر", false, 13);
        AddItem(db, cold, "وگن پرومکس", 767000, FallbackImage, "یکی از بزرگ‌ترین انتخاب‌های منو", false, 14);

        AddItem(db, drinks, "دلستر قوطی کلاسیک", 30300, FallbackImage, null, false, 2);
        AddItem(db, drinks, "دلستر قوطی لیمویی", 30300, FallbackImage, null, false, 3);
        AddItem(db, drinks, "دلستر قوطی استوایی", 30300, FallbackImage, null, false, 4);
        AddItem(db, drinks, "دلستر قوطی هلو", 30300, FallbackImage, null, false, 5);
        AddItem(db, drinks, "نوشابه بطری", 25000, FallbackImage, null, false, 6);
        AddItem(db, drinks, "نوشابه شیشه‌ای", 29000, FallbackImage, null, false, 7);
        AddItem(db, drinks, "نوشابه خانواده", 111000, FallbackImage, null, false, 9);
        AddItem(db, drinks, "انرژی‌زا", 50000, FallbackImage, null, false, 10);

        await db.SaveChangesAsync();
    }

    private static async Task RepairBranchesAsync(AppDbContext db)
    {
        if (await db.Branches.CountAsync() != 4)
            return;

        var existingNames = (await db.Branches.Select(x => x.Name).ToListAsync()).ToHashSet();
        var knownIncompleteSeed = new[] { "عظیمیه (مرکزی)", "پونک", "فردوس", "گوهردشت" };
        if (knownIncompleteSeed.Any(name => !existingNames.Contains(name)))
            return;

        AddBranch(db, "گلشهر", "کرج", "خیابان درختی، ۲۰۰ متر پایین‌تر از بلوار حدادی، روبه‌روی مرکز خرید درختی", false, 5, "09362895496");
        AddBranch(db, "فردیس", "فردیس", "کانال غربی، جنب بانک مسکن", false, 6, "09368923176");
        AddBranch(db, "هشتگرد", "هشتگرد", "بلوار امام خمینی، جنب بیمه ایران", false, 7, "09193807078");
        AddBranch(db, "شهریار", "شهریار", "بلوار شهید کلهر، نرسیده به میدان فرمانداری، نبش کوچه عدالت", false, 8, "02165222147", "09967914678");
        AddBranch(db, "کیان‌آباد", "اهواز", "کیان‌آباد، وهابی، نبش ۱۳ شرقی", false, 9, "09388781222");
        AddBranch(db, "متل قو", "مازندران", "بعد از میدان سلمان‌شهر، جنب لیلی", false, 10, "09043998069", "01154617108");
        AddBranch(db, "اسلامشهر", "اسلامشهر", "گلستان، بعد از بلوار سردار سلیمانی (ورودی گلستان)", false, 11, "09922688968");
        AddBranch(db, "نظرآباد", "نظرآباد", "خیابان تهران، نرسیده به فرمانداری", false, 12, "09120570594", "02645353772");
        AddBranch(db, "تهرانسر", "تهران", "بلوار لاله", false, 13, "09101977455");

        await db.SaveChangesAsync();
    }

    private static void AddItem(AppDbContext db, MenuCategory category, string name, long price, string image, string? description, bool featured, int order)
    {
        var item = new MenuItem
        {
            MenuCategoryId = category.Id,
            Name = name,
            Price = price,
            Description = description,
            IsFeatured = featured,
            IsActive = true,
            SortOrder = order
        };

        item.Images.Add(new MenuItemImage
        {
            FilePath = image,
            AltText = $"{name} نوید ژلاتو",
            IsPrimary = true,
            SortOrder = 1
        });

        db.MenuItems.Add(item);
    }

    private static void AddBranch(AppDbContext db, string name, string city, string address, bool central, int order, params string[] phones)
    {
        var branch = new Branch
        {
            Name = name,
            City = city,
            Address = address,
            IsCentral = central,
            IsActive = true,
            Status = BranchStatus.Open,
            SortOrder = order
        };

        for (var i = 0; i < phones.Length; i++)
            branch.Phones.Add(new BranchPhone { PhoneNumber = phones[i], SortOrder = i + 1 });

        db.Branches.Add(branch);
    }
}
