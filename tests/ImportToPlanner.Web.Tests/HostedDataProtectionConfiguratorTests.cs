using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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

        services.AddSingleton(storageConfiguration);
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = Environments.Production };

        var exception = Record.Exception(() => HostedDataProtectionConfigurator.Configure(
            services,
            hostEnvironment));

        Assert.Null(exception);

        using var serviceProvider = services.BuildServiceProvider();
        var dataProtectionOptions = serviceProvider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        var blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();

        Assert.Equal(HostedDataProtectionConfigurator.HostedApplicationDiscriminator, dataProtectionOptions.ApplicationDiscriminator);
        Assert.Equal(HostedDataProtectionConfigurator.HostedKeyLifetime, keyManagementOptions.NewKeyLifetime);
        Assert.NotNull(blobServiceClient);
        Assert.NotNull(serviceProvider.GetRequiredService<IDataProtectionProvider>());
    }

    [Fact]
    public void FromConfiguration_WhenConnectionStringMissing_ReturnsStorageConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
            })
            .Build();

        var result = StorageConfiguration.FromConfiguration(configuration);

        Assert.Equal("dataprotection", result.DataProtectionContainer);
        Assert.Equal("keys.xml", result.DataProtectionBlob);
    }

    [Fact]
    public void FromConfiguration_WhenStorageKeyMissing_ThrowsDeterministically()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataProtectionContainer"] = "dataprotection",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => StorageConfiguration.FromConfiguration(configuration));

        Assert.Contains("Storage:DataProtectionBlob", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_InE2ETestingEnvironment_UsesEphemeralFileSystemKeys()
    {
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = E2ETestingHostEnvironment.EnvironmentName };

        var exception = Record.Exception(() => HostedDataProtectionConfigurator.Configure(services, hostEnvironment));

        Assert.Null(exception);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IDataProtectionProvider>());
    }

    private static IConfiguration BuildStorageConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
            })
            .Build();

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "ImportToPlanner.Web.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
