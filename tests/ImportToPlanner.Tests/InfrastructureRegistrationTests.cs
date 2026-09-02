using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Infrastructure.Graph.Planner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Tests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void AddCommercialStorageClients_RegistersTableServiceClient()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:tables"] = "UseDevelopmentStorage=true",
        });

        builder.AddCommercialStorageClients();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();
        Assert.NotNull(tableServiceClient);
    }

    [Fact]
    public void AddCommercial_RegistersCommercialStoresAndUseCases()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
                ["Storage:CommercialCreditLedgerTable"] = "CommercialCreditLedger",
            })
            .Build();

        services.AddCommercial(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
        var commercialAccountsTableClient = serviceProvider.GetRequiredKeyedService<TableClient>(ImportToPlanner.Commercial.DependencyInjection.CommercialAccountsTableClientKey);
        var commercialAuditTableClient = serviceProvider.GetRequiredKeyedService<TableClient>(ImportToPlanner.Commercial.DependencyInjection.CommercialAuditTableClientKey);
        var commercialCreditLedgerTableClient = serviceProvider.GetRequiredKeyedService<TableClient>(ImportToPlanner.Commercial.DependencyInjection.CommercialCreditLedgerTableClientKey);
        var accessUseCase = serviceProvider.GetRequiredService<ICommercialAccessUseCase>();
        var profileUseCase = serviceProvider.GetRequiredService<ICommercialProfileUseCase>();

        Assert.Equal("TableTenantOperationalMetadataStore", metadataStore.GetType().Name);
        Assert.Equal("CommercialAccounts", commercialAccountsTableClient.Name);
        Assert.Equal("CommercialAccountAuditEvents", commercialAuditTableClient.Name);
        Assert.Equal("CommercialCreditLedger", commercialCreditLedgerTableClient.Name);
        Assert.Equal("CommercialAccessUseCase", accessUseCase.GetType().Name);
        Assert.Equal("GetCommercialProfileUseCase", profileUseCase.GetType().Name);
    }

    [Fact]
    public void AddInfrastructure_WhenCommercialModeEnabled_DoesNotRegisterSelfHostMetadataStore()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        services.AddInfrastructure(configuration);

        var metadataDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(ITenantOperationalMetadataStore))
            .ToList();

        Assert.Empty(metadataDescriptors);
    }

    [Fact]
    public void AddInfrastructureAndCommercial_WhenCommercialModeEnabled_RegistersTableMetadataStore()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
                ["Storage:CommercialCreditLedgerTable"] = "CommercialCreditLedger",
            })
            .Build();

        services.AddInfrastructure(configuration);
        services.AddCommercial(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();

        Assert.Equal("TableTenantOperationalMetadataStore", metadataStore.GetType().Name);
    }

    [Fact]
    public void AddInfrastructure_RegistersGraphGatewayAndSelfHostMetadataStoreWithoutTables()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
            })
            .Build();

        services.AddInfrastructure(configuration);

        var plannerDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(IPlannerGateway));
        Assert.Equal(ServiceLifetime.Scoped, plannerDescriptor.Lifetime);
        Assert.Equal(typeof(GraphPlannerGateway), plannerDescriptor.ImplementationType);

        using var serviceProvider = services.BuildServiceProvider();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();
        var commercialAccountStore = serviceProvider.GetService<ICommercialAccountStore>();
        var creditLedgerTableClient = serviceProvider.GetService<TableClient>();

        Assert.Equal("SelfHostTenantOperationalMetadataStore", metadataStore.GetType().Name);
        Assert.Null(tableServiceClient);
        Assert.Null(commercialAccountStore);
        Assert.Null(creditLedgerTableClient);
    }

    [Fact]
    public void AddCommercial_RegistersCreditLedgerTableClientOnlyWhenCommercialConfigured()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
                ["Storage:CommercialCreditLedgerTable"] = "CommercialCreditLedger",
            })
            .Build();

        services.AddCommercial(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var creditLedgerTableClient = serviceProvider.GetRequiredKeyedService<TableClient>(
            ImportToPlanner.Commercial.DependencyInjection.CommercialCreditLedgerTableClientKey);

        Assert.Equal("CommercialCreditLedger", creditLedgerTableClient.Name);
    }
}
