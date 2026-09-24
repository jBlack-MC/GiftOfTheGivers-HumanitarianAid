using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftOfTheGivers.Data.Migrations;

public partial class P0SecurityAndDonationIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "IdempotencyKey",
            table: "Donations",
            type: "uniqueidentifier",
            nullable: false,
            defaultValueSql: "NEWID()");

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TargetEntity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TargetId = table.Column<int>(type: "int", nullable: false),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Donations_IdempotencyKey",
            table: "Donations",
            column: "IdempotencyKey",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TargetEntity_TargetId",
            table: "AuditLogs",
            columns: new[] { "TargetEntity", "TargetId" });
        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_Timestamp",
            table: "AuditLogs",
            column: "Timestamp");

        migrationBuilder.AddCheckConstraint(
            name: "CK_ReliefProject_FundsRequired_NonNegative",
            table: "ReliefProjects",
            sql: "[FundsRequired] >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_ReliefProject_Dates",
            table: "ReliefProjects",
            sql: "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_ReliefProject_Dates", "ReliefProjects");
        migrationBuilder.DropCheckConstraint("CK_ReliefProject_FundsRequired_NonNegative", "ReliefProjects");
        migrationBuilder.DropTable("AuditLogs");
        migrationBuilder.DropIndex("IX_Donations_IdempotencyKey", "Donations");
        migrationBuilder.DropColumn("IdempotencyKey", "Donations");
    }
}