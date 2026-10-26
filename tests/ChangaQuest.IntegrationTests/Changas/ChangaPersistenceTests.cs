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

    [Fact]
    public async Task Mutual_cancellation_acceptance_cancels_and_releases_assignment_atomically()
    {
        var databaseName = $"ChangaQuestCancellation_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var changaId = Guid.NewGuid();
        try
        {
            await using var context = new ApplicationDbContext(options);
            await context.Database.MigrateAsync();
            var store = new ChangaStore(context);
            await store.AddAsync(new Changa
            {
                Id = changaId,
                Description = "Descripción asignada para cancelar.",
                Reward = 5000,
                Zone = "Centro",
                ExactAddress = "Dirección privada 123",
                MaterialsNeeded = false,
                AssignmentMode = AssignmentMode.FirstCome,
                Status = ChangaStatus.Assigned,
                AuthorId = "requester-id",
                Assignment = new ChangaAssignment
                {
                    Id = Guid.NewGuid(),
                    ChangaId = changaId,
                    WorkerId = "worker-id",
                    AssignedAt = DateTimeOffset.UtcNow
                },
                CreatedAt = DateTimeOffset.UtcNow
            });

            var proposal = await store.CreateCancellationRequestAsync(
                changaId, "requester-id", "worker-id");
            Assert.Equal(ChangaCancellationPersistenceStatus.Created, proposal.Status);

            var duplicate = await store.CreateCancellationRequestAsync(
                changaId, "worker-id", "requester-id");
            Assert.Equal(ChangaCancellationPersistenceStatus.Conflict, duplicate.Status);
            Assert.Equal(
                1,
                await context.Set<ChangaCancellationRequest>()
                    .CountAsync(request => request.ChangaId == changaId));

            var accepted = await store.RespondCancellationRequestAsync(
                changaId, proposal.Request!.Id, "worker-id", true);
            Assert.Equal(ChangaCancellationPersistenceStatus.Accepted, accepted.Status);

            var cancelled = await context.Changas
                .Include(changa => changa.Assignment)
                .SingleAsync(changa => changa.Id == changaId);
            Assert.Equal(ChangaStatus.Cancelled, cancelled.Status);
            Assert.Null(cancelled.Assignment);
            Assert.Equal(
                ChangaCancellationRequestStatus.Accepted,
                await context.Set<ChangaCancellationRequest>()
                    .Where(request => request.ChangaId == changaId)
                    .Select(request => request.Status)
                    .SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Taking_a_changa_persists_one_unique_assignment_and_is_idempotent()
    {
        var databaseName = $"ChangaQuestChangaAssignment_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var changaId = Guid.NewGuid();
        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                await new ChangaStore(context).AddAsync(new Changa
                {
                    Id = changaId,
                    Description = "Pintar una pared interior de una habitación.",
                    Reward = 15000,
                    Zone = "General Paz",
                    ExactAddress = "Dirección privada 987",
                    MaterialsNeeded = false,
                    AssignmentMode = AssignmentMode.FirstCome,
                    Status = ChangaStatus.Available,
                    AuthorId = "author-id",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var store = new ChangaStore(context);
                Assert.Equal(ChangaTakePersistenceResult.Assigned, await store.TakeAsync(changaId, "worker-id"));
            }

            await using (var retryContext = new ApplicationDbContext(options))
            {
                var store = new ChangaStore(retryContext);
                Assert.Equal(
                    ChangaTakePersistenceResult.AlreadyAssignedToWorker,
                    await store.TakeAsync(changaId, "worker-id"));
                Assert.Equal(1, await retryContext.Set<ChangaAssignment>().CountAsync());
                Assert.Equal(ChangaStatus.Assigned, await retryContext.Changas.Select(item => item.Status).SingleAsync());
            }
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Concurrent_takes_leave_one_assignment_and_one_conflict()
    {
        var databaseName = $"ChangaQuestConcurrentTake_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var changaId = Guid.NewGuid();
        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                await new ChangaStore(context).AddAsync(new Changa
                {
                    Id = changaId,
                    Description = "Pintar una pared interior de una habitación.",
                    Reward = 15000,
                    Zone = "General Paz",
                    ExactAddress = "Dirección privada 987",
                    MaterialsNeeded = false,
                    AssignmentMode = AssignmentMode.FirstCome,
                    Status = ChangaStatus.Available,
                    AuthorId = "author-id",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            var first = Task.Run(async () =>
            {
                await using var context = new ApplicationDbContext(options);
                return await new ChangaStore(context).TakeAsync(changaId, "worker-a");
            });
            var second = Task.Run(async () =>
            {
                await using var context = new ApplicationDbContext(options);
                return await new ChangaStore(context).TakeAsync(changaId, "worker-b");
            });

            var results = await Task.WhenAll(first, second);

            Assert.Equal(1, results.Count(result => result == ChangaTakePersistenceResult.Assigned));
            Assert.Equal(1, results.Count(result => result == ChangaTakePersistenceResult.AlreadyAssignedToOther));
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Add_coordination_consent_migration_persists_each_party_decision_and_discards_declined_phone()
    {
        var databaseName = $"ChangaQuestConsent_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var changaId = Guid.NewGuid();
        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var store = new ChangaStore(context);
                await store.AddAsync(new Changa
                {
                    Id = changaId,
                    Description = "Coordinar una changa con consentimiento explícito.",
                    Reward = 12000,
                    Zone = "Centro",
                    ExactAddress = "Dirección ficticia 123",
                    MaterialsNeeded = false,
                    AssignmentMode = AssignmentMode.FirstCome,
                    Status = ChangaStatus.Available,
                    AuthorId = "requester-id",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                Assert.Equal(ChangaTakePersistenceResult.Assigned, await store.TakeAsync(changaId, "worker-id"));
                await store.SetPhoneConsentAsync(changaId, true, PhoneConsentStatus.Accepted, "+54 351 12345678");
                await store.SetPhoneConsentAsync(changaId, false, PhoneConsentStatus.Declined, null);
            }

            await using var readContext = new ApplicationDbContext(options);
            var assignment = await readContext.Set<ChangaAssignment>().SingleAsync();
            Assert.Equal(PhoneConsentStatus.Accepted, assignment.RequesterPhoneConsent);
            Assert.Equal("+54 351 12345678", assignment.RequesterPhone);
            Assert.Equal(PhoneConsentStatus.Declined, assignment.WorkerPhoneConsent);
            Assert.Null(assignment.WorkerPhone);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [Fact]
    public async Task Add_coordination_consent_migration_defaults_existing_assignments_to_pending()
    {
        var databaseName = $"ChangaQuestConsentUpgrade_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);
        var changaId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync("20261022232936_AddChangaAssignments");
                context.Changas.Add(new Changa
                {
                    Id = changaId,
                    Description = "Ayudar a organizar libros en una biblioteca pequeña.",
                    Reward = 12000,
                    Zone = "Centro",
                    ExactAddress = "Dirección ficticia 123",
                    MaterialsNeeded = false,
                    AssignmentMode = AssignmentMode.FirstCome,
                    Status = ChangaStatus.Assigned,
                    AuthorId = "requester-id",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO [ChangaAssignment] ([Id], [ChangaId], [WorkerId], [AssignedAt]) VALUES ({assignmentId}, {changaId}, {"worker-id"}, {DateTimeOffset.UtcNow})");

                await context.Database.MigrateAsync();
                context.ChangeTracker.Clear();
                var assignment = await context.Set<ChangaAssignment>().SingleAsync(item => item.Id == assignmentId);
                Assert.Equal(PhoneConsentStatus.Pending, assignment.RequesterPhoneConsent);
                Assert.Null(assignment.RequesterPhone);
                Assert.Equal(PhoneConsentStatus.Pending, assignment.WorkerPhoneConsent);
                Assert.Null(assignment.WorkerPhone);
            }
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
