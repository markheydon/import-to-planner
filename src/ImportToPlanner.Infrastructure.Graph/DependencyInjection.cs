using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Infrastructure.Graph.Import;
using ImportToPlanner.Infrastructure.Graph.Planner;
using ImportToPlanner.Infrastructure.Graph.TenantMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Infrastructure.Graph;

/// <summary>
/// Extension methods for registering infrastructure-layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Graph-backed infrastructure services required for CSV import and planner gateway access.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMicrosoftGraphInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<ICsvImportParser, CsvImportParser>();
        services.AddSingleton<ITenantOperationalMetadataStore, SelfHostTenantOperationalMetadataStore>();

        services.AddScoped<IPlannerGateway, GraphPlannerGateway>();

        return services;
    }
}
