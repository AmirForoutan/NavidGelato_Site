using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261011234500_AddPaymentLinkShortCode")]
public partial class AddPaymentLinkShortCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ShortCode",
            table: "BranchPaymentLinks",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_BranchPaymentLinks_ShortCode",
            table: "BranchPaymentLinks",
            column: "ShortCode",
            unique: true,
            filter: "[ShortCode] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_BranchPaymentLinks_ShortCode",
            table: "BranchPaymentLinks");

        migrationBuilder.DropColumn(
            name: "ShortCode",
            table: "BranchPaymentLinks");
    }
}
