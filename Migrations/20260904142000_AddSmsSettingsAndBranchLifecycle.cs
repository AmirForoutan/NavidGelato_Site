using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904142000_AddSmsSettingsAndBranchLifecycle")]
public partial class AddSmsSettingsAndBranchLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "OpeningDateTime",
            table: "Branches",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Status",
            table: "Branches",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "SmsSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Enabled = table.Column<bool>(type: "bit", nullable: false),
                Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                EncryptedPassword = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                From = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                RecipientNumbers = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SmsSettings", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SmsSettings");
        migrationBuilder.DropColumn(name: "OpeningDateTime", table: "Branches");
        migrationBuilder.DropColumn(name: "Status", table: "Branches");
    }
}
