using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261009225500_AddCategoryGroupCode")]
public partial class AddCategoryGroupCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "GroupCode",
            table: "MenuCategories",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("UPDATE [MenuCategories] SET [GroupCode] = [Id] WHERE [GroupCode] IS NULL;");

        migrationBuilder.AlterColumn<int>(
            name: "GroupCode",
            table: "MenuCategories",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MenuCategories_GroupCode",
            table: "MenuCategories",
            column: "GroupCode",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MenuCategories_GroupCode",
            table: "MenuCategories");

        migrationBuilder.DropColumn(
            name: "GroupCode",
            table: "MenuCategories");
    }
}
