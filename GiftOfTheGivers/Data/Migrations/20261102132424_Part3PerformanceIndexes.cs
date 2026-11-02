using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftOfTheGivers.Data.Migrations
{
    /// <inheritdoc />
    public partial class Part3PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot alter a column that a CHECK constraint references, and cannot
            // index nvarchar(max). Drop the constraint, narrow the column, then restore it.
            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ReliefProjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects",
                sql: "[Status] IN ('Active','Completed','Suspended')");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefProjects_Status_CreatedDate",
                table: "ReliefProjects",
                columns: new[] { "Status", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReliefProjects_Status_CreatedDate",
                table: "ReliefProjects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ReliefProjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReliefProject_Status",
                table: "ReliefProjects",
                sql: "[Status] IN ('Active','Completed','Suspended')");
        }
    }
}
