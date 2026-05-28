using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Infrastructure.Graph.CommercialAccounts;
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
    public const string CommercialAccountsTableClientKey = "CommercialAccountsTable";

    public const string CommercialAuditTableClientKey = "CommercialAuditTable";

    /// <summary>
    /// Adds Aspire-managed Azure Table Storage client registrations required by infrastructure adapters.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <see cref="IHostApplicationBuilder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddInfrastructureStorageClients(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddAzureTableServiceClient(connectionName: "tables");

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

        var tenantMetadataTableName = configuration["Storage:TenantMetadataTable"];
        if (string.IsNullOrWhiteSpace(tenantMetadataTableName))
        {
            throw new InvalidOperationException("Storage configuration is invalid. Set 'Storage:TenantMetadataTable'.");
        }

        services.AddScoped<ICsvImportParser, CsvImportParser>();

        var commercialModeEnabled = configuration.GetValue<bool>("Features:CommercialMode:Enabled");
        if (commercialModeEnabled)
        {
            var commercialAccountsTableName = configuration["Storage:CommercialAccountsTable"];
            if (string.IsNullOrWhiteSpace(commercialAccountsTableName))
            {
                throw new InvalidOperationException("Storage configuration is invalid. Set 'Storage:CommercialAccountsTable'.");
            }

            var commercialAuditTableName = configuration["Storage:CommercialAuditTable"];
            if (string.IsNullOrWhiteSpace(commercialAuditTableName))
            {
                throw new InvalidOperationException("Storage configuration is invalid. Set 'Storage:CommercialAuditTable'.");
            }

            services.AddKeyedSingleton<TableClient>(
                CommercialAccountsTableClientKey,
                (serviceProvider, _) => serviceProvider
                    .GetRequiredService<TableServiceClient>()
                    .GetTableClient(commercialAccountsTableName));
            services.AddKeyedSingleton<TableClient>(
                CommercialAuditTableClientKey,
                (serviceProvider, _) => serviceProvider
                    .GetRequiredService<TableServiceClient>()
                    .GetTableClient(commercialAuditTableName));
            services.AddScoped<ICommercialAccountStore>(serviceProvider =>
                new TableCommercialAccountStore(
                    serviceProvider.GetRequiredKeyedService<TableClient>(CommercialAccountsTableClientKey)));
            services.AddScoped<ICommercialAuditStore>(serviceProvider =>
                new TableCommercialAuditStore(
                    serviceProvider.GetRequiredKeyedService<TableClient>(CommercialAuditTableClientKey)));
        }
        else
        {
            services.AddScoped<ICommercialAccountStore, NoOpCommercialAccountStore>();
            services.AddScoped<ICommercialAuditStore, NoOpCommercialAuditStore>();
        }

        services.AddSingleton<ITenantOperationalMetadataStore>(serviceProvider =>
            new TableTenantOperationalMetadataStore(
                serviceProvider.GetRequiredService<TableServiceClient>(),
                tenantMetadataTableName));
        services.AddScoped<IPlannerGateway, GraphPlannerGateway>();

        return services;
    }
}
