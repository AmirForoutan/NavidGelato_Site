using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904190000_AddSiteSettings")]
public partial class AddSiteSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[SiteSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [SiteSettings]
    (
        [Id] int NOT NULL IDENTITY,
        [EnamadHtml] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
    );
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiteSettings");
    }
}
