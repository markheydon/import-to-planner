using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Web.Features.CommercialAccounts.Backend;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Web.Tests;

public sealed class CommercialBackendCompositionTests
{
    [Fact]
    public void AddCommercialModeServices_WhenCommercialModeDisabled_RegistersCommercialApiClient()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
            })
            .Build();

        services.AddCommercialBackendServices(configuration);

        var commercialApiClientType = typeof(TenantAuthorityConfiguration).Assembly.GetType(
            "ImportToPlanner.Web.Features.CommercialAccounts.Backend.CommercialApiServiceClient",
            throwOnError: true)!;

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == commercialApiClientType);
    }

    [Fact]
    public void AddCommercialModeServices_WhenCommercialModeEnabled_RegistersTenantMetadataStore()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        services.AddCommercialBackendServices(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ITenantOperationalMetadataStore)
                && string.Equals(descriptor.ImplementationType?.Name, "BackendTenantOperationalMetadataStore", StringComparison.Ordinal));
    }

    [Fact]
    public void AddCommercialModeServices_WhenCommercialModeEnabled_DoesNotRegisterWebRetentionHostedService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        services.AddCommercialBackendServices(configuration);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && string.Equals(descriptor.ImplementationType?.Name, "CommercialAccountRetentionHostedService", StringComparison.Ordinal));
    }
}
