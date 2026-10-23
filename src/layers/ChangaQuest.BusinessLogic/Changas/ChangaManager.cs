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
            Latitude = command.Latitude ?? ChangaPolicy.DefaultSearchLatitude,
            Longitude = command.Longitude ?? ChangaPolicy.DefaultSearchLongitude,
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
            Changas: changas.Select(changa => Project(changa)).ToArray());
    }

    public async Task<ChangaManagerResult> SearchAvailableAsync(string? userId, ChangaSearchQuery query)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var radius = query.RadiusKm ?? 3;
        if (radius is not (1 or 3 or 5 or 10))
        {
            return new ChangaManagerResult(
                ChangaManagerStatus.Invalid,
                Errors: new Dictionary<string, string[]>
                {
                    ["radiusKm"] = ["El radio debe ser de 1, 3, 5 o 10 km."]
                });
        }

        var user = await accountStore.FindByIdAsync(userId!);
        var latitude = query.Latitude ?? user?.SearchLatitude ?? ChangaPolicy.DefaultSearchLatitude;
        var longitude = query.Longitude ?? user?.SearchLongitude ?? ChangaPolicy.DefaultSearchLongitude;
        if (!IsValidCoordinate(latitude, longitude))
        {
            return new ChangaManagerResult(
                ChangaManagerStatus.Invalid,
                Errors: new Dictionary<string, string[]>
                {
                    ["location"] = ["Ingresá una ubicación manual válida."]
                });
        }

        if (query.SavePreference)
        {
            user!.SearchLatitude = latitude;
            user.SearchLongitude = longitude;
            await accountStore.SaveSearchPreferenceAsync(user);
        }

        var changas = await changaStore.ListAvailableForSearchAsync();
        return new ChangaManagerResult(
            ChangaManagerStatus.Success,
            Changas: changas
                .Select(changa => (Changa: changa, DistanceKm: DistanceInKm(
                    latitude,
                    longitude,
                    changa.Latitude,
                    changa.Longitude)))
                .Where(item => item.DistanceKm <= radius)
                .OrderBy(item => item.DistanceKm)
                .Select(item => Project(item.Changa, item.DistanceKm))
                .ToArray());
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

    private static ChangaPublicView Project(Changa changa, double distanceKm = 0) => new(
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
        "available",
        "first_come",
        changa.IsSample,
        changa.CreatedAt,
        Math.Round(distanceKm, 1));

    private static bool IsValidCoordinate(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;

    private static double DistanceInKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthRadiusKm = 6371;
        var latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        var longitudeDelta = DegreesToRadians(longitude2 - longitude1);
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
                        Math.Cos(DegreesToRadians(latitude1)) *
                        Math.Cos(DegreesToRadians(latitude2)) *
                        Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
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
    bool HasMaterialsProvider,
    double? Latitude = null,
    double? Longitude = null);

public sealed record ChangaSearchQuery(
    double? Latitude,
    double? Longitude,
    int? RadiusKm,
    bool SavePreference);

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
    DateTimeOffset CreatedAt,
    double DistanceKm);

public enum ChangaManagerStatus
{
    Success,
    Invalid,
    Unauthenticated,
    Forbidden,
    NotFound
}

public sealed record ChangaManagerResult(
    ChangaManagerStatus Status,
    ChangaPublicView? Changa = null,
    IReadOnlyList<ChangaPublicView>? Changas = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
