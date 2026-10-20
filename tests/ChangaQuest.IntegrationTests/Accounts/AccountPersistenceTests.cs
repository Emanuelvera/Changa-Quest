using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.Entities.Accounts;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.IntegrationTests.Accounts;

public class AccountPersistenceTests
{
    [Fact]
    public async Task Adult_declaration_and_unverified_state_persist_in_sql_server()
    {
        var databaseName = $"ChangaQuestIntegration_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                context.Users.Add(new AppUser
                {
                    UserName = "adult@example.test",
                    Email = "adult@example.test",
                    NormalizedUserName = "ADULT@EXAMPLE.TEST",
                    NormalizedEmail = "ADULT@EXAMPLE.TEST",
                    DeclaredAdult = true,
                    EmailConfirmed = false
                });
                await context.SaveChangesAsync();
            }

            await using var readContext = new ApplicationDbContext(options);
            var account = await readContext.Users.SingleAsync(user => user.Email == "adult@example.test");

            Assert.True(account.DeclaredAdult);
            Assert.False(account.EmailConfirmed);
            Assert.DoesNotContain(
                typeof(AppUser).GetProperties(),
                property => property.Name.Contains("Birth", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Contains("IdentityDocument", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await using var cleanup = new ApplicationDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
