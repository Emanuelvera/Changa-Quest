using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations;

public partial class AddCancellationRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ChangaCancellationRequest",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChangaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                TargetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChangaCancellationRequest", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChangaCancellationRequest_Changas_ChangaId",
                    column: x => x.ChangaId,
                    principalTable: "Changas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ChangaCancellationRequest_ChangaId_Pending",
            table: "ChangaCancellationRequest",
            column: "ChangaId",
            unique: true,
            filter: "[Status] = 'Pending'");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "ChangaCancellationRequest");
}
