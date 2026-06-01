using Azure.Data.Tables;
using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.CommercialService.CommercialAccounts.Abstractions;
using ImportToPlanner.CommercialService.CommercialAccounts.Services;
using ImportToPlanner.CommercialService.CommercialAccounts.Storage;
using ImportToPlanner.CommercialService.TenantMetadata.Storage;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.CommercialService;

/// <summary>
/// Extension methods for registering and mapping the hosted commercial API service.
/// </summary>
public static class DependencyInjection
{
    private const string TenantOperationalMetadataTableClientKey = "TenantOperationalMetadataTable";
    private const string CommercialAccountsTableClientKey = "CommercialAccountsTable";
    private const string CommercialAuditTableClientKey = "CommercialAuditTable";

    /// <summary>
    /// Adds hosted commercial API dependencies.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCommercialApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CommercialModeOptions>()
            .Bind(configuration.GetSection(CommercialModeOptions.ConfigurationSectionName))
            .ValidateOnStart();
        services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);
        services.AddHostedService<CommercialAccountRetentionHostedService>();

        services.AddCommercialBackendUseCases();
        services.AddCommercialStorage(configuration);

        return services;
    }

    private static IServiceCollection AddCommercialBackendUseCases(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICommercialAccessUseCase, CommercialAccessUseCase>();
        services.AddScoped<DeleteCommercialAccountUseCase>();
        services.AddScoped<RestoreCommercialAccountUseCase>();
        services.AddScoped<PurgeExpiredCommercialAccountsUseCase>();
        services.AddScoped<ICommercialProfileUseCase, GetCommercialProfileUseCase>();

        return services;
    }

    private static IServiceCollection AddCommercialStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var tenantMetadataTableName = configuration["Storage:TenantMetadataTable"];
        if (string.IsNullOrWhiteSpace(tenantMetadataTableName))
        {
            throw new InvalidOperationException("Storage configuration is invalid. Set 'Storage:TenantMetadataTable'.");
        }

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
            TenantOperationalMetadataTableClientKey,
            (serviceProvider, _) => serviceProvider
                .GetRequiredService<TableServiceClient>()
                .GetTableClient(tenantMetadataTableName));
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
        services.AddScoped<ITenantOperationalMetadataStore>(serviceProvider =>
            new TableTenantOperationalMetadataStore(
                serviceProvider.GetRequiredKeyedService<TableClient>(TenantOperationalMetadataTableClientKey)));

        return services;
    }

}
