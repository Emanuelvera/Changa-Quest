using ChangaQuest.Entities.Changas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChangaQuest.DataAccess.Changas;

public static class ChangaDataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddChangaDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IChangaStore, ChangaStore>();
        services.AddHostedService<ChangaDevelopmentSampleSeeder>();
        return services;
    }
}

internal sealed class ChangaDevelopmentSampleSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment) : IHostedService
{
    private static readonly IReadOnlyCollection<Changa> Samples =
    [
        new Changa
        {
            Id = Guid.Parse("a1000000-0000-4000-8000-000000000001"),
            Description = "Ejemplo ficticio: ordenar libros y armar una biblioteca pequeña.",
            Reward = 12000,
            Zone = "Nueva Córdoba",
            ExactAddress = "Dirección ficticia de ejemplo 101",
            Latitude = -31.4167,
            Longitude = -64.1833,
            MaterialsNeeded = false,
            AssignmentMode = AssignmentMode.FirstCome,
            Status = ChangaStatus.Available,
            IsSample = true,
            CreatedAt = DateTimeOffset.Parse("2026-01-01T12:00:00Z")
        },
        new Changa
        {
            Id = Guid.Parse("a1000000-0000-4000-8000-000000000002"),
            Description = "Ejemplo ficticio: ayudar a mover cajas livianas dentro de una vivienda.",
            Reward = 18000,
            Zone = "General Paz",
            ExactAddress = "Dirección ficticia de ejemplo 202",
            Latitude = -31.4000,
            Longitude = -64.1700,
            MaterialsNeeded = true,
            MaterialsProvider = MaterialsProvider.Requester,
            AssignmentMode = AssignmentMode.FirstCome,
            Status = ChangaStatus.Available,
            IsSample = true,
            CreatedAt = DateTimeOffset.Parse("2026-01-01T13:00:00Z")
        }
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IChangaStore>();
        await store.EnsureSamplesAsync(Samples);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
