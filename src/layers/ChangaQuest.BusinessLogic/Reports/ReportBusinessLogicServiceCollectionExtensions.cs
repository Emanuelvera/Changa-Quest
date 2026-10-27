using Microsoft.Extensions.DependencyInjection;
using ChangaQuest.DataAccess.Reports;

namespace ChangaQuest.BusinessLogic.Reports;

public static class ReportBusinessLogicServiceCollectionExtensions
{
    public static IServiceCollection AddReportBusinessLogic(this IServiceCollection services)
    {
        services.AddReportDataAccess();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ReportManager>();
        return services;
    }
}
