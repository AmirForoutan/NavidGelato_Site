using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261024183000_AddMessageProfanityFilter")]
public partial class AddMessageProfanityFilter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MessageFilterEnabled",
            table: "SmsSettings",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "MessageFilterTerms",
            table: "SmsSettings",
            type: "nvarchar(max)",
            maxLength: 12000,
            nullable: false,
            defaultValue: "احمق\nبی شعور\nبیشعور\nبی ناموس\nبیناموس\nحرامزاده\nحرومزاده\nمادر جنده\nمادرجنده\nقحبه\nجنده\nفاحشه\nکصخل\nکسخل\nگوه\nگوه خور\nگه خور\nآشغال");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MessageFilterEnabled",
            table: "SmsSettings");

        migrationBuilder.DropColumn(
            name: "MessageFilterTerms",
            table: "SmsSettings");
    }
}
