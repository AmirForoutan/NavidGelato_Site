using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NavidGelato_Site.Data;

#nullable disable

namespace NavidGelato_Site.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261011174500_AddBranchPaymentLinks")]
public partial class AddBranchPaymentLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Iban",
            table: "Branches",
            type: "nvarchar(26)",
            maxLength: 26,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "PaymentGatewaySettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Enabled = table.Column<bool>(type: "bit", nullable: false),
                TerminalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                TokenUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                PaymentUrlTemplate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                VerifyUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                PublicBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DefaultPurchaseId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentGatewaySettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BranchPaymentLinks",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BranchId = table.Column<int>(type: "int", nullable: false),
                PublicToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ResNum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                SettlementIban = table.Column<string>(type: "nvarchar(26)", maxLength: 26, nullable: false),
                CellNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastCallbackAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                GatewayToken = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                GatewayRequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                GatewayRequestStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RefNum = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                TraceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                VerifiedAmount = table.Column<long>(type: "bigint", nullable: true),
                FailureMessage = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BranchPaymentLinks", x => x.Id);
                table.ForeignKey(
                    name: "FK_BranchPaymentLinks_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BranchPaymentLinks_BranchId_CreatedAtUtc",
            table: "BranchPaymentLinks",
            columns: new[] { "BranchId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_BranchPaymentLinks_PublicToken",
            table: "BranchPaymentLinks",
            column: "PublicToken",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BranchPaymentLinks_RefNum",
            table: "BranchPaymentLinks",
            column: "RefNum",
            unique: true,
            filter: "[RefNum] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_BranchPaymentLinks_ResNum",
            table: "BranchPaymentLinks",
            column: "ResNum",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BranchPaymentLinks");
        migrationBuilder.DropTable(name: "PaymentGatewaySettings");

        migrationBuilder.DropColumn(
            name: "Iban",
            table: "Branches");
    }
}
