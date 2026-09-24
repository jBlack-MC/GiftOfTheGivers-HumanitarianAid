using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftOfTheGivers.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_ReliefProjects_ReliefProjectId",
                table: "Donations");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUpdates_ReliefProjects_ReliefProjectId",
                table: "ProjectUpdates");

            migrationBuilder.DropIndex(
                name: "IX_Donations_TransactionReference",
                table: "Donations");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ReliefProjects",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Donations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_TransactionReference",
                table: "Donations",
                column: "TransactionReference",
                unique: true,
                filter: "[TransactionReference] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_ReliefProjects_ReliefProjectId",
                table: "Donations",
                column: "ReliefProjectId",
                principalTable: "ReliefProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUpdates_ReliefProjects_ReliefProjectId",
                table: "ProjectUpdates",
                column: "ReliefProjectId",
                principalTable: "ReliefProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_ReliefProjects_ReliefProjectId",
                table: "Donations");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUpdates_ReliefProjects_ReliefProjectId",
                table: "ProjectUpdates");

            migrationBuilder.DropIndex(
                name: "IX_Donations_TransactionReference",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ReliefProjects");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Donations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_TransactionReference",
                table: "Donations",
                column: "TransactionReference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_ReliefProjects_ReliefProjectId",
                table: "Donations",
                column: "ReliefProjectId",
                principalTable: "ReliefProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUpdates_ReliefProjects_ReliefProjectId",
                table: "ProjectUpdates",
                column: "ReliefProjectId",
                principalTable: "ReliefProjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
