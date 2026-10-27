using ChangaQuest.DataAccess.Accounts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChangaQuest.DataAccess.Accounts.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025132000_AddCompletionFlow")]
public partial class AddCompletionFlow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CompletionRequestedAt", table: "ChangaAssignment", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CompletionDeadline", table: "ChangaAssignment", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CompletionRespondedAt", table: "ChangaAssignment", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "CompletionReviewCause", table: "ChangaAssignment", type: "nvarchar(16)", maxLength: 16, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CompletionRequestedAt", table: "ChangaAssignment");
        migrationBuilder.DropColumn(name: "CompletionDeadline", table: "ChangaAssignment");
        migrationBuilder.DropColumn(name: "CompletionRespondedAt", table: "ChangaAssignment");
        migrationBuilder.DropColumn(name: "CompletionReviewCause", table: "ChangaAssignment");
    }
}
