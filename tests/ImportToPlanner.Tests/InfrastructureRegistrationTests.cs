using ImportToPlanner.Application.Common.Abstractions;
using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Infrastructure.Graph.Planner;
using Microsoft.Extensions.Configuration;

namespace ImportToPlanner.Tests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void AddMicrosoftGraphInfrastructure_WhenCommercialModeDisabled_RegistersGraphGatewayAndSelfHostMetadataStore()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
            })
            .Build();

        services.AddMicrosoftGraphInfrastructure(configuration);

        var plannerDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(IPlannerGateway));
        Assert.Equal(ServiceLifetime.Scoped, plannerDescriptor.Lifetime);
        Assert.Equal(typeof(GraphPlannerGateway), plannerDescriptor.ImplementationType);

        using var serviceProvider = services.BuildServiceProvider();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();

        Assert.Equal("SelfHostTenantOperationalMetadataStore", metadataStore.GetType().Name);
    }

    [Fact]
    public void AddMicrosoftGraphInfrastructure_WhenCommercialModeEnabled_RegistersGraphGatewayAndSelfHostMetadataStore()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        services.AddMicrosoftGraphInfrastructure(configuration);

        var plannerDescriptor = services.Single(descriptor => descriptor.ServiceType == typeof(IPlannerGateway));
        Assert.Equal(ServiceLifetime.Scoped, plannerDescriptor.Lifetime);
        Assert.Equal(typeof(GraphPlannerGateway), plannerDescriptor.ImplementationType);

        using var serviceProvider = services.BuildServiceProvider();
        var metadataStore = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();

        Assert.Equal("SelfHostTenantOperationalMetadataStore", metadataStore.GetType().Name);
    }
}
