using ImportToPlanner.Application.Abstractions;
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
    /// Adds infrastructure services required for CSV import and planner gateway access.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var useGraphGateway = bool.TryParse(configuration["PlannerGateway:UseGraph"], out var graphMode) && graphMode;
        var hostedStorageEnabled = bool.TryParse(configuration["HostedStorage:Enabled"], out var hostedStorageMode) && hostedStorageMode;

        services.AddScoped<ICsvImportParser, CsvImportParser>();
        services.AddSingleton<ITenantOperationalMetadataStore>(_ =>
        {
            var hostedStorageConnectionString = ResolveHostedStorageConnectionString(configuration);
            if (hostedStorageEnabled && !string.IsNullOrWhiteSpace(hostedStorageConnectionString))
            {
                var tableName = configuration["HostedStorage:TenantMetadataTable"] ?? "TenantOperationalMetadata";
                return new TableTenantOperationalMetadataStore(hostedStorageConnectionString, tableName);
            }

            return new InMemoryTenantOperationalMetadataStore();
        });

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

    private static string? ResolveHostedStorageConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration["HostedStorage:ConnectionString"]
            ?? configuration.GetConnectionString("hostedstorageblobs")
            ?? configuration.GetConnectionString("hostedstoragetables")
            ?? configuration.GetConnectionString("hostedstorage");
    }
}
