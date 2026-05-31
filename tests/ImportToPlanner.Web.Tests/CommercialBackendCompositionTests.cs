using ImportToPlanner.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Web.Tests;

public sealed class CommercialBackendCompositionTests
{
    [Fact]
    public void AddCommercialModeServices_WhenCommercialModeDisabled_RegistersDisabledCommercialAdapters()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "false",
            })
            .Build();

        services.AddCommercialModeServices(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ICommercialAccessUseCase)
                && string.Equals(descriptor.ImplementationType?.Name, "DisabledCommercialAccessUseCase", StringComparison.Ordinal));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ICommercialProfileUseCase)
                && string.Equals(descriptor.ImplementationType?.Name, "DisabledCommercialProfileUseCase", StringComparison.Ordinal));
    }

    [Fact]
    public void AddCommercialModeServices_WhenBackendApiEnabled_RegistersBackendCommercialAdapters()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Features:CommercialMode:UseBackendApi"] = "true",
            })
            .Build();

        services.AddCommercialModeServices(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ICommercialAccessUseCase)
                && string.Equals(descriptor.ImplementationType?.Name, "BackendCommercialAccessUseCase", StringComparison.Ordinal));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ICommercialProfileUseCase)
                && string.Equals(descriptor.ImplementationType?.Name, "BackendCommercialProfileUseCase", StringComparison.Ordinal));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ITenantOperationalMetadataStore)
                && string.Equals(descriptor.ImplementationType?.Name, "BackendTenantOperationalMetadataStore", StringComparison.Ordinal));
    }

    [Fact]
    public void AddCommercialModeServices_WhenBackendApiEnabled_DoesNotRegisterWebRetentionHostedService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Features:CommercialMode:UseBackendApi"] = "true",
            })
            .Build();

        services.AddCommercialModeServices(configuration);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && string.Equals(descriptor.ImplementationType?.Name, "CommercialAccountRetentionHostedService", StringComparison.Ordinal));
    }
}
