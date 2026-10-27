using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations;

public partial class AddAccountBlocks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountBlocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BlockerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                BlockedId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountBlocks", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AccountBlocks_BlockerId_BlockedId",
            table: "AccountBlocks",
            columns: new[] { "BlockerId", "BlockedId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "AccountBlocks");
}
