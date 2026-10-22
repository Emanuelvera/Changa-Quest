using ChangaQuest.BusinessLogic.Changas;
using ChangaQuest.DataAccess.Changas;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.BusinessLogic.Changas;

public static class ChangaBusinessLogicServiceCollectionExtensions
{
    public static IServiceCollection AddChangaBusinessLogic(this IServiceCollection services)
    {
        services.AddChangaDataAccess();
        services.AddScoped<ChangaManager>();
        return services;
    }
}

