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
            ReportManagerStatus.Invalid => Results.ValidationProblem(
                result.Errors ?? new Dictionary<string, string[]>()),
            ReportManagerStatus.ChangaNotFound => Problem(
                StatusCodes.Status404NotFound,
                "Changa no encontrada",
                "No existe una changa con ese identificador.",
                "changa_not_found"),
            ReportManagerStatus.Unauthenticated => Problem(
                StatusCodes.Status401Unauthorized,
                "Sesión no iniciada",
                "Iniciá sesión para enviar un reporte."),
            _ => Problem(
                StatusCodes.Status403Forbidden,
                "Cuenta no habilitada",
                "Solo las cuentas adultas con correo verificado pueden enviar reportes.")
        };
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        ReportManager manager)
    {
        var result = await manager.GetAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier), id);

        return result.Status switch
        {
            ReportManagerStatus.Success => Results.Ok(result.Report),
            ReportManagerStatus.ReportNotFound => Problem(
                StatusCodes.Status404NotFound,
                "Reporte no encontrado",
                "No existe un reporte con ese identificador.",
                "report_not_found"),
            ReportManagerStatus.Unauthenticated => Problem(
                StatusCodes.Status401Unauthorized,
                "Sesión no iniciada",
                "Iniciá sesión para continuar."),
            _ => Problem(
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                "Solo la revisión administrativa puede consultar reportes.",
                "report_forbidden")
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

    private static bool TryReadGuid(JsonElement request, string property, out Guid id)
    {
        id = Guid.Empty;
        return ReadString(request, property) is { } text && Guid.TryParse(text, out id) && id != Guid.Empty;
    }
}
