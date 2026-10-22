using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.Entities.Changas;

namespace ChangaQuest.BusinessLogic.Changas;

public sealed class ChangaManager(
    IChangaStore changaStore,
    IAccountIdentityStore accountStore)
{
    public async Task<ChangaManagerResult> CreateAsync(string? userId, ChangaCreateCommand command)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var validation = ChangaRegistrationValidator.Validate(
            command.Description,
            command.Reward,
            command.Zone,
            command.ExactAddress,
            command.MaterialsNeeded,
            command.MaterialsProvider,
            command.HasMaterialsProvider);
        if (!validation.IsValid)
        {
            return new ChangaManagerResult(ChangaManagerStatus.Invalid, Errors: validation.Errors);
        }

        var changa = new Changa
        {
            Id = Guid.NewGuid(),
            Description = command.Description!.Trim(),
            Reward = command.Reward!.Value,
            Zone = command.Zone!.Trim(),
            ExactAddress = command.ExactAddress!.Trim(),
            MaterialsNeeded = command.MaterialsNeeded!.Value,
            MaterialsProvider = ParseMaterialsProvider(command.MaterialsProvider),
            AssignmentMode = AssignmentMode.FirstCome,
            Status = ChangaStatus.Available,
            AuthorId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await changaStore.AddAsync(changa);
        return new ChangaManagerResult(ChangaManagerStatus.Success, Changa: Project(changa));
    }

    public async Task<ChangaManagerResult> ListAvailableAsync(string? userId)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var changas = await changaStore.ListAvailableAsync();
        return new ChangaManagerResult(
            ChangaManagerStatus.Success,
            Changas: changas.Select(Project).ToArray());
    }

    public async Task<ChangaManagerResult> FindAvailableAsync(string? userId, Guid id)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var changa = await changaStore.FindAvailableByIdAsync(id);
        return changa is null
            ? new ChangaManagerResult(ChangaManagerStatus.NotFound)
            : new ChangaManagerResult(ChangaManagerStatus.Success, Changa: Project(changa));
    }

    public async Task<ChangaManagerResult> TakeAsync(string? userId, Guid id)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null)
        {
            return new ChangaManagerResult(ChangaManagerStatus.NotFound);
        }

        if (changa.AuthorId == userId)
        {
            return new ChangaManagerResult(ChangaManagerStatus.SelfAssignment);
        }

        var take = await changaStore.TakeAsync(id, userId!);
        changa = await changaStore.FindByIdAsync(id) ?? changa;
        return take switch
        {
            ChangaTakePersistenceResult.Assigned or ChangaTakePersistenceResult.AlreadyAssignedToWorker =>
                new ChangaManagerResult(
                    ChangaManagerStatus.Success,
                    Changa: Project(changa),
                    Assignment: new ChangaAssignmentView(changa.Assignment!.Id, changa.Assignment.AssignedAt)),
            ChangaTakePersistenceResult.AlreadyAssignedToOther =>
                new ChangaManagerResult(ChangaManagerStatus.AlreadyAssigned),
            ChangaTakePersistenceResult.NotAvailable =>
                new ChangaManagerResult(ChangaManagerStatus.NotAvailable),
            _ => new ChangaManagerResult(ChangaManagerStatus.NotFound)
        };
    }

    private async Task<ChangaManagerResult?> CheckAccessAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ChangaManagerResult(ChangaManagerStatus.Unauthenticated);
        }

        var user = await accountStore.FindByIdAsync(userId);
        return user is null || !user.EmailConfirmed || !user.DeclaredAdult
            ? new ChangaManagerResult(ChangaManagerStatus.Forbidden)
            : null;
    }

    private static MaterialsProvider? ParseMaterialsProvider(string? value) => value switch
    {
        "requester" => MaterialsProvider.Requester,
        "worker" => MaterialsProvider.Worker,
        _ => null
    };

    private static ChangaPublicView Project(Changa changa) => new(
        changa.Id,
        changa.Description,
        changa.Reward,
        changa.Zone,
        changa.MaterialsNeeded,
        changa.MaterialsProvider switch
        {
            MaterialsProvider.Requester => "requester",
            MaterialsProvider.Worker => "worker",
            _ => null
        },
        changa.Status switch
        {
            ChangaStatus.Available => "available",
            ChangaStatus.Assigned => "assigned",
            ChangaStatus.Cancelled => "cancelled",
            _ => "available"
        },
        "first_come",
        changa.IsSample,
        changa.CreatedAt);
}

public static class ChangaRegistrationValidator
{
    public static ChangaValidationResult Validate(
        string? description,
        int? reward,
        string? zone,
        string? exactAddress,
        bool? materialsNeeded,
        string? materialsProvider,
        bool hasMaterialsProvider)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (!HasLength(description, ChangaPolicy.MinimumDescriptionLength, ChangaPolicy.MaximumDescriptionLength))
        {
            errors["description"] = [$"La descripción debe tener entre {ChangaPolicy.MinimumDescriptionLength} y {ChangaPolicy.MaximumDescriptionLength} caracteres."];
        }

        if (reward is null || reward < ChangaPolicy.MinimumReward || reward > ChangaPolicy.MaximumReward)
        {
            errors["reward"] = [$"La recompensa debe ser un importe entero entre {ChangaPolicy.MinimumReward} y {ChangaPolicy.MaximumReward} ARS."];
        }

        if (!HasLength(zone, ChangaPolicy.MinimumZoneLength, ChangaPolicy.MaximumZoneLength))
        {
            errors["zone"] = [$"La zona debe tener entre {ChangaPolicy.MinimumZoneLength} y {ChangaPolicy.MaximumZoneLength} caracteres."];
        }

        if (!HasLength(exactAddress, ChangaPolicy.MinimumExactAddressLength, ChangaPolicy.MaximumExactAddressLength))
        {
            errors["exactAddress"] = [$"La dirección debe tener entre {ChangaPolicy.MinimumExactAddressLength} y {ChangaPolicy.MaximumExactAddressLength} caracteres."];
        }

        if (materialsNeeded is null)
        {
            errors["materialsNeeded"] = ["Indicá si hacen falta materiales."];
        }
        else if (materialsNeeded.Value)
        {
            if (!hasMaterialsProvider || materialsProvider is not ("requester" or "worker"))
            {
                errors["materialsProvider"] = ["Indicá quién aporta los materiales."];
            }
        }
        else if (hasMaterialsProvider)
        {
            errors["materialsProvider"] = ["No indiques quién aporta materiales si no hacen falta."];
        }

        return new ChangaValidationResult(errors.Count == 0, errors);
    }

    private static bool HasLength(string? value, int minimum, int maximum)
    {
        var length = value?.Trim().Length ?? 0;
        return length >= minimum && length <= maximum;
    }
}

public sealed record ChangaCreateCommand(
    string? Description,
    int? Reward,
    string? Zone,
    string? ExactAddress,
    bool? MaterialsNeeded,
    string? MaterialsProvider,
    bool HasMaterialsProvider);

public sealed record ChangaValidationResult(
    bool IsValid,
    IReadOnlyDictionary<string, string[]> Errors);

public sealed record ChangaPublicView(
    Guid Id,
    string Description,
    int Reward,
    string Zone,
    bool MaterialsNeeded,
    string? MaterialsProvider,
    string Status,
    string AssignmentMode,
    bool IsSample,
    DateTimeOffset CreatedAt);

public enum ChangaManagerStatus
{
    Success,
    Invalid,
    Unauthenticated,
    Forbidden,
    NotFound,
    SelfAssignment,
    AlreadyAssigned,
    NotAvailable
}

public sealed record ChangaManagerResult(
    ChangaManagerStatus Status,
    ChangaPublicView? Changa = null,
    IReadOnlyList<ChangaPublicView>? Changas = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    ChangaAssignmentView? Assignment = null);

public sealed record ChangaAssignmentView(Guid Id, DateTimeOffset AssignedAt);
