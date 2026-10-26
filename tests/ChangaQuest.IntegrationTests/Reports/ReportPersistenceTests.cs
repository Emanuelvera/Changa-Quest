using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.DataAccess.Reports;
using ChangaQuest.Entities.Changas;
using ChangaQuest.Entities.Reports;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.IntegrationTests.Reports;

public class ReportPersistenceTests
{
    [Fact]
    public async Task Migration_persists_a_report_with_and_without_evidence_and_restricts_unknown_changas()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ChangaQuestReports_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        try
        {
            await using var context = new ApplicationDbContext(options);
            await context.Database.MigrateAsync();
            var changa = new Changa
            {
                Id = Guid.NewGuid(),
                Description = "Una changa descriptiva de prueba.",
                CategoryId = "limpieza-domestica",
                Reward = 1000,
                Zone = "Nueva Córdoba",
                ExactAddress = "Dirección ficticia 1",
                Status = ChangaStatus.Available,
                AssignmentMode = AssignmentMode.FirstCome,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await new ChangaStore(context).AddAsync(changa);
            var store = new ReportStore(context);
            var withEvidence = NewReport(changa.Id, "Evidencia");
            var withoutEvidence = NewReport(changa.Id, null);

            await store.AddAsync(withEvidence);
            await store.AddAsync(withoutEvidence);

            await using var readContext = new ApplicationDbContext(options);
            var readStore = new ReportStore(readContext);
            var first = await readStore.FindByIdAsync(withEvidence.Id);
            var second = await readStore.FindByIdAsync(withoutEvidence.Id);
            Assert.Equal("Evidencia", first!.Evidence);
            Assert.Equal(ReportStatus.Pending, first.Status);
            Assert.Equal(changa.Id, first.ChangaId);
            Assert.Null(second!.Evidence);
            Assert.Null(await readStore.FindByIdAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<DbUpdateException>(() => store.AddAsync(NewReport(Guid.NewGuid(), null)));
        }
        finally
        {
            await using var cleanup = new ApplicationDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static Report NewReport(Guid changaId, string? evidence) => new()
    {
        Id = Guid.NewGuid(),
        ReporterId = "reporter-id",
        ChangaId = changaId,
        Reason = "Motivo del reporte",
        Evidence = evidence,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
