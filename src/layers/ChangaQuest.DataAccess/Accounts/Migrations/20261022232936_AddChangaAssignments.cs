using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddChangaAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangaAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangaAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangaAssignment_Changas_ChangaId",
                        column: x => x.ChangaId,
                        principalTable: "Changas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangaAssignment_ChangaId",
                table: "ChangaAssignment",
                column: "ChangaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangaAssignment");
        }
    }
}
