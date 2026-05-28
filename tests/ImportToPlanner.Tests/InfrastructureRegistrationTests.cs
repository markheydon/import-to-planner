using Azure.Data.Tables;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Infrastructure.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportToPlanner.Tests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersGraphGatewayAndTableMetadataStore_Unconditionally()
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
    public void AddInfrastructure_WhenCommercialModeDisabled_RegistersNoOpCommercialStores()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accountStore = serviceProvider.GetRequiredService<ICommercialAccountStore>();
        var auditStore = serviceProvider.GetRequiredService<ICommercialAuditStore>();

        Assert.Equal("NoOpCommercialAccountStore", accountStore.GetType().Name);
        Assert.Equal("NoOpCommercialAuditStore", auditStore.GetType().Name);
    }
}
