using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NavidGelato_Site.Models;

namespace NavidGelato_Site.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options), IDataProtectionKeyContext
{
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemImage> MenuItemImages => Set<MenuItemImage>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchPhone> BranchPhones => Set<BranchPhone>();
    public DbSet<BranchMenuCategory> BranchMenuCategories => Set<BranchMenuCategory>();
    public DbSet<BranchMenuItem> BranchMenuItems => Set<BranchMenuItem>();
    public DbSet<SmsSettings> SmsSettings => Set<SmsSettings>();
    public DbSet<ContactErrorLog> ContactErrorLogs => Set<ContactErrorLog>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<BranchPaymentLink> BranchPaymentLinks => Set<BranchPaymentLink>();
    public DbSet<PaymentGatewaySettings> PaymentGatewaySettings => Set<PaymentGatewaySettings>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MenuCategory>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<MenuCategory>().HasIndex(x => x.GroupCode).IsUnique();
        modelBuilder.Entity<MenuItem>().HasIndex(x => x.ProductCode).IsUnique().HasFilter("[ProductCode] IS NOT NULL");
        modelBuilder.Entity<Branch>().HasIndex(x => x.SecurityKey).IsUnique().HasFilter("[SecurityKey] IS NOT NULL");
        modelBuilder.Entity<BranchPaymentLink>().HasIndex(x => x.PublicToken).IsUnique();
        modelBuilder.Entity<BranchPaymentLink>().HasIndex(x => x.ShortCode).IsUnique().HasFilter("[ShortCode] IS NOT NULL");
        modelBuilder.Entity<BranchPaymentLink>().HasIndex(x => x.ResNum).IsUnique();
        modelBuilder.Entity<BranchPaymentLink>().HasIndex(x => x.RefNum).IsUnique().HasFilter("[RefNum] IS NOT NULL");
        modelBuilder.Entity<BranchPaymentLink>().HasIndex(x => new { x.BranchId, x.CreatedAtUtc });

        modelBuilder.Entity<MenuItem>()
            .HasOne(x => x.MenuCategory)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MenuItemImage>()
            .HasOne(x => x.MenuItem)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BranchPhone>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.Phones)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BranchMenuCategory>().HasKey(x => new { x.BranchId, x.MenuCategoryId });
        modelBuilder.Entity<BranchMenuCategory>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.MenuCategoryLinks)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BranchMenuCategory>()
            .HasOne(x => x.MenuCategory)
            .WithMany(x => x.BranchLinks)
            .HasForeignKey(x => x.MenuCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BranchMenuItem>().HasKey(x => new { x.BranchId, x.MenuItemId });
        modelBuilder.Entity<BranchMenuItem>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.MenuItemLinks)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BranchMenuItem>()
            .HasOne(x => x.MenuItem)
            .WithMany(x => x.BranchLinks)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BranchPaymentLink>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.PaymentLinks)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Branch>().Property(x => x.Latitude).HasPrecision(10, 7);
        modelBuilder.Entity<Branch>().Property(x => x.Longitude).HasPrecision(10, 7);
        modelBuilder.Entity<ContactErrorLog>().HasIndex(x => x.OccurredAtUtc);
    }
}
