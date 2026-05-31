using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Infrastructure.Graph.Import;
using ImportToPlanner.Infrastructure.Graph.Planner;
using ImportToPlanner.Infrastructure.Graph.TenantMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Infrastructure.Graph;

/// <summary>
/// Extension methods for registering infrastructure-layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Aspire-managed Azure Table Storage client registrations required by infrastructure adapters.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <see cref="IHostApplicationBuilder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddInfrastructureStorageClients(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (ShouldUseTableTenantMetadataStorage(builder.Configuration))
        {
            builder.AddAzureTableServiceClient(connectionName: "tables");
        }

        return builder;
    }

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

        services.AddScoped<ICsvImportParser, CsvImportParser>();

        if (ShouldUseTableTenantMetadataStorage(configuration))
        {
            var tenantMetadataTableName = configuration["Storage:TenantMetadataTable"];
            if (string.IsNullOrWhiteSpace(tenantMetadataTableName))
            {
                throw new InvalidOperationException("Storage configuration is invalid. Set 'Storage:TenantMetadataTable'.");
            }

            services.AddSingleton<ITenantOperationalMetadataStore>(serviceProvider =>
                new TableTenantOperationalMetadataStore(
                    serviceProvider.GetRequiredService<TableServiceClient>(),
                    tenantMetadataTableName));
        }
        else
        {
            services.AddSingleton<ITenantOperationalMetadataStore, SelfHostTenantOperationalMetadataStore>();
        }

        services.AddScoped<IPlannerGateway, GraphPlannerGateway>();

        return services;
    }

    private static bool ShouldUseTableTenantMetadataStorage(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var commercialModeEnabled = configuration.GetValue<bool>("Features:CommercialMode:Enabled");
        if (!commercialModeEnabled)
        {
            return false;
        }

        var useBackendApi = configuration.GetValue<bool>("Features:CommercialMode:UseBackendApi");
        return !useBackendApi;
    }
}
