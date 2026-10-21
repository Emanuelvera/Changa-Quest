using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ChangaQuest.BusinessLogic.Accounts;
using ChangaQuest.Entities.Accounts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ChangaQuest.Presentation.Accounts;

public static class AccountEndpointExtensions
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var accounts = endpoints.MapGroup("/api/v1/accounts").WithTags("Accounts");

        accounts.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        accounts.MapGet("/confirm-email", ConfirmEmailAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        accounts.MapPost("/login", SignInAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        accounts.MapPost("/logout", SignOutAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        accounts.MapGet("/me", GetCurrentAccountAsync)
            .RequireAuthorization()
            .Produces<CurrentAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterAccountRequest request,
        HttpContext context,
        AccountManager manager,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        var result = await manager.RegisterAsync(request.Email, request.Password, request.DeclaredAdult);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                result.Errors ?? new Dictionary<string, string[]>(),
                title: "No se pudo crear la cuenta.");
        }

        if (environment.IsDevelopment() && result.UserId is not null && result.ConfirmationToken is not null)
        {
            var confirmationUrl = $"{context.Request.Scheme}://{context.Request.Host}" +
                $"/api/v1/accounts/confirm-email?userId={Uri.EscapeDataString(result.UserId)}" +
                $"&token={Uri.EscapeDataString(result.ConfirmationToken)}";
            loggerFactory.CreateLogger("ChangaQuest.AccountVerification")
                .LogInformation("Enlace local de verificación: {VerificationUrl}", confirmationUrl);
        }

        return Results.Accepted(
            "/api/v1/accounts/login",
            new { message = "Cuenta creada. Verificá tu correo para poder iniciar sesión." });
    }

    private static async Task<IResult> ConfirmEmailAsync(
        string? userId,
        string? token,
        AccountManager manager)
    {
        var confirmed = !string.IsNullOrWhiteSpace(userId) &&
            !string.IsNullOrWhiteSpace(token) &&
            await manager.ConfirmEmailAsync(userId, token);

        var message = confirmed
            ? "Correo verificado. Ya podés iniciar sesión."
            : "El enlace de verificación no es válido o ya fue utilizado.";
        var heading = confirmed ? "Correo verificado" : "No se pudo verificar el correo";
        var html = $"""
            <!doctype html>
            <html lang="es-AR">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{heading} · Changa Quest</title></head>
            <body><main><h1>{heading}</h1><p>{message}</p><a href="/">Volver a Changa Quest</a></main></body>
            </html>
            """;

        return confirmed
            ? Results.Content(html, "text/html; charset=utf-8")
            : Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: heading,
                detail: message,
                extensions: new Dictionary<string, object?> { ["code"] = "email_confirmation_invalid" });
    }

    private static async Task<IResult> SignInAsync(
        SignInRequest request,
        AccountManager manager)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > AccountPolicy.MaximumEmailLength ||
            !new EmailAddressAttribute().IsValid(email))
        {
            errors["email"] = ["Ingresá un correo electrónico válido."];
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors["password"] = ["Ingresá tu contraseña."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(
                errors,
                title: "No se pudo iniciar sesión.");
        }

        var status = await manager.SignInAsync(email!, request.Password!);
        return status switch
        {
            AccountLoginStatus.Succeeded => Results.Ok(new { message = "Sesión iniciada." }),
            AccountLoginStatus.EmailNotVerified => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Verificá tu correo",
                detail: "Confirmá el correo electrónico antes de iniciar sesión.",
                extensions: new Dictionary<string, object?> { ["code"] = "email_not_verified" }),
            AccountLoginStatus.NotEligible => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Cuenta no habilitada",
                detail: "La cuenta no cumple los requisitos para acceder.",
                extensions: new Dictionary<string, object?> { ["code"] = "account_not_eligible" }),
            _ => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No se pudo iniciar sesión",
                detail: "Revisá el correo y la contraseña.")
        };
    }

    private static async Task<IResult> SignOutAsync(AccountManager manager)
    {
        await manager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentAccountAsync(
        ClaimsPrincipal principal,
        AccountManager manager)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sesión no iniciada",
                detail: "Iniciá sesión para consultar tu cuenta.");
        }

        var user = await manager.FindByIdAsync(userId);
        if (user is null || !user.EmailConfirmed || !user.DeclaredAdult)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Cuenta no habilitada",
                detail: "Iniciá sesión con una cuenta verificada y elegible.");
        }

        return Results.Ok(new CurrentAccountResponse(user.Email ?? string.Empty));
    }
}

public sealed record RegisterAccountRequest(string? Email, string? Password, bool DeclaredAdult);
public sealed record SignInRequest(string? Email, string? Password);
public sealed record CurrentAccountResponse(string Email);
