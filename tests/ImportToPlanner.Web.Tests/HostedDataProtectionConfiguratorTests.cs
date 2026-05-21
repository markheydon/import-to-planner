using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Tests;

public sealed class HostedDataProtectionConfiguratorTests
{
    [Fact]
    public void Configure_WhenHostedSharedMultiTenantWithHostedStorageEnabled_RegistersHostedBlobDataProtection()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["HostedStorage:ConnectionString"] = "UseDevelopmentStorage=true",
            ["HostedStorage:DataProtectionContainer"] = "dataprotection",
            ["HostedStorage:DataProtectionBlob"] = "keys.xml",
        });

        var exception = Record.Exception(() => HostedDataProtectionConfigurator.Configure(
            services,
            configuration,
            CreateDeploymentModeConfiguration(DeploymentMode.HostedSharedMultiTenant, hostedStorageEnabled: true)));

        Assert.Null(exception);

        using var serviceProvider = services.BuildServiceProvider();
        var dataProtectionOptions = serviceProvider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        Assert.Equal(HostedDataProtectionConfigurator.HostedApplicationDiscriminator, dataProtectionOptions.ApplicationDiscriminator);
        Assert.Equal(HostedDataProtectionConfigurator.HostedKeyLifetime, keyManagementOptions.NewKeyLifetime);
        Assert.NotNull(serviceProvider.GetRequiredService<IDataProtectionProvider>());
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), service => service is HostedDataProtectionContainerBootstrapper);
    }

    [Fact]
    public void Configure_WhenHostedConnectionStringIsProvidedViaAspireConnectionStrings_RegistersHostedBlobDataProtection()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:hostedstorageblobs"] = "UseDevelopmentStorage=true",
            ["HostedStorage:DataProtectionContainer"] = "dataprotection",
            ["HostedStorage:DataProtectionBlob"] = "keys.xml",
        });

        var exception = Record.Exception(() => HostedDataProtectionConfigurator.Configure(
            services,
            configuration,
            CreateDeploymentModeConfiguration(DeploymentMode.HostedSharedMultiTenant, hostedStorageEnabled: true)));

        Assert.Null(exception);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IDataProtectionProvider>());
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), service => service is HostedDataProtectionContainerBootstrapper);
    }

    [Theory]
    [InlineData("HostedStorage:ConnectionString")]
    [InlineData("HostedStorage:DataProtectionContainer")]
    [InlineData("HostedStorage:DataProtectionBlob")]
    public void Configure_WhenHostedStorageSettingMissing_ThrowsDeterministically(string missingSetting)
    {
        var settings = new Dictionary<string, string?>
        {
            ["HostedStorage:ConnectionString"] = "UseDevelopmentStorage=true",
            ["HostedStorage:DataProtectionContainer"] = "dataprotection",
            ["HostedStorage:DataProtectionBlob"] = "keys.xml",
        };
        settings.Remove(missingSetting);

        var exception = Assert.Throws<InvalidOperationException>(() => HostedDataProtectionConfigurator.Configure(
            new ServiceCollection(),
            BuildConfiguration(settings),
            CreateDeploymentModeConfiguration(DeploymentMode.HostedSharedMultiTenant, hostedStorageEnabled: true)));

        Assert.Contains(missingSetting, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_WhenSelfHostedSingleTenant_DoesNotRequireHostedStorage()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() => HostedDataProtectionConfigurator.Configure(
            services,
            BuildConfiguration([]),
            CreateDeploymentModeConfiguration(DeploymentMode.SelfHostedSingleTenant, hostedStorageEnabled: false)));

        Assert.Null(exception);
        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType == typeof(HostedDataProtectionContainerBootstrapper));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IConfigureOptions<DataProtectionOptions>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IConfigureOptions<KeyManagementOptions>));
    }

    private static IConfiguration BuildConfiguration(IEnumerable<KeyValuePair<string, string?>> settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

    private static DeploymentModeConfiguration CreateDeploymentModeConfiguration(DeploymentMode mode, bool hostedStorageEnabled)
        => new(
            mode,
            mode == DeploymentMode.HostedSharedMultiTenant ? "organizations" : "common",
            UseGraphGateway: true,
            HostedStorageEnabled: hostedStorageEnabled,
            InitialReplicaPolicy: "SingleActiveReplica",
            RequiredScopes: ["User.Read"],
            AdminConsentUri: null);
}
