using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftOfTheGivers.Data.Migrations
{
    /// <inheritdoc />
    public partial class P1PaymentsEmailAndStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Volunteer_Status",
                table: "Volunteers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Volunteers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ReliefProjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);


            migrationBuilder.AddColumn<string>(
                name: "DonorEmail",
                table: "Donations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Donations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetEntity = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Handled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inquiries", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Volunteer_Status",
                table: "Volunteers",
                sql: "[Status] IN ('Pending','Approved','Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_Dates",
                table: "ReliefProjects",
                sql: "[EndDate] IS NULL OR [EndDate] >= [StartDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_FundsRequired_NonNegative",
                table: "ReliefProjects",
                sql: "[FundsRequired] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects",
                sql: "[Status] IN ('Active','Completed','Suspended')");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Inquiries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Volunteer_Status",
                table: "Volunteers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_Dates",
                table: "ReliefProjects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_FundsRequired_NonNegative",
                table: "ReliefProjects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects");

            migrationBuilder.DropIndex(
                name: "IX_Donations_IdempotencyKey",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "DonorEmail",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Donations");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Volunteers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ReliefProjects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");


            migrationBuilder.AddCheckConstraint(
                name: "CK_Volunteer_Status",
                table: "Volunteers",
                sql: "[Status] IN ('Pending','Approved','Active','Inactive')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects",
                sql: "[Status] IN ('Planned','Active','Completed','Suspended')");
        }
    }
}
