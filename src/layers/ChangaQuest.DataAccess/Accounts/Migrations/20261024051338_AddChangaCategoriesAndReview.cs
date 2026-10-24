using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddChangaCategoriesAndReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "Changas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "Changas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChangaCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangaCategories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ChangaCategories",
                columns: new[] { "Id", "IsApproved", "Name" },
                values: new object[,]
                {
                    { "cocina-preparacion-basica", true, "Cocina o preparación básica" },
                    { "compras-mandados", true, "Compras y mandados" },
                    { "cuidado-mascotas", true, "Cuidado de mascotas" },
                    { "jardineria-basica", true, "Jardinería básica" },
                    { "limpieza-domestica", true, "Limpieza doméstica" },
                    { "organizacion-hogar", true, "Organización del hogar" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangaCategories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Changas");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "Changas");
        }
    }
}
