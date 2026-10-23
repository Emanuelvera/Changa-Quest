using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinationConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequesterPhone",
                table: "ChangaAssignment",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterPhoneConsent",
                table: "ChangaAssignment",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "WorkerPhone",
                table: "ChangaAssignment",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerPhoneConsent",
                table: "ChangaAssignment",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequesterPhone",
                table: "ChangaAssignment");

            migrationBuilder.DropColumn(
                name: "RequesterPhoneConsent",
                table: "ChangaAssignment");

            migrationBuilder.DropColumn(
                name: "WorkerPhone",
                table: "ChangaAssignment");

            migrationBuilder.DropColumn(
                name: "WorkerPhoneConsent",
                table: "ChangaAssignment");
        }
    }
}
