using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Changas",
                type: "float(9)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Changas",
                type: "float(9)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SearchLatitude",
                table: "AspNetUsers",
                type: "float(9)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SearchLongitude",
                table: "AspNetUsers",
                type: "float(9)",
                precision: 9,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Changas");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Changas");

            migrationBuilder.DropColumn(
                name: "SearchLatitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SearchLongitude",
                table: "AspNetUsers");
        }
    }
}
