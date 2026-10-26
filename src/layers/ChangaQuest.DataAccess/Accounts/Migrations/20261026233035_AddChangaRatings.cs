using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddChangaRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangaRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RateeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangaRatings", x => x.Id);
                    table.CheckConstraint("CK_ChangaRatings_Score", "[Score] >= 1 AND [Score] <= 5");
                    table.ForeignKey(
                        name: "FK_ChangaRatings_Changas_ChangaId",
                        column: x => x.ChangaId,
                        principalTable: "Changas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangaRatings_ChangaId_RaterId",
                table: "ChangaRatings",
                columns: new[] { "ChangaId", "RaterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangaRatings_RateeId",
                table: "ChangaRatings",
                column: "RateeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangaRatings");
        }
    }
}
