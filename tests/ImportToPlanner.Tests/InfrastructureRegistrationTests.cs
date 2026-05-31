using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Infrastructure.Graph.Planner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Tests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructureStorageClients_WhenCommercialModeDisabled_DoesNotRegisterTableServiceClient()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Features:CommercialMode:Enabled"] = "false",
        });

        builder.AddInfrastructureStorageClients();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();
        Assert.Null(tableServiceClient);
    }

    [Fact]
    public void AddInfrastructureStorageClients_WhenCommercialModeEnabled_RegistersTableServiceClient()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Features:CommercialMode:Enabled"] = "true",
            ["ConnectionStrings:tables"] = "UseDevelopmentStorage=true",
        });

        builder.AddInfrastructureStorageClients();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();
        Assert.NotNull(tableServiceClient);
    }

    [Fact]
    public void AddInfrastructureStorageClients_WhenCommercialModeEnabledWithBackendApi_DoesNotRegisterTableServiceClient()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Features:CommercialMode:Enabled"] = "true",
            ["Features:CommercialMode:UseBackendApi"] = "true",
            ["ConnectionStrings:tables"] = "UseDevelopmentStorage=true",
        });

        builder.AddInfrastructureStorageClients();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();
        Assert.Null(tableServiceClient);
    }

    [Fact]
    public void AddInfrastructure_WhenCommercialModeEnabled_RegistersGraphGatewayAndTableMetadataStore()
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
            })
            .Build();

        services.AddInfrastructure(configuration);

        var plannerDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(IPlannerGateway));
        Assert.Equal(ServiceLifetime.Scoped, plannerDescriptor.Lifetime);
        Assert.Equal(typeof(GraphPlannerGateway), plannerDescriptor.ImplementationType);

        using var serviceProvider = services.BuildServiceProvider();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
        var commercialAccountsTableClient = serviceProvider.GetRequiredKeyedService<TableClient>(DependencyInjection.CommercialAccountsTableClientKey);
        var commercialAuditTableClient = serviceProvider.GetRequiredKeyedService<TableClient>(DependencyInjection.CommercialAuditTableClientKey);

        Assert.Equal("TableTenantOperationalMetadataStore", metadataStore.GetType().Name);
        Assert.Equal("CommercialAccounts", commercialAccountsTableClient.Name);
        Assert.Equal("CommercialAccountAuditEvents", commercialAuditTableClient.Name);
    }

    [Fact]
    public void AddInfrastructure_WhenCommercialModeDisabled_RegistersNoOpCommercialStoresAndSelfHostMetadataStoreWithoutTables()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accountStore = serviceProvider.GetRequiredService<ICommercialAccountStore>();
        var auditStore = serviceProvider.GetRequiredService<ICommercialAuditStore>();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();

        Assert.Equal("NoOpCommercialAccountStore", accountStore.GetType().Name);
        Assert.Equal("NoOpCommercialAuditStore", auditStore.GetType().Name);
        Assert.Equal("SelfHostTenantOperationalMetadataStore", metadataStore.GetType().Name);
        Assert.Null(tableServiceClient);
    }

    [Fact]
    public void AddInfrastructure_WhenCommercialModeEnabledWithBackendApi_RegistersNoOpCommercialStoresAndSelfHostMetadataStoreWithoutTables()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Features:CommercialMode:UseBackendApi"] = "true",
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accountStore = serviceProvider.GetRequiredService<ICommercialAccountStore>();
        var auditStore = serviceProvider.GetRequiredService<ICommercialAuditStore>();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
        var tableServiceClient = serviceProvider.GetService<TableServiceClient>();

        Assert.Equal("NoOpCommercialAccountStore", accountStore.GetType().Name);
        Assert.Equal("NoOpCommercialAuditStore", auditStore.GetType().Name);
        Assert.Equal("SelfHostTenantOperationalMetadataStore", metadataStore.GetType().Name);
        Assert.Null(tableServiceClient);
    }
}
