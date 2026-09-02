using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Accounts.Storage;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Credits.Storage;
using ImportToPlanner.Commercial.Services;
using ImportToPlanner.Commercial.TenantMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Commercial;

/// <summary>
/// Extension methods for registering commercial account dependencies.
/// </summary>
public static class DependencyInjection
{
    public const string CommercialAccountsTableClientKey = "CommercialAccountsTable";

    public const string CommercialAuditTableClientKey = "CommercialAuditTable";

    public const string CommercialCreditLedgerTableClientKey = "CommercialCreditLedgerTable";

    /// <summary>
    /// Adds Aspire-managed Azure Table Storage client registrations required by commercial adapters.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <see cref="IHostApplicationBuilder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddCommercialStorageClients(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddAzureTableServiceClient(connectionName: "tables");

        return builder;
    }

    /// <summary>
    /// Adds commercial account stores, use cases, and table-backed tenant metadata.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCommercial(this IServiceCollection services, IConfiguration configuration)
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

        var commercialCreditLedgerTableName = configuration["Storage:CommercialCreditLedgerTable"];
        if (string.IsNullOrWhiteSpace(commercialCreditLedgerTableName))
        {
            throw new InvalidOperationException("Storage configuration is invalid. Set 'Storage:CommercialCreditLedgerTable'.");
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
        services.AddKeyedSingleton<TableClient>(
            CommercialCreditLedgerTableClientKey,
            (serviceProvider, _) => serviceProvider
                .GetRequiredService<TableServiceClient>()
                .GetTableClient(commercialCreditLedgerTableName));
        services.AddScoped<ICommercialAccountStore>(serviceProvider =>
            new TableCommercialAccountStore(
                serviceProvider.GetRequiredKeyedService<TableClient>(CommercialAccountsTableClientKey)));
        services.AddScoped<ICommercialAuditStore>(serviceProvider =>
            new TableCommercialAuditStore(
                serviceProvider.GetRequiredKeyedService<TableClient>(CommercialAuditTableClientKey)));
        services.AddSingleton<IUtcClock, SystemUtcClock>();
        services.AddScoped<ImportExecutionCreditBalanceCache>();
        services.AddScoped<ICreditLedgerStore>(serviceProvider =>
            new TableCreditLedgerStore(
                serviceProvider.GetRequiredKeyedService<TableClient>(CommercialCreditLedgerTableClientKey)));
        services.AddScoped<IEnsureCurrentCreditBalanceUseCase, EnsureCurrentCreditBalanceUseCase>();

        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IImportTaskCreationQuota)).ToList())
        {
            services.Remove(descriptor);
        }

        services.AddScoped<IImportTaskCreationQuota, ImportTaskCreationCreditQuota>();
        services.AddSingleton<ITenantOperationalMetadataStore>(serviceProvider =>
            new TableTenantOperationalMetadataStore(
                serviceProvider.GetRequiredService<TableServiceClient>(),
                tenantMetadataTableName));

        services.AddScoped<ICommercialAccessUseCase, CommercialAccessUseCase>();
        services.AddScoped<GetCommercialProfileUseCase>();
        services.AddScoped<DeleteCommercialAccountUseCase>();
        services.AddScoped<RestoreCommercialAccountUseCase>();
        services.AddScoped<PurgeExpiredCommercialAccountsUseCase>();
        services.AddScoped<ICommercialProfileUseCase, GetCommercialProfileUseCase>();

        return services;
    }
}
