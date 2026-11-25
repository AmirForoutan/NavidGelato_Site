using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261012120500_AddPaymentSmsTemplate")]
public partial class AddPaymentSmsTemplate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PaymentLinkTemplate",
            table: "SmsSettings",
            type: "nvarchar(3000)",
            maxLength: 3000,
            nullable: false,
            defaultValue: "نوید ژلاتو\nلینک پرداخت {amount} ریال:\n{link}\nمهلت پرداخت: ۱۵ دقیقه پس از اولین باز شدن لینک");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PaymentLinkTemplate",
            table: "SmsSettings");
    }
}
