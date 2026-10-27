using ChangaQuest.DataAccess.Accounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.BusinessLogic.Accounts;

public static class AccountBusinessLogicServiceCollectionExtensions
{
    public static IServiceCollection AddAccountBusinessLogic(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAccountDataAccess(configuration);
        services.AddScoped<AccountManager>();
        services.AddScoped<AccountBlockManager>();
        return services;
    }
}
