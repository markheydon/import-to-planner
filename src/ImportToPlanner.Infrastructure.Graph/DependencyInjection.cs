using ImportToPlanner.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Infrastructure.Graph;

/// <summary>
/// Extension methods for registering infrastructure-layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services required for CSV import and planner gateway access.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="useGraphGateway">
    /// A value indicating whether to use the Microsoft Graph planner gateway implementation.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, bool useGraphGateway)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICsvImportParser, CsvImportParser>();

        if (useGraphGateway)
        {
            services.AddScoped<IPlannerGateway, GraphPlannerGateway>();
        }
        else
        {
            services.AddScoped<IPlannerGateway, InMemoryPlannerGateway>();
        }

        return services;
    }
}
