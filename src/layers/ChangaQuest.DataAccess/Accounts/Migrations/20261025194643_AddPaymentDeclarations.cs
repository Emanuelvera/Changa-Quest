using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDeclarations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RequesterPaymentDeclaredAt",
                table: "ChangaAssignment",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WorkerPaymentDeclaredAt",
                table: "ChangaAssignment",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequesterPaymentDeclaredAt",
                table: "ChangaAssignment");

            migrationBuilder.DropColumn(
                name: "WorkerPaymentDeclaredAt",
                table: "ChangaAssignment");
        }
    }
}
