using ChangaQuest.DataAccess.Accounts;
using ChangaQuest.DataAccess.Changas;
using ChangaQuest.Entities.Changas;

namespace ChangaQuest.BusinessLogic.Changas;

public sealed class ChangaManager(
    IChangaStore changaStore,
    IAccountIdentityStore accountStore,
    TimeProvider? timeProvider = null)
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
        if (string.IsNullOrWhiteSpace(command.CategoryId))
        {
            validation = validation with
            {
                IsValid = false,
                Errors = validation.Errors
                    .Append(new KeyValuePair<string, string[]>("categoryId", ["Elegí una categoría aprobada."]))
                    .ToDictionary(item => item.Key, item => item.Value)
            };
        }
        else if (await changaStore.FindCategoryAsync(command.CategoryId.Trim()) is not { IsApproved: true })
        {
            return new ChangaManagerResult(
                ChangaManagerStatus.InvalidCategory,
                Errors: new Dictionary<string, string[]>
                {
                    ["categoryId"] = ["La categoría no existe o no está aprobada."]
                });
        }
        if (!validation.IsValid)
        {
            return new ChangaManagerResult(ChangaManagerStatus.Invalid, Errors: validation.Errors);
        }

        var changa = new Changa
        {
            Id = Guid.NewGuid(),
            Description = command.Description!.Trim(),
            CategoryId = command.CategoryId!.Trim(),
            Reward = command.Reward!.Value,
            Zone = command.Zone!.Trim(),
            ExactAddress = command.ExactAddress!.Trim(),
            Latitude = command.Latitude ?? ChangaPolicy.DefaultSearchLatitude,
            Longitude = command.Longitude ?? ChangaPolicy.DefaultSearchLongitude,
            MaterialsNeeded = command.MaterialsNeeded!.Value,
            MaterialsProvider = ParseMaterialsProvider(command.MaterialsProvider),
            AssignmentMode = AssignmentMode.FirstCome,
            Status = ChangaStatus.PendingReview,
            AuthorId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await changaStore.AddAsync(changa);
        return new ChangaManagerResult(ChangaManagerStatus.Success, Changa: Project(changa));
    }

    public async Task<ChangaManagerResult> ListApprovedCategoriesAsync(string? userId)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        return new ChangaManagerResult(
            ChangaManagerStatus.Success,
            Categories: await changaStore.ListApprovedCategoriesAsync());
    }

    public async Task<ChangaManagerResult> ApproveAsync(string? userId, Guid id)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null) return access;
        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        if (changa.Status != ChangaStatus.PendingReview) return new(ChangaManagerStatus.InvalidReview);
        if (await changaStore.FindCategoryAsync(changa.CategoryId) is not { IsApproved: true })
        {
            return new(
                ChangaManagerStatus.InvalidReview,
                Errors: new Dictionary<string, string[]>
                {
                    ["categoryId"] = ["La categoría ya no está aprobada."]
                });
        }
        changa.Status = ChangaStatus.Available;
        changa.ReviewReason = null;
        await changaStore.SaveAsync(changa);
        return new(ChangaManagerStatus.Success, Changa: Project(changa));
    }

    public async Task<ChangaManagerResult> RejectAsync(string? userId, Guid id, string? reason)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null) return access;
        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        if (changa.Status != ChangaStatus.PendingReview) return new(ChangaManagerStatus.InvalidReview);
        var reviewReason = string.IsNullOrWhiteSpace(reason)
            ? "La publicación no fue aprobada."
            : reason.Trim();
        if (reviewReason.Length > 500)
        {
            return new(
                ChangaManagerStatus.InvalidReview,
                Errors: new Dictionary<string, string[]>
                {
                    ["reason"] = ["El motivo no puede superar los 500 caracteres."]
                });
        }
        changa.Status = ChangaStatus.Rejected;
        changa.ReviewReason = reviewReason;
        await changaStore.SaveAsync(changa);
        return new(ChangaManagerStatus.Success, Changa: Project(changa));
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

    public async Task<ChangaManagerResult> GetCoordinationAsync(string? userId, Guid id)
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

        if (changa.Assignment is null)
        {
            return new ChangaManagerResult(ChangaManagerStatus.NotAssigned);
        }

        var role = GetCoordinationRole(changa, userId!);
        return role is null
            ? new ChangaManagerResult(ChangaManagerStatus.CoordinationForbidden)
            : new ChangaManagerResult(
                ChangaManagerStatus.Success,
                Coordination: ProjectCoordination(changa, role.Value));
    }

    public async Task<ChangaManagerResult> SetPhoneConsentAsync(
        string? userId,
        Guid id,
        bool sharePhone,
        string? phone)
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

        if (changa.Assignment is null)
        {
            return new ChangaManagerResult(ChangaManagerStatus.NotAssigned);
        }

        var role = GetCoordinationRole(changa, userId!);
        if (role is null)
        {
            return new ChangaManagerResult(ChangaManagerStatus.CoordinationForbidden);
        }

        if (sharePhone && !IsValidPhone(phone))
        {
            return new ChangaManagerResult(
                ChangaManagerStatus.InvalidPhone,
                Errors: new Dictionary<string, string[]>
                {
                    ["phone"] = ["Ingresá un teléfono ficticio válido con entre 8 y 15 dígitos."]
                });
        }

        await changaStore.SetPhoneConsentAsync(
            id,
            role == ChangaCoordinationRole.Requester,
            sharePhone ? PhoneConsentStatus.Accepted : PhoneConsentStatus.Declined,
            sharePhone ? phone!.Trim() : null);

        changa = await changaStore.FindByIdAsync(id) ?? changa;
        return new ChangaManagerResult(
            ChangaManagerStatus.Success,
            Coordination: ProjectCoordination(changa, role.Value));
    }

    public async Task<ChangaManagerResult> ListCoordinationsAsync(string? userId)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null)
        {
            return access;
        }

        var assignments = await changaStore.ListAssignmentsForAccountAsync(userId!);
        var coordinations = assignments
            .Select(assignment =>
            {
                var changa = assignment.Changa;
                var role = GetCoordinationRole(changa, userId!);
                return role is null ? null : ProjectCoordination(changa, role.Value) with { ExactAddress = null };
            })
            .Where(item => item is not null)
            .Cast<ChangaCoordinationView>()
            .ToArray();
        return new ChangaManagerResult(ChangaManagerStatus.Success, Coordinations: coordinations);
    }

    public async Task<ChangaManagerResult> GetRatingEligibilityAsync(string? userId, Guid id)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null) return access;

        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        var role = GetCoordinationRole(changa, userId!);
        if (role is null) return new(ChangaManagerStatus.RatingForbidden);

        if (changa.Status != ChangaStatus.Closed || changa.Assignment is null)
        {
            return new(
                ChangaManagerStatus.Success,
                RatingEligibility: new ChangaRatingEligibilityView(id, false, false));
        }

        if (string.IsNullOrWhiteSpace(GetRateeId(changa, role.Value)))
        {
            return new(
                ChangaManagerStatus.Success,
                RatingEligibility: new ChangaRatingEligibilityView(id, false, false));
        }

        var hasRated = await changaStore.HasRatingAsync(id, userId!);
        return new(
            ChangaManagerStatus.Success,
            RatingEligibility: new ChangaRatingEligibilityView(id, !hasRated, hasRated));
    }

    public async Task<ChangaManagerResult> SubmitRatingAsync(string? userId, Guid id, int? score)
    {
        var access = await CheckAccessAsync(userId);
        if (access is not null) return access;

        if (score is < 1 or > 5 or null)
        {
            return new(
                ChangaManagerStatus.Invalid,
                Errors: new Dictionary<string, string[]>
                {
                    ["score"] = ["La calificación debe ser un número entero entre 1 y 5."]
                });
        }

        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        var role = GetCoordinationRole(changa, userId!);
        if (role is null) return new(ChangaManagerStatus.RatingForbidden);
        if (changa.Status != ChangaStatus.Closed || changa.Assignment is null)
        {
            return new(ChangaManagerStatus.RatingNotAvailable);
        }

        var rateeId = GetRateeId(changa, role.Value);
        if (string.IsNullOrWhiteSpace(rateeId))
        {
            return new(ChangaManagerStatus.RatingNotAvailable);
        }

        var rating = new ChangaRating
        {
            Id = Guid.NewGuid(),
            ChangaId = changa.Id,
            RaterId = userId!,
            RateeId = rateeId,
            Score = score.Value,
            CreatedAt = UtcNow
        };
        var persisted = await changaStore.AddRatingAsync(rating);
        if (persisted == ChangaRatingPersistenceResult.AlreadyExists)
        {
            return new(ChangaManagerStatus.RatingAlreadySubmitted);
        }

        return new(
            ChangaManagerStatus.Success,
            RatingReceipt: new ChangaRatingReceiptView(changa.Id, rating.Score));
    }

    public async Task<ChangaManagerResult> GetRatingProfileAsync(string? viewerId, string? profileId)
    {
        var access = await CheckAccessAsync(viewerId);
        if (access is not null) return access;
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return new(ChangaManagerStatus.NotFound);
        }

        var profile = await accountStore.FindByIdAsync(profileId);
        if (profile is null || !profile.EmailConfirmed || !profile.DeclaredAdult)
        {
            return new(ChangaManagerStatus.NotFound);
        }

        var aggregate = await changaStore.GetRatingAggregateAsync(profileId);
        return new(
            ChangaManagerStatus.Success,
            RatingProfile: new RatingProfileView(aggregate.AverageRating, aggregate.RatingCount));
    }

    public async Task<ChangaManagerResult> GetCompletionAsync(string? userId, Guid id)
    {
        var access = await CheckCompletionAccessAsync(userId);
        if (access is not null) return access;

        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        if (changa.Assignment is null) return new(ChangaManagerStatus.NotAssigned);
        var role = GetCoordinationRole(changa, userId!);
        if (role is null) return new(ChangaManagerStatus.CompletionForbidden);

        changa = await ApplyExpirationAsync(changa);
        return new(ChangaManagerStatus.Success, Completion: ProjectCompletion(changa, role.Value));
    }

    public async Task<ChangaManagerResult> MarkDoneAsync(string? userId, Guid id)
    {
        var access = await CheckCompletionAccessAsync(userId);
        if (access is not null) return access;

        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        if (changa.Assignment is null) return new(ChangaManagerStatus.NotAssigned);
        if (GetCoordinationRole(changa, userId!) != ChangaCoordinationRole.Worker)
        {
            return new(ChangaManagerStatus.CompletionForbidden);
        }

        changa = await ApplyExpirationAsync(changa);
        if (changa.Status != ChangaStatus.Assigned)
        {
            return new(ChangaManagerStatus.CompletionInvalidState);
        }

        var now = UtcNow;
        var deadline = now.AddMinutes(ChangaPolicy.CompletionResponseMinutes);
        var transition = await changaStore.TransitionCompletionAsync(
            id, ChangaStatus.Assigned, ChangaStatus.PendingResponse,
            requestedAt: now, deadline: deadline);
        if (transition != ChangaCompletionTransitionResult.Updated)
        {
            return new(ChangaManagerStatus.CompletionInvalidState);
        }

        changa = await changaStore.FindByIdAsync(id) ?? changa;
        return new(ChangaManagerStatus.Success,
            Completion: ProjectCompletion(changa, ChangaCoordinationRole.Worker));
    }

    public Task<ChangaManagerResult> ConfirmCompletionAsync(string? userId, Guid id) =>
        RespondToCompletionAsync(userId, id, confirm: true);

    public Task<ChangaManagerResult> RejectCompletionAsync(string? userId, Guid id) =>
        RespondToCompletionAsync(userId, id, confirm: false);

    private async Task<ChangaManagerResult> RespondToCompletionAsync(string? userId, Guid id, bool confirm)
    {
        var access = await CheckCompletionAccessAsync(userId);
        if (access is not null) return access;

        var changa = await changaStore.FindByIdAsync(id);
        if (changa is null) return new(ChangaManagerStatus.NotFound);
        if (changa.Assignment is null) return new(ChangaManagerStatus.NotAssigned);
        if (GetCoordinationRole(changa, userId!) != ChangaCoordinationRole.Requester)
        {
            return new(ChangaManagerStatus.CompletionForbidden);
        }

        changa = await ApplyExpirationAsync(changa);
        if (changa.Status != ChangaStatus.PendingResponse)
        {
            return new(ChangaManagerStatus.CompletionNotPending);
        }

        var targetStatus = confirm ? ChangaStatus.Confirmed : ChangaStatus.CompletionReview;
        var transition = await changaStore.TransitionCompletionAsync(
            id,
            ChangaStatus.PendingResponse,
            targetStatus,
            respondedAt: UtcNow,
            reviewCause: confirm ? null : "rejected");
        if (transition != ChangaCompletionTransitionResult.Updated)
        {
            return new(ChangaManagerStatus.CompletionNotPending);
        }

        changa = await changaStore.FindByIdAsync(id) ?? changa;
        return new(ChangaManagerStatus.Success,
            Completion: ProjectCompletion(changa, ChangaCoordinationRole.Requester));
    }

    private async Task<Changa> ApplyExpirationAsync(Changa changa)
    {
        if (changa.Status == ChangaStatus.PendingResponse &&
            changa.Assignment?.CompletionDeadline is { } deadline && UtcNow >= deadline)
        {
            await changaStore.TransitionCompletionAsync(
                changa.Id,
                ChangaStatus.PendingResponse,
                ChangaStatus.CompletionReview,
                reviewCause: "expired");
            return await changaStore.FindByIdAsync(changa.Id) ?? changa;
        }

        return changa;
    }

    private async Task<ChangaManagerResult?> CheckCompletionAccessAsync(string? userId)
    {
        var access = await CheckAccessAsync(userId);
        if (access?.Status == ChangaManagerStatus.Forbidden)
        {
            return new ChangaManagerResult(ChangaManagerStatus.CompletionForbidden);
        }
        return access;
    }

    private DateTimeOffset UtcNow => (timeProvider ?? TimeProvider.System).GetUtcNow();

    private static ChangaCompletionView ProjectCompletion(Changa changa, ChangaCoordinationRole role) => new(
        changa.Id,
        changa.Description,
        role == ChangaCoordinationRole.Requester ? "requester" : "worker",
        changa.Status switch
        {
            ChangaStatus.Assigned => "assigned",
            ChangaStatus.PendingResponse => "pending_response",
            ChangaStatus.Confirmed => "confirmed",
            ChangaStatus.CompletionReview => "completion_review",
            _ => "assigned"
        },
        changa.Assignment?.CompletionRequestedAt,
        changa.Assignment?.CompletionDeadline,
        changa.Assignment?.CompletionReviewCause);

    private static ChangaCoordinationRole? GetCoordinationRole(Changa changa, string userId)
    {
        if (changa.AuthorId == userId)
        {
            return ChangaCoordinationRole.Requester;
        }

        return changa.Assignment?.WorkerId == userId
            ? ChangaCoordinationRole.Worker
            : null;
    }

    private static ChangaCoordinationView ProjectCoordination(Changa changa, ChangaCoordinationRole role)
    {
        var assignment = changa.Assignment!;
        var requester = role == ChangaCoordinationRole.Requester;
        var mutualConsent = assignment.RequesterPhoneConsent == PhoneConsentStatus.Accepted &&
                            assignment.WorkerPhoneConsent == PhoneConsentStatus.Accepted;
        var ownConsent = requester ? assignment.RequesterPhoneConsent : assignment.WorkerPhoneConsent;
        var otherConsent = requester ? assignment.WorkerPhoneConsent : assignment.RequesterPhoneConsent;
        var otherPhone = mutualConsent
            ? requester ? assignment.WorkerPhone : assignment.RequesterPhone
            : null;
        return new ChangaCoordinationView(
            changa.Id,
            changa.Description,
            requester ? "requester" : "worker",
            requester ? assignment.WorkerId : changa.AuthorId,
            ownConsent switch
            {
                PhoneConsentStatus.Accepted => "accepted",
                PhoneConsentStatus.Declined => "declined",
                _ => "pending"
            },
            otherConsent == PhoneConsentStatus.Accepted ? "accepted" : "not_shared",
            mutualConsent,
            otherPhone,
            requester ? null : changa.ExactAddress);
    }

    private static string? GetRateeId(Changa changa, ChangaCoordinationRole role) =>
        role == ChangaCoordinationRole.Requester
            ? changa.Assignment?.WorkerId
            : changa.AuthorId;

    private static bool IsValidPhone(string? phone)
    {
        var normalized = phone?.Trim();
        if (string.IsNullOrEmpty(normalized) || normalized.Length > ChangaPolicy.MaximumPhoneLength)
        {
            return false;
        }

        var digits = 0;
        var parentheses = 0;
        for (var index = 0; index < normalized.Length; index++)
        {
            var character = normalized[index];
            if (character is >= '0' and <= '9')
            {
                digits++;
            }
            else if (character == '+')
            {
                if (index != 0)
                {
                    return false;
                }
            }
            else if (character == '(')
            {
                parentheses++;
            }
            else if (character == ')')
            {
                if (parentheses == 0)
                {
                    return false;
                }

                parentheses--;
            }
            else if (!char.IsWhiteSpace(character) && character != '-')
            {
                return false;
            }
        }

        return digits is >= ChangaPolicy.MinimumPhoneDigits and <= ChangaPolicy.MaximumPhoneDigits && parentheses == 0;
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
        changa.CategoryId,
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
            ChangaStatus.PendingReview => "pending_review",
            ChangaStatus.Rejected => "rejected",
            ChangaStatus.PendingResponse => "pending_response",
            ChangaStatus.Confirmed => "confirmed",
            ChangaStatus.CompletionReview => "completion_review",
            ChangaStatus.Closed => "closed",
            _ => "available"
        },
        changa.ReviewReason,
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
    double? Longitude = null,
    string? CategoryId = null);

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
    string CategoryId,
    int Reward,
    string Zone,
    bool MaterialsNeeded,
    string? MaterialsProvider,
    string Status,
    string? ReviewReason,
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
    NotFound,
    SelfAssignment,
    AlreadyAssigned,
    NotAvailable,
    CoordinationForbidden,
    NotAssigned,
    InvalidPhone,
    InvalidCategory,
    InvalidReview,
    CompletionForbidden,
    CompletionNotPending,
    CompletionInvalidState,
    RatingForbidden,
    RatingNotAvailable,
    RatingAlreadySubmitted
}

public enum ChangaCoordinationRole
{
    Requester,
    Worker
}

public sealed record ChangaManagerResult(
    ChangaManagerStatus Status,
    ChangaPublicView? Changa = null,
    IReadOnlyList<ChangaPublicView>? Changas = null,
    IReadOnlyList<ChangaCategory>? Categories = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    ChangaAssignmentView? Assignment = null,
    ChangaCoordinationView? Coordination = null,
    IReadOnlyList<ChangaCoordinationView>? Coordinations = null,
    ChangaCompletionView? Completion = null,
    ChangaRatingEligibilityView? RatingEligibility = null,
    ChangaRatingReceiptView? RatingReceipt = null,
    RatingProfileView? RatingProfile = null);

public sealed record ChangaAssignmentView(Guid Id, DateTimeOffset AssignedAt);

public sealed record ChangaCompletionView(
    Guid ChangaId,
    string Description,
    string Role,
    string Status,
    DateTimeOffset? MarkedDoneAt,
    DateTimeOffset? ResponseDeadline,
    string? ReviewCause);

public sealed record ChangaCoordinationView(
    Guid ChangaId,
    string Description,
    string Role,
    string? OtherPartyId,
    string YourConsent,
    string OtherPartyConsent,
    bool HasMutualConsent,
    string? OtherPhone,
    string? ExactAddress);

public sealed record ChangaRatingEligibilityView(Guid ChangaId, bool CanRate, bool HasRated);

public sealed record ChangaRatingReceiptView(Guid ChangaId, int Score);

public sealed record RatingProfileView(double? AverageRating, int RatingCount);
