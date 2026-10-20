using ChangaQuest.BusinessLogic.Accounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.Presentation.Accounts;

public static class AccountServiceCollectionExtensions
{
    public static IServiceCollection AddAccountServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddAccountBusinessLogic(configuration);
    }
}
