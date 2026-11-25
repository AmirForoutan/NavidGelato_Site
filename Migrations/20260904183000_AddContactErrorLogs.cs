using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904183000_AddContactErrorLogs")]
public partial class AddContactErrorLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContactErrorLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RequestName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                RequestMobile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                RequestMessage = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                ExceptionType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                TechnicalDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RequestPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ContactErrorLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ContactErrorLogs_OccurredAtUtc",
            table: "ContactErrorLogs",
            column: "OccurredAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ContactErrorLogs");
    }
}
