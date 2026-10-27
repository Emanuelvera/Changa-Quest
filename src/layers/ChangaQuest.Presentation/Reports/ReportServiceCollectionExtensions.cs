using ChangaQuest.BusinessLogic.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.Presentation.Reports;

public static class ReportServiceCollectionExtensions
{
    public static IServiceCollection AddReportServices(this IServiceCollection services)
    {
        services.AddReportBusinessLogic();
        return services;
    }
}
