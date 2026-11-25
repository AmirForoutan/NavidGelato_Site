using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261024190500_AddIntendedSmsTextToContactErrors")]
public partial class AddIntendedSmsTextToContactErrors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IntendedSmsText",
            table: "ContactErrorLogs",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IntendedSmsText",
            table: "ContactErrorLogs");
    }
}
