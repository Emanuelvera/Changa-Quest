using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddChangas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Changas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Reward = table.Column<int>(type: "int", nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExactAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaterialsNeeded = table.Column<bool>(type: "bit", nullable: false),
                    MaterialsProvider = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    AssignmentMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSample = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Changas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Changas_IsSample",
                table: "Changas",
                column: "IsSample");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Changas");
        }
    }
}
