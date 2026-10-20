using ChangaQuest.Entities.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.DataAccess.Accounts;

public static class AccountDataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddAccountDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChangaQuest")
            ?? throw new InvalidOperationException("Falta la conexión de base de datos ChangaQuest.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = AccountPolicy.MinimumPasswordLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            })
            .AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "ChangaQuest.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context =>
                WriteAuthenticationProblem(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Sesión no iniciada",
                    "Iniciá sesión para acceder a este recurso.");
            options.Events.OnRedirectToAccessDenied = context =>
                WriteAuthenticationProblem(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "Acceso denegado",
                    "Tu cuenta no tiene permiso para acceder a este recurso.");
        });
        services.AddAuthorization();
        services.AddScoped<IAccountIdentityStore, IdentityAccountStore>();
        services.AddHostedService<AccountDatabaseMigrationService>();

        return services;
    }

    private static Task WriteAuthenticationProblem(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = "about:blank",
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            },
            options: null,
            contentType: "application/problem+json");
    }
}
