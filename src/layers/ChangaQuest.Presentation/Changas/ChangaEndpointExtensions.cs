using System.Security.Claims;
using System.Text.Json;
using ChangaQuest.BusinessLogic.Changas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChangaQuest.Presentation.Changas;

public static class ChangaEndpointExtensions
{
    public static IEndpointRouteBuilder MapChangaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var changas = endpoints.MapGroup("/api/v1/changas")
            .WithTags("Changas")
            .RequireAuthorization();

        changas.MapPost("", CreateAsync)
            .Produces<ChangaPublicView>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        changas.MapGet("", ListAsync)
            .Produces<IReadOnlyList<ChangaPublicView>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        changas.MapGet("/{id:guid}", FindAsync)
            .Produces<ChangaPublicView>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        changas.MapPost("/{id:guid}/take", TakeAsync)
            .Produces<ChangaTakeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        JsonElement request,
        ClaimsPrincipal principal,
        ChangaManager manager)
    {
        var command = new ChangaCreateCommand(
            ReadString(request, "description"),
            ReadInt32(request, "reward"),
            ReadString(request, "zone"),
            ReadString(request, "exactAddress"),
            ReadBoolean(request, "materialsNeeded"),
            ReadString(request, "materialsProvider"),
            request.ValueKind == JsonValueKind.Object && request.TryGetProperty("materialsProvider", out _),
            ReadDouble(request, "latitude"),
            ReadDouble(request, "longitude"));
        var result = await manager.CreateAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier), command);

        return result.Status switch
        {
            ChangaManagerStatus.Success => Results.Created(
                $"/api/v1/changas/{result.Changa!.Id}",
                result.Changa),
            ChangaManagerStatus.Invalid => Results.ValidationProblem(
                result.Errors ?? new Dictionary<string, string[]>(),
                title: "No se pudo publicar la changa."),
            ChangaManagerStatus.Unauthenticated => Problem(
                StatusCodes.Status401Unauthorized,
                "Sesión no iniciada",
                "Iniciá sesión para publicar una changa."),
            ChangaManagerStatus.Forbidden => Problem(
                StatusCodes.Status403Forbidden,
                "Cuenta no habilitada",
                "Solo las cuentas adultas con correo verificado pueden publicar."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Error interno", "No se pudo completar la operación.")
        };
    }

    private static async Task<IResult> ListAsync(
        double? latitude,
        double? longitude,
        int? radiusKm,
        bool? savePreference,
        ClaimsPrincipal principal,
        ChangaManager manager)
    {
        var result = await manager.SearchAvailableAsync(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            new ChangaSearchQuery(latitude, longitude, radiusKm, savePreference ?? false));
        return result.Status switch
        {
            ChangaManagerStatus.Success => Results.Ok(result.Changas),
            ChangaManagerStatus.Invalid => Results.ValidationProblem(
                result.Errors ?? new Dictionary<string, string[]>(),
                title: "No se pudo buscar changas."),
            _ => AccessProblem(result.Status)
        };
    }

    private static async Task<IResult> FindAsync(
        Guid id,
        ClaimsPrincipal principal,
        ChangaManager manager)
    {
        var result = await manager.FindAvailableAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier), id);
        return result.Status switch
        {
            ChangaManagerStatus.Success => Results.Ok(result.Changa),
            ChangaManagerStatus.NotFound => Problem(
                StatusCodes.Status404NotFound,
                "Changa no encontrada",
                "No existe una changa disponible con ese identificador."),
            _ => AccessProblem(result.Status)
        };
    }

    private static IResult AccessProblem(ChangaManagerStatus status) => status switch
    {
        ChangaManagerStatus.Unauthenticated => Problem(
            StatusCodes.Status401Unauthorized,
            "Sesión no iniciada",
            "Iniciá sesión para consultar changas."),
        _ => Problem(
            StatusCodes.Status403Forbidden,
            "Cuenta no habilitada",
            "Solo las cuentas adultas con correo verificado pueden consultar changas.")
    };

    private static async Task<IResult> TakeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ChangaManager manager)
    {
        var result = await manager.TakeAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier), id);
        return result.Status switch
        {
            ChangaManagerStatus.Success => Results.Ok(new ChangaTakeResponse(result.Changa!, result.Assignment!)),
            ChangaManagerStatus.NotFound => Problem(
                StatusCodes.Status404NotFound,
                "Changa no encontrada",
                "No existe una changa con ese identificador.",
                "changa_not_found"),
            ChangaManagerStatus.SelfAssignment => Problem(
                StatusCodes.Status409Conflict,
                "No podés tomar tu propia changa",
                "La persona que publica no puede tomar su propia changa.",
                "changa_author_cannot_take"),
            ChangaManagerStatus.AlreadyAssigned => Problem(
                StatusCodes.Status409Conflict,
                "La changa ya fue asignada",
                "Otra persona ya tomó esta changa.",
                "changa_already_assigned"),
            ChangaManagerStatus.NotAvailable => Problem(
                StatusCodes.Status409Conflict,
                "La changa ya no está disponible",
                "La changa cambió de estado y ya no se puede tomar.",
                "changa_not_available"),
            _ => AccessProblem(result.Status)
        };
    }

    private static IResult Problem(int statusCode, string title, string detail, string? code = null) =>
        Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            extensions: code is null ? null : new Dictionary<string, object?> { ["code"] = code });

    private static string? ReadString(JsonElement request, string property) =>
        request.ValueKind == JsonValueKind.Object &&
        request.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt32(JsonElement request, string property) =>
        request.ValueKind == JsonValueKind.Object &&
        request.TryGetProperty(property, out var value) &&
        value.TryGetInt32(out var result)
            ? result
            : null;

    private static bool? ReadBoolean(JsonElement request, string property) =>
        request.ValueKind == JsonValueKind.Object &&
        request.TryGetProperty(property, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static double? ReadDouble(JsonElement request, string property) =>
        request.ValueKind == JsonValueKind.Object &&
        request.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetDouble(out var result)
            ? result
            : null;
}

public sealed record ChangaTakeResponse(
    ChangaPublicView Changa,
    ChangaAssignmentView Assignment);
