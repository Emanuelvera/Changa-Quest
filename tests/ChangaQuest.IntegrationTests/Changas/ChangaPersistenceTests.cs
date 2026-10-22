using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.Entities.Changas;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.IntegrationTests.Changas;

public class ChangaPersistenceTests
{
    [Fact]
    public async Task Migration_creates_the_changas_table_in_sql_server()
    {
        var databaseName = $"ChangaQuestChangas_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        try
        {
            await using var context = new ApplicationDbContext(options);
            await context.Database.MigrateAsync();
            var tableId = await context.Database.SqlQueryRaw<int?>(
                "SELECT OBJECT_ID(N'dbo.Changas', N'U') AS [Value]").SingleAsync();

            Assert.NotNull(tableId);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Changa_fields_persist_and_development_samples_are_idempotent()
    {
        var databaseName = $"ChangaQuestChangaData_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var sampleId = Guid.NewGuid();
        var changaId = Guid.NewGuid();
        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var store = new ChangaStore(context);
                var sample = new Changa
                {
                    Id = sampleId,
                    Description = "Ejemplo ficticio: organizar libros.",
                    Reward = 5000,
                    Zone = "Centro",
                    ExactAddress = "Dirección ficticia 123",
                    MaterialsNeeded = false,
                    AssignmentMode = AssignmentMode.FirstCome,
                    Status = ChangaStatus.Available,
                    IsSample = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await store.EnsureSamplesAsync([sample]);
                await store.EnsureSamplesAsync([sample]);
                await store.AddAsync(new Changa
                {
                    Id = changaId,
                    Description = "Pintar una pared interior de una habitación.",
                    Reward = 15000,
                    Zone = "General Paz",
                    ExactAddress = "Dirección privada 987",
                    MaterialsNeeded = true,
                    MaterialsProvider = MaterialsProvider.Worker,
                    AssignmentMode = AssignmentMode.FirstCome,
                    Status = ChangaStatus.Available,
                    AuthorId = "verified-user",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await using var readContext = new ApplicationDbContext(options);
            var stored = await readContext.Changas.SingleAsync(changa => changa.Id == changaId);
            Assert.Equal("Dirección privada 987", stored.ExactAddress);
            Assert.Equal("verified-user", stored.AuthorId);
            Assert.Equal(MaterialsProvider.Worker, stored.MaterialsProvider);
            Assert.Equal(1, await readContext.Changas.CountAsync(changa => changa.Id == sampleId));
            Assert.True((await readContext.Changas.SingleAsync(changa => changa.Id == sampleId)).IsSample);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string databaseName) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

    private static async Task DeleteDatabaseAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var cleanup = new ApplicationDbContext(options);
        await cleanup.Database.EnsureDeletedAsync();
    }
}
