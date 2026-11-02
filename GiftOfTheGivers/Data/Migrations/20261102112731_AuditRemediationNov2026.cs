using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftOfTheGivers.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Remediation for the November 2026 engineering audit (AUD-2026-11-01): currency
    /// normalisation (AmountZar), payment verification metadata, offline project funds, privacy
    /// consent timestamps, optional volunteer DOB/phone, sized audit columns + details, Data
    /// Protection key storage, and removal of untracked duplicate check constraints.
    /// </summary>
    public partial class AuditRemediationNov2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Volunteers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactPhone",
                table: "Volunteers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactName",
                table: "Volunteers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Volunteers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentAt",
                table: "Volunteers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfflineFundsZar",
                table: "ReliefProjects",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentAt",
                table: "Inquiries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountZar",
                table: "Donations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DonorName",
                table: "Donations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToZar",
                table: "Donations",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentVerifiedAt",
                table: "Donations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentAt",
                table: "Donations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPaymentId",
                table: "Donations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TargetEntity",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "AuditLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            // ---- Backfill existing rows before the new constraints are enforced -------------
            // Rand value of historical donations. ZAR is exact; USD/EUR use the same indicative
            // rates as the default Currency:RatesToZar configuration (documented in the
            // remediation addendum) because no historical rate was ever stored.
            migrationBuilder.Sql(@"
UPDATE Donations SET ExchangeRateToZar = 1, AmountZar = Amount WHERE Currency = 'ZAR';
UPDATE Donations SET ExchangeRateToZar = 18.50, AmountZar = ROUND(Amount * 18.50, 2) WHERE Currency = 'USD';
UPDATE Donations SET ExchangeRateToZar = 20.00, AmountZar = ROUND(Amount * 20.00, 2) WHERE Currency = 'EUR';");

            // 'PayPal' was offered by the old form but never supported by the gateway.
            migrationBuilder.Sql(@"UPDATE Donations SET PaymentMethod = 'Card' WHERE PaymentMethod NOT IN ('Card','EFT','Cash');");

            // Rows that were already verified keep their settlement date (best available: capture date).
            migrationBuilder.Sql(@"UPDATE Donations SET PaymentVerifiedAt = DonationDate WHERE PaymentStatus = 1 AND PaymentVerifiedAt IS NULL;");

            // ---- Remove duplicate constraints added by raw SQL in P0Phase0CriticalFixes ------
            // They duplicate CK_Donation_Amount / CK_ReliefProject_* that EF manages, and EF did
            // not know they existed (schema drift).
            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_Donations_Amount_Positive', 'C') IS NOT NULL ALTER TABLE Donations DROP CONSTRAINT CK_Donations_Amount_Positive;
IF OBJECT_ID('CK_ReliefProjects_FundsRequired_NonNegative', 'C') IS NOT NULL ALTER TABLE ReliefProjects DROP CONSTRAINT CK_ReliefProjects_FundsRequired_NonNegative;
IF OBJECT_ID('CK_ReliefProjects_Dates', 'C') IS NOT NULL ALTER TABLE ReliefProjects DROP CONSTRAINT CK_ReliefProjects_Dates;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_OfflineFunds_NonNegative",
                table: "ReliefProjects",
                sql: "[OfflineFundsZar] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_PaymentStatus_DonationDate",
                table: "Donations",
                columns: new[] { "PaymentStatus", "DonationDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Donation_AmountZar",
                table: "Donations",
                sql: "[AmountZar] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Donation_ExchangeRate",
                table: "Donations",
                sql: "[ExchangeRateToZar] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Donation_PaymentMethod",
                table: "Donations",
                sql: "[PaymentMethod] IN ('Card','EFT','Cash')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_OfflineFunds_NonNegative",
                table: "ReliefProjects");

            migrationBuilder.DropIndex(
                name: "IX_Donations_PaymentStatus_DonationDate",
                table: "Donations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Donation_AmountZar",
                table: "Donations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Donation_ExchangeRate",
                table: "Donations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Donation_PaymentMethod",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentAt",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "OfflineFundsZar",
                table: "ReliefProjects");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentAt",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "AmountZar",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "DonorName",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToZar",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PaymentVerifiedAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Volunteers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactPhone",
                table: "Volunteers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactName",
                table: "Volunteers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Volunteers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TargetEntity",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_Donations_Amount_Positive', 'C') IS NULL ALTER TABLE Donations ADD CONSTRAINT CK_Donations_Amount_Positive CHECK (Amount > 0);
IF OBJECT_ID('CK_ReliefProjects_FundsRequired_NonNegative', 'C') IS NULL ALTER TABLE ReliefProjects ADD CONSTRAINT CK_ReliefProjects_FundsRequired_NonNegative CHECK (FundsRequired >= 0);
IF OBJECT_ID('CK_ReliefProjects_Dates', 'C') IS NULL ALTER TABLE ReliefProjects ADD CONSTRAINT CK_ReliefProjects_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);");
        }
    }
}
