using ChangaQuest.BusinessLogic.Changas;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.Presentation.Changas;

public static class ChangaServiceCollectionExtensions
{
    public static IServiceCollection AddChangaServices(this IServiceCollection services)
    {
        services.AddChangaBusinessLogic();
        return services;
    }
}
