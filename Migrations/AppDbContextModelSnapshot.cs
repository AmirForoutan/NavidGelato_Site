using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 128);
        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<string>("FriendlyName").HasColumnType("nvarchar(max)");
            b.Property<string>("Xml").HasColumnType("nvarchar(max)");
            b.HasKey("Id");
            b.ToTable("DataProtectionKeys");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
        {
            b.Property<string>("Id").HasColumnType("nvarchar(450)");
            b.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("nvarchar(max)");
            b.Property<string>("Name").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.Property<string>("NormalizedName").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id"); b.HasIndex("NormalizedName").IsUnique().HasDatabaseName("RoleNameIndex").HasFilter("[NormalizedName] IS NOT NULL"); b.ToTable("AspNetRoles");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<string>("ClaimType").HasColumnType("nvarchar(max)"); b.Property<string>("ClaimValue").HasColumnType("nvarchar(max)"); b.Property<string>("RoleId").IsRequired().HasColumnType("nvarchar(450)");
            b.HasKey("Id"); b.HasIndex("RoleId"); b.ToTable("AspNetRoleClaims");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<string>("ClaimType").HasColumnType("nvarchar(max)"); b.Property<string>("ClaimValue").HasColumnType("nvarchar(max)"); b.Property<string>("UserId").IsRequired().HasColumnType("nvarchar(450)");
            b.HasKey("Id"); b.HasIndex("UserId"); b.ToTable("AspNetUserClaims");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
        {
            b.Property<string>("LoginProvider").HasColumnType("nvarchar(450)"); b.Property<string>("ProviderKey").HasColumnType("nvarchar(450)"); b.Property<string>("ProviderDisplayName").HasColumnType("nvarchar(max)"); b.Property<string>("UserId").IsRequired().HasColumnType("nvarchar(450)");
            b.HasKey("LoginProvider","ProviderKey"); b.HasIndex("UserId"); b.ToTable("AspNetUserLogins");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
        {
            b.Property<string>("UserId").HasColumnType("nvarchar(450)"); b.Property<string>("RoleId").HasColumnType("nvarchar(450)"); b.HasKey("UserId","RoleId"); b.HasIndex("RoleId"); b.ToTable("AspNetUserRoles");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
        {
            b.Property<string>("UserId").HasColumnType("nvarchar(450)"); b.Property<string>("LoginProvider").HasColumnType("nvarchar(450)"); b.Property<string>("Name").HasColumnType("nvarchar(450)"); b.Property<string>("Value").HasColumnType("nvarchar(max)"); b.HasKey("UserId","LoginProvider","Name"); b.ToTable("AspNetUserTokens");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.ApplicationUser", b =>
        {
            b.Property<string>("Id").HasColumnType("nvarchar(450)"); b.Property<int>("AccessFailedCount").HasColumnType("int"); b.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("nvarchar(max)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2"); b.Property<string>("DisplayName").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)"); b.Property<string>("Email").HasMaxLength(256).HasColumnType("nvarchar(256)"); b.Property<bool>("EmailConfirmed").HasColumnType("bit"); b.Property<bool>("IsEnabled").HasColumnType("bit"); b.Property<bool>("LockoutEnabled").HasColumnType("bit"); b.Property<DateTimeOffset?>("LockoutEnd").HasColumnType("datetimeoffset"); b.Property<bool>("MustChangePassword").HasColumnType("bit"); b.Property<string>("NormalizedEmail").HasMaxLength(256).HasColumnType("nvarchar(256)"); b.Property<string>("NormalizedUserName").HasMaxLength(256).HasColumnType("nvarchar(256)"); b.Property<string>("PasswordHash").HasColumnType("nvarchar(max)"); b.Property<string>("PhoneNumber").HasColumnType("nvarchar(max)"); b.Property<bool>("PhoneNumberConfirmed").HasColumnType("bit"); b.Property<string>("SecurityStamp").HasColumnType("nvarchar(max)"); b.Property<bool>("TwoFactorEnabled").HasColumnType("bit"); b.Property<string>("UserName").HasMaxLength(256).HasColumnType("nvarchar(256)");
            b.HasKey("Id"); b.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex"); b.HasIndex("NormalizedUserName").IsUnique().HasDatabaseName("UserNameIndex").HasFilter("[NormalizedUserName] IS NOT NULL"); b.ToTable("AspNetUsers");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.Branch", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id")); b.Property<string>("Address").IsRequired().HasMaxLength(700).HasColumnType("nvarchar(700)"); b.Property<string>("BaladUrl").HasMaxLength(1000).HasColumnType("nvarchar(1000)"); b.Property<string>("City").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)"); b.Property<string>("GoogleMapsUrl").HasMaxLength(1000).HasColumnType("nvarchar(1000)"); b.Property<bool>("IsActive").HasColumnType("bit"); b.Property<bool>("IsCentral").HasColumnType("bit"); b.Property<decimal?>("Latitude").HasPrecision(10,7).HasColumnType("decimal(10,7)"); b.Property<decimal?>("Longitude").HasPrecision(10,7).HasColumnType("decimal(10,7)"); b.Property<string>("Iban").HasMaxLength(26).HasColumnType("nvarchar(26)"); b.Property<string>("Name").IsRequired().HasMaxLength(160).HasColumnType("nvarchar(160)"); b.Property<string>("NeshanUrl").HasMaxLength(1000).HasColumnType("nvarchar(1000)"); b.Property<DateTime?>("OpeningDateTime").HasColumnType("datetime2"); b.Property<string>("SecurityKey").HasMaxLength(128).HasColumnType("nvarchar(128)"); b.Property<int>("SortOrder").HasColumnType("int"); b.Property<int>("Status").HasColumnType("int"); b.Property<string>("WazeUrl").HasMaxLength(1000).HasColumnType("nvarchar(1000)"); b.HasKey("Id"); b.HasIndex("SecurityKey").IsUnique().HasFilter("[SecurityKey] IS NOT NULL"); b.ToTable("Branches");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.BranchPhone", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id")); b.Property<int>("BranchId").HasColumnType("int"); b.Property<string>("PhoneNumber").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)"); b.Property<int>("SortOrder").HasColumnType("int"); b.HasKey("Id"); b.HasIndex("BranchId"); b.ToTable("BranchPhones");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.BranchPaymentLink", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd().HasColumnType("bigint"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));
            b.Property<long>("Amount").HasColumnType("bigint");
            b.Property<int>("BranchId").HasColumnType("int");
            b.Property<string>("CellNumber").HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<DateTime?>("ExpiresAtUtc").HasColumnType("datetime2");
            b.Property<string>("FailureMessage").HasMaxLength(1200).HasColumnType("nvarchar(1200)");
            b.Property<string>("GatewayRequestId").HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<DateTime?>("GatewayRequestStartedAtUtc").HasColumnType("datetime2");
            b.Property<string>("GatewayToken").HasMaxLength(600).HasColumnType("nvarchar(600)");
            b.Property<DateTime?>("LastCallbackAtUtc").HasColumnType("datetime2");
            b.Property<DateTime?>("OpenedAtUtc").HasColumnType("datetime2");
            b.Property<DateTime?>("PaidAtUtc").HasColumnType("datetime2");
            b.Property<string>("PublicToken").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("ShortCode").HasMaxLength(16).HasColumnType("nvarchar(16)");
            b.Property<string>("RefNum").HasMaxLength(160).HasColumnType("nvarchar(160)");
            b.Property<string>("ResNum").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("SettlementIban").IsRequired().HasMaxLength(26).HasColumnType("nvarchar(26)");
            b.Property<int>("Status").HasColumnType("int");
            b.Property<string>("TraceNo").HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<long?>("VerifiedAmount").HasColumnType("bigint");
            b.HasKey("Id");
            b.HasIndex("PublicToken").IsUnique();
            b.HasIndex("ShortCode").IsUnique().HasFilter("[ShortCode] IS NOT NULL");
            b.HasIndex("RefNum").IsUnique().HasFilter("[RefNum] IS NOT NULL");
            b.HasIndex("ResNum").IsUnique();
            b.HasIndex("BranchId", "CreatedAtUtc");
            b.ToTable("BranchPaymentLinks");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.PaymentGatewaySettings", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<string>("DefaultPurchaseId").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<bool>("Enabled").HasColumnType("bit");
            b.Property<string>("PaymentUrlTemplate").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<string>("PublicBaseUrl").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("TerminalId").IsRequired().HasMaxLength(64).HasColumnType("nvarchar(64)");
            b.Property<string>("TokenUrl").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("datetime2");
            b.Property<string>("VerifyUrl").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.HasKey("Id");
            b.ToTable("PaymentGatewaySettings");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.BranchMenuCategory", b =>
        {
            b.Property<int>("BranchId").HasColumnType("int");
            b.Property<int>("MenuCategoryId").HasColumnType("int");
            b.HasKey("BranchId", "MenuCategoryId");
            b.HasIndex("MenuCategoryId");
            b.ToTable("BranchMenuCategories");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.BranchMenuItem", b =>
        {
            b.Property<int>("BranchId").HasColumnType("int");
            b.Property<int>("MenuItemId").HasColumnType("int");
            b.HasKey("BranchId", "MenuItemId");
            b.HasIndex("MenuItemId");
            b.ToTable("BranchMenuItems");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.ContactErrorLog", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<string>("ErrorMessage").IsRequired().HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<string>("ExceptionType").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("IntendedSmsText").HasMaxLength(4000).HasColumnType("nvarchar(4000)");
            b.Property<DateTime>("OccurredAtUtc").HasColumnType("datetime2");
            b.Property<string>("RequestMessage").IsRequired().HasMaxLength(1200).HasColumnType("nvarchar(1200)");
            b.Property<string>("RequestMobile").HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>("RequestName").IsRequired().HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<string>("RequestPath").IsRequired().HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<string>("TechnicalDetails").HasColumnType("nvarchar(max)");
            b.HasKey("Id"); b.HasIndex("OccurredAtUtc"); b.ToTable("ContactErrorLogs");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.MenuCategory", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id")); b.Property<int>("GroupCode").HasColumnType("int"); b.Property<bool>("IsActive").HasColumnType("bit"); b.Property<string>("Slug").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)"); b.Property<int>("SortOrder").HasColumnType("int"); b.Property<string>("Subtitle").HasMaxLength(500).HasColumnType("nvarchar(500)"); b.Property<string>("Title").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)"); b.HasKey("Id"); b.HasIndex("GroupCode").IsUnique(); b.HasIndex("Slug").IsUnique(); b.ToTable("MenuCategories");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.MenuItem", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id")); b.Property<string>("Description").HasMaxLength(1000).HasColumnType("nvarchar(1000)"); b.Property<bool>("IsActive").HasColumnType("bit"); b.Property<bool>("IsFeatured").HasColumnType("bit"); b.Property<int>("MenuCategoryId").HasColumnType("int"); b.Property<string>("Name").IsRequired().HasMaxLength(180).HasColumnType("nvarchar(180)"); b.Property<long>("Price").HasColumnType("bigint"); b.Property<int?>("ProductCode").HasColumnType("int"); b.Property<int>("SortOrder").HasColumnType("int"); b.HasKey("Id"); b.HasIndex("MenuCategoryId"); b.HasIndex("ProductCode").IsUnique().HasFilter("[ProductCode] IS NOT NULL"); b.ToTable("MenuItems");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.MenuItemImage", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id")); b.Property<string>("AltText").HasMaxLength(250).HasColumnType("nvarchar(250)"); b.Property<string>("FilePath").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)"); b.Property<bool>("IsPrimary").HasColumnType("bit"); b.Property<int>("MenuItemId").HasColumnType("int"); b.Property<int>("SortOrder").HasColumnType("int"); b.HasKey("Id"); b.HasIndex("MenuItemId"); b.ToTable("MenuItemImages");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.SiteSettings", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<string>("EnamadHtml").HasMaxLength(12000).HasColumnType("nvarchar(max)");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("datetime2");
            b.HasKey("Id"); b.ToTable("SiteSettings");
        });
        modelBuilder.Entity("NavidGelato_Site.Models.SmsSettings", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int"); SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id")); b.Property<bool>("Enabled").HasColumnType("bit"); b.Property<string>("EncryptedPassword").IsRequired().HasMaxLength(3000).HasColumnType("nvarchar(3000)"); b.Property<string>("From").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)"); b.Property<string>("RecipientNumbers").IsRequired().HasMaxLength(3000).HasColumnType("nvarchar(3000)"); b.Property<string>("PaymentLinkTemplate").IsRequired().HasMaxLength(3000).HasColumnType("nvarchar(3000)"); b.Property<bool>("MessageFilterEnabled").HasColumnType("bit"); b.Property<string>("MessageFilterTerms").IsRequired().HasMaxLength(12000).HasColumnType("nvarchar(max)"); b.Property<DateTime>("UpdatedAtUtc").HasColumnType("datetime2"); b.Property<string>("Username").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)"); b.HasKey("Id"); b.ToTable("SmsSettings");
        });
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>",b=>{b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole",null).WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade).IsRequired();});
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>",b=>{b.HasOne("NavidGelato_Site.Models.ApplicationUser",null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();});
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>",b=>{b.HasOne("NavidGelato_Site.Models.ApplicationUser",null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();});
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>",b=>{b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole",null).WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.HasOne("NavidGelato_Site.Models.ApplicationUser",null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();});
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>",b=>{b.HasOne("NavidGelato_Site.Models.ApplicationUser",null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade).IsRequired();});
        modelBuilder.Entity("NavidGelato_Site.Models.BranchPhone",b=>{b.HasOne("NavidGelato_Site.Models.Branch","Branch").WithMany("Phones").HasForeignKey("BranchId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.Navigation("Branch");});
        modelBuilder.Entity("NavidGelato_Site.Models.BranchPaymentLink",b=>{b.HasOne("NavidGelato_Site.Models.Branch","Branch").WithMany("PaymentLinks").HasForeignKey("BranchId").OnDelete(DeleteBehavior.Restrict).IsRequired();b.Navigation("Branch");});
        modelBuilder.Entity("NavidGelato_Site.Models.BranchMenuCategory",b=>{b.HasOne("NavidGelato_Site.Models.Branch","Branch").WithMany("MenuCategoryLinks").HasForeignKey("BranchId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.HasOne("NavidGelato_Site.Models.MenuCategory","MenuCategory").WithMany("BranchLinks").HasForeignKey("MenuCategoryId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.Navigation("Branch");b.Navigation("MenuCategory");});
        modelBuilder.Entity("NavidGelato_Site.Models.BranchMenuItem",b=>{b.HasOne("NavidGelato_Site.Models.Branch","Branch").WithMany("MenuItemLinks").HasForeignKey("BranchId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.HasOne("NavidGelato_Site.Models.MenuItem","MenuItem").WithMany("BranchLinks").HasForeignKey("MenuItemId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.Navigation("Branch");b.Navigation("MenuItem");});
        modelBuilder.Entity("NavidGelato_Site.Models.MenuItem",b=>{b.HasOne("NavidGelato_Site.Models.MenuCategory","MenuCategory").WithMany("Items").HasForeignKey("MenuCategoryId").OnDelete(DeleteBehavior.Restrict).IsRequired();b.Navigation("MenuCategory");});
        modelBuilder.Entity("NavidGelato_Site.Models.MenuItemImage",b=>{b.HasOne("NavidGelato_Site.Models.MenuItem","MenuItem").WithMany("Images").HasForeignKey("MenuItemId").OnDelete(DeleteBehavior.Cascade).IsRequired();b.Navigation("MenuItem");});
#pragma warning restore 612, 618
    }
}
