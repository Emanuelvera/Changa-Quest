using System.Security.Claims;
using System.Text.Json;
using ChangaQuest.BusinessLogic.Reports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChangaQuest.Presentation.Reports;

public static class ReportEndpointExtensions
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reports = endpoints.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        reports.MapGet("", ListAsync)
            .Produces<IReadOnlyList<ReportCaseListItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        reports.MapPost("", SubmitAsync)
            .Produces<ReportSubmittedView>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        reports.MapGet("/{id:guid}", GetAsync)
            .Produces<ReportAdminView>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        reports.MapGet("/{id:guid}/case", GetCaseAsync)
            .Produces<ReportPartyCaseView>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        reports.MapPost("/{id:guid}/responses", SubmitResponseAsync)
            .Produces<ReportResponseView>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        reports.MapPost("/{id:guid}/decision", DecideAsync)
            .Produces<ReportDecisionView>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        reports.MapPost("/{id:guid}/appeals", AppealAsync)
            .Produces<ReportAppealView>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        reports.MapPost("/{id:guid}/appeals/{appealId:guid}/resolution", ResolveAppealAsync)
            .Produces<ReportAppealView>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(
        JsonElement request,
        ClaimsPrincipal principal,
        ReportManager manager)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!TryReadGuid(request, "changaId", out var changaId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["changaId"] = ["Indicá la changa que querés reportar."]
            });
        }

        var result = await manager.SubmitAsync(
            userId,
            new ReportSubmitCommand(changaId, ReadString(request, "reason"), ReadString(request, "evidence")));

        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Created($"/api/v1/reports/{result.Submitted!.Id}", result.Submitted),
            ReportManagerStatus.Invalid => Results.ValidationProblem(result.Errors ?? new Dictionary<string, string[]>()),
            ReportManagerStatus.ChangaNotFound => Problem(
                StatusCodes.Status404NotFound, "Changa no encontrada",
                "No existe una changa con ese identificador.", "changa_not_found"),
            ReportManagerStatus.Unauthenticated => Problem(
                StatusCodes.Status401Unauthorized, "Sesión no iniciada", "Iniciá sesión para enviar un reporte."),
            _ => Problem(
                StatusCodes.Status403Forbidden, "Cuenta no habilitada",
                "Solo las cuentas adultas con correo verificado pueden enviar reportes.")
        };
    }

    private static async Task<IResult> GetAsync(
        Guid id, ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.GetAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier), id);
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Report),
            ReportManagerStatus.ReportNotFound => Problem(
                StatusCodes.Status404NotFound, "Reporte no encontrado",
                "No existe un reporte con ese identificador.", "report_not_found"),
            _ => AccessProblem(result.Status)
        };
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.ListCasesAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier));
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Cases),
            _ => AccessProblem(result.Status)
        };
    }

    private static async Task<IResult> GetCaseAsync(
        Guid id, ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.GetCaseAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier), id);
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Case),
            ReportManagerStatus.ReportNotFound => Problem(
                StatusCodes.Status404NotFound, "Caso no encontrado",
                "No existe un caso con ese identificador.", "report_not_found"),
            _ => AccessProblem(result.Status)
        };
    }

    private static async Task<IResult> SubmitResponseAsync(
        Guid id, JsonElement request, ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.SubmitResponseAsync(
            principal.FindFirstValue(ClaimTypes.NameIdentifier), id,
            new ReportTextCommand(ReadString(request, "text")));
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Created($"/api/v1/reports/{id}/responses", result.Response),
            ReportManagerStatus.Invalid => Results.ValidationProblem(result.Errors ?? new Dictionary<string, string[]>()),
            _ => MutationProblem(result.Status)
        };
    }

    private static async Task<IResult> DecideAsync(
        Guid id, JsonElement request, ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.DecideAsync(
            principal.FindFirstValue(ClaimTypes.NameIdentifier), id,
            new ReportDecisionCommand(
                ReadString(request, "conclusion"),
                ReadString(request, "reason"),
                ReadBoolean(request, "evidenceConsidered")));
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Decision),
            ReportManagerStatus.Invalid => Results.ValidationProblem(result.Errors ?? new Dictionary<string, string[]>()),
            _ => MutationProblem(result.Status)
        };
    }

    private static async Task<IResult> AppealAsync(
        Guid id, JsonElement request, ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.AppealAsync(
            principal.FindFirstValue(ClaimTypes.NameIdentifier), id,
            new ReportTextCommand(ReadString(request, "text")));
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Appeal),
            ReportManagerStatus.Invalid => Results.ValidationProblem(result.Errors ?? new Dictionary<string, string[]>()),
            _ => MutationProblem(result.Status)
        };
    }

    private static async Task<IResult> ResolveAppealAsync(
        Guid id, Guid appealId, JsonElement request, ClaimsPrincipal principal, ReportManager manager)
    {
        var result = await manager.ResolveAppealAsync(
            principal.FindFirstValue(ClaimTypes.NameIdentifier), id, appealId,
            new ReportAppealResolutionCommand(ReadString(request, "resolution")));
        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Appeal),
            ReportManagerStatus.Invalid => Results.ValidationProblem(result.Errors ?? new Dictionary<string, string[]>()),
            _ => MutationProblem(result.Status)
        };
    }

    private static IResult AccessProblem(ReportManagerStatus status) => status switch
    {
        ReportManagerStatus.Unauthenticated => Problem(
            StatusCodes.Status401Unauthorized, "Sesión no iniciada", "Iniciá sesión para continuar."),
        _ => Problem(
            StatusCodes.Status403Forbidden, "Acceso denegado",
            "No tenés autorización para consultar este caso.", "report_forbidden")
    };

    private static IResult MutationProblem(ReportManagerStatus status) => status switch
    {
        ReportManagerStatus.Unauthenticated => Problem(
            StatusCodes.Status401Unauthorized, "Sesión no iniciada", "Iniciá sesión para continuar."),
        ReportManagerStatus.AdminForbidden or ReportManagerStatus.PartyForbidden or ReportManagerStatus.Forbidden =>
            Problem(StatusCodes.Status403Forbidden, "Acceso denegado",
                "No tenés autorización para realizar esta acción.", "report_forbidden"),
        ReportManagerStatus.ReportNotFound => Problem(
            StatusCodes.Status404NotFound, "Caso no encontrado",
            "No existe un caso con ese identificador.", "report_not_found"),
        ReportManagerStatus.AppealNotFound => Problem(
            StatusCodes.Status404NotFound, "Apelación no encontrada",
            "No existe una apelación con ese identificador.", "appeal_not_found"),
        ReportManagerStatus.CaseClosed => Problem(
            StatusCodes.Status409Conflict, "Caso cerrado a respuestas",
            "No se admiten respuestas después de registrar la decisión.", "report_case_decided"),
        ReportManagerStatus.DecisionExists => Problem(
            StatusCodes.Status409Conflict, "Decisión ya registrada",
            "El caso ya tiene una decisión.", "report_decision_exists"),
        ReportManagerStatus.DecisionMissing => Problem(
            StatusCodes.Status409Conflict, "Decisión pendiente",
            "Solo se puede apelar una decisión registrada.", "report_decision_required"),
        ReportManagerStatus.AppealResolved => Problem(
            StatusCodes.Status409Conflict, "Apelación ya resuelta",
            "La apelación ya tiene una resolución.", "appeal_already_resolved"),
        _ => Problem(
            StatusCodes.Status409Conflict, "Conflicto de estado",
            "No se puede completar la acción en el estado actual del caso.", "report_state_conflict")
    };

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

    private static bool? ReadBoolean(JsonElement request, string property) =>
        request.ValueKind == JsonValueKind.Object &&
        request.TryGetProperty(property, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static bool TryReadGuid(JsonElement request, string property, out Guid id)
    {
        id = Guid.Empty;
        return ReadString(request, property) is { } text && Guid.TryParse(text, out id) && id != Guid.Empty;
    }
}
