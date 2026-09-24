using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftOfTheGivers.Data.Migrations
{
    /// <inheritdoc />
    public partial class P0Phase0CriticalFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_AspNetUsers_DonorId",
                table: "Donations");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_AspNetUsers_DonorId",
                table: "Donations",
                column: "DonorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_Donations_Amount_Positive', 'C') IS NULL
BEGIN
    ALTER TABLE Donations ADD CONSTRAINT CK_Donations_Amount_Positive CHECK (Amount > 0);
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_ReliefProjects_FundsRequired_NonNegative', 'C') IS NULL
BEGIN
    ALTER TABLE ReliefProjects ADD CONSTRAINT CK_ReliefProjects_FundsRequired_NonNegative CHECK (FundsRequired >= 0);
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_ReliefProjects_Dates', 'C') IS NULL
BEGIN
    ALTER TABLE ReliefProjects ADD CONSTRAINT CK_ReliefProjects_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_ReliefProjects_Dates', 'C') IS NOT NULL
BEGIN
    ALTER TABLE ReliefProjects DROP CONSTRAINT CK_ReliefProjects_Dates;
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_ReliefProjects_FundsRequired_NonNegative', 'C') IS NOT NULL
BEGIN
    ALTER TABLE ReliefProjects DROP CONSTRAINT CK_ReliefProjects_FundsRequired_NonNegative;
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('CK_Donations_Amount_Positive', 'C') IS NOT NULL
BEGIN
    ALTER TABLE Donations DROP CONSTRAINT CK_Donations_Amount_Positive;
END");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_AspNetUsers_DonorId",
                table: "Donations");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_AspNetUsers_DonorId",
                table: "Donations",
                column: "DonorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
