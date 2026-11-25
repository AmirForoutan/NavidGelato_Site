using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904102000_InitialAdminAndCatalog")]
public partial class InitialAdminAndCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminUsers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AdminUsers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Branches",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Address = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                NeshanUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                BaladUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                WazeUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                GoogleMapsUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsCentral = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Branches", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MenuCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MenuCategories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BranchPhones",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                BranchId = table.Column<int>(type: "int", nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BranchPhones", x => x.Id);
                table.ForeignKey("FK_BranchPhones_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MenuItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                MenuCategoryId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Price = table.Column<long>(type: "bigint", nullable: false),
                IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MenuItems", x => x.Id);
                table.ForeignKey("FK_MenuItems_MenuCategories_MenuCategoryId", x => x.MenuCategoryId, "MenuCategories", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MenuItemImages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                MenuItemId = table.Column<int>(type: "int", nullable: false),
                FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                AltText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MenuItemImages", x => x.Id);
                table.ForeignKey("FK_MenuItemImages_MenuItems_MenuItemId", x => x.MenuItemId, "MenuItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AdminUsers_UserName", "AdminUsers", "UserName", unique: true);
        migrationBuilder.CreateIndex("IX_BranchPhones_BranchId", "BranchPhones", "BranchId");
        migrationBuilder.CreateIndex("IX_MenuCategories_Slug", "MenuCategories", "Slug", unique: true);
        migrationBuilder.CreateIndex("IX_MenuItemImages_MenuItemId", "MenuItemImages", "MenuItemId");
        migrationBuilder.CreateIndex("IX_MenuItems_MenuCategoryId", "MenuItems", "MenuCategoryId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AdminUsers");
        migrationBuilder.DropTable("BranchPhones");
        migrationBuilder.DropTable("MenuItemImages");
        migrationBuilder.DropTable("Branches");
        migrationBuilder.DropTable("MenuItems");
        migrationBuilder.DropTable("MenuCategories");
    }
}
