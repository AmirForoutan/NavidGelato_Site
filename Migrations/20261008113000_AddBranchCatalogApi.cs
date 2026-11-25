using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261008113000_AddBranchCatalogApi")]
public partial class AddBranchCatalogApi : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ProductCode",
            table: "MenuItems",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("UPDATE [MenuItems] SET [ProductCode] = [Id] WHERE [ProductCode] IS NULL;");

        migrationBuilder.AddColumn<string>(
            name: "SecurityKey",
            table: "Branches",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "BranchMenuCategories",
            columns: table => new
            {
                BranchId = table.Column<int>(type: "int", nullable: false),
                MenuCategoryId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BranchMenuCategories", x => new { x.BranchId, x.MenuCategoryId });
                table.ForeignKey(
                    name: "FK_BranchMenuCategories_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BranchMenuCategories_MenuCategories_MenuCategoryId",
                    column: x => x.MenuCategoryId,
                    principalTable: "MenuCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BranchMenuItems",
            columns: table => new
            {
                BranchId = table.Column<int>(type: "int", nullable: false),
                MenuItemId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BranchMenuItems", x => new { x.BranchId, x.MenuItemId });
                table.ForeignKey(
                    name: "FK_BranchMenuItems_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BranchMenuItems_MenuItems_MenuItemId",
                    column: x => x.MenuItemId,
                    principalTable: "MenuItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MenuItems_ProductCode",
            table: "MenuItems",
            column: "ProductCode",
            unique: true,
            filter: "[ProductCode] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Branches_SecurityKey",
            table: "Branches",
            column: "SecurityKey",
            unique: true,
            filter: "[SecurityKey] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_BranchMenuCategories_MenuCategoryId",
            table: "BranchMenuCategories",
            column: "MenuCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_BranchMenuItems_MenuItemId",
            table: "BranchMenuItems",
            column: "MenuItemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BranchMenuCategories");
        migrationBuilder.DropTable(name: "BranchMenuItems");

        migrationBuilder.DropIndex(
            name: "IX_MenuItems_ProductCode",
            table: "MenuItems");

        migrationBuilder.DropIndex(
            name: "IX_Branches_SecurityKey",
            table: "Branches");

        migrationBuilder.DropColumn(
            name: "ProductCode",
            table: "MenuItems");

        migrationBuilder.DropColumn(
            name: "SecurityKey",
            table: "Branches");
    }
}
