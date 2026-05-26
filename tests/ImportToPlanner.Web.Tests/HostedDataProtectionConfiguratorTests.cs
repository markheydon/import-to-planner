using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Tests;

public sealed class HostedDataProtectionConfiguratorTests
{
    [Fact]
    public void Configure_WithValidStorageSettings_RegistersBlobBackedDataProtection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new BlobServiceClient("UseDevelopmentStorage=true"));
        var configuration = BuildStorageConfiguration();
        var storageConfiguration = StorageConfiguration.FromConfiguration(configuration);

        var exception = Record.Exception(() => HostedDataProtectionConfigurator.Configure(
            services,
            storageConfiguration));

        Assert.Null(exception);

        using var serviceProvider = services.BuildServiceProvider();
        var dataProtectionOptions = serviceProvider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        var blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();

        Assert.Equal(HostedDataProtectionConfigurator.HostedApplicationDiscriminator, dataProtectionOptions.ApplicationDiscriminator);
        Assert.Equal(HostedDataProtectionConfigurator.HostedKeyLifetime, keyManagementOptions.NewKeyLifetime);
        Assert.NotNull(blobServiceClient);
        Assert.NotNull(serviceProvider.GetRequiredService<IDataProtectionProvider>());
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), service => service is HostedDataProtectionContainerBootstrapper);
    }

    [Fact]
    public void FromConfiguration_WhenConnectionStringMissing_ReturnsStorageConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
            })
            .Build();

        var result = StorageConfiguration.FromConfiguration(configuration);

        Assert.Equal("TenantOperationalMetadata", result.TenantMetadataTable);
        Assert.Equal("dataprotection", result.DataProtectionContainer);
        Assert.Equal("keys.xml", result.DataProtectionBlob);
    }

    [Fact]
    public void FromConfiguration_WhenStorageKeyMissing_ThrowsDeterministically()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:DataProtectionContainer"] = "dataprotection",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => StorageConfiguration.FromConfiguration(configuration));

        Assert.Contains("Storage:DataProtectionBlob", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration BuildStorageConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
            })
            .Build();
}
