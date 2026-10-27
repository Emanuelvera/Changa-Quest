using ChangaQuest.DataAccess.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ChangaQuest.IntegrationTests.Accounts;

public sealed class MigrationDiscoveryTests
{
    [Fact]
    public async Task DiscoversAccountBlocksMigration()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ChangaQuestMigrationDiscovery;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        await using var context = new ApplicationDbContext(options);
        var migrations = context.Database
            .GetService<IMigrationsAssembly>()
            .Migrations;

        Assert.Contains("20261027110000_AddAccountBlocks", migrations.Keys);
    }
}
