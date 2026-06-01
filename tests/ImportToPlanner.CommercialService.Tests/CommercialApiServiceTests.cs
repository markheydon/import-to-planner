using Azure.Data.Tables;
using ImportToPlanner.CommercialService;
using ImportToPlanner.CommercialService.CommercialAccounts.Services;
using ImportToPlanner.CommercialService.TenantMetadata.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Tests;

public sealed class CommercialApiServiceTests
{
    [Fact]
    public void ConfigureServices_RegistersCommercialOperationUseCases()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        CommercialServiceComposition.ConfigureServices(services, configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accessService = serviceProvider.GetRequiredService<CommercialAccessService>();
        var profileService = serviceProvider.GetRequiredService<CommercialProfileService>();
        var tenantMetadataService = serviceProvider.GetRequiredService<TenantMetadataService>();

        Assert.Equal("CommercialAccessService", accessService.GetType().Name);
        Assert.Equal("CommercialProfileService", profileService.GetType().Name);
        Assert.Equal("TenantMetadataService", tenantMetadataService.GetType().Name);
    }

    [Fact]
    public void ConfigureServices_RegistersRetentionHostedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
                ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
            })
            .Build();

        CommercialServiceComposition.ConfigureServices(services, configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && string.Equals(descriptor.ImplementationType?.Name, "CommercialAccountRetentionHostedService", StringComparison.Ordinal));
    }

    [Fact]
    public void StartupConfigurationValidator_WhenTablesConnectionStringMissing_ThrowsFriendlyError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:CommercialMode:Enabled"] = "true",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => StartupConfigurationValidator.Validate(configuration));

        Assert.Contains("ConnectionStrings:tables", exception.Message, StringComparison.Ordinal);
    }
}
