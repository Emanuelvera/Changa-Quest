using Microsoft.Extensions.DependencyInjection;

namespace ChangaQuest.DataAccess.Reports;

public static class ReportDataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddReportDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IReportStore, ReportStore>();
        return services;
    }
}
