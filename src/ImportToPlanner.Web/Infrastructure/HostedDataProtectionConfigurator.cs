using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace ImportToPlanner.Web.Infrastructure;

internal static class HostedDataProtectionConfigurator
{
    internal const string HostedApplicationDiscriminator = "ImportToPlanner.Hosted";
    internal static readonly TimeSpan HostedKeyLifetime = TimeSpan.FromDays(14);

    public static void Configure(IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        var dataProtectionBuilder = services.AddDataProtection();

        if (E2ETestingHostEnvironment.IsE2ETesting(hostEnvironment))
        {
            var keysDirectory = Path.Combine(Path.GetTempPath(), "import-to-planner-e2e-dataprotection");
            Directory.CreateDirectory(keysDirectory);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
        }
        else
        {
            dataProtectionBuilder.PersistKeysToAzureBlobStorage(serviceProvider =>
            {
                var storageConfiguration = serviceProvider.GetRequiredService<StorageConfiguration>();
                var storageSettings = HostedDataProtectionStorageSettings.FromStorageConfiguration(storageConfiguration);
                return storageSettings.CreateBlobClient(serviceProvider.GetRequiredService<BlobServiceClient>());
            });

            services.AddSingleton(serviceProvider =>
            {
                var storageConfiguration = serviceProvider.GetRequiredService<StorageConfiguration>();
                return HostedDataProtectionStorageSettings.FromStorageConfiguration(storageConfiguration);
            });
        }

        services.Configure<DataProtectionOptions>(options =>
        {
            options.ApplicationDiscriminator = HostedApplicationDiscriminator;
        });

        services.Configure<KeyManagementOptions>(options =>
        {
            options.NewKeyLifetime = HostedKeyLifetime;
        });
    }
}

internal sealed record HostedDataProtectionStorageSettings(
    string ContainerName,
    string BlobName)
{
    public static HostedDataProtectionStorageSettings FromStorageConfiguration(StorageConfiguration storageConfiguration)
    {
        ArgumentNullException.ThrowIfNull(storageConfiguration);

        return new HostedDataProtectionStorageSettings(
            storageConfiguration.DataProtectionContainer,
            storageConfiguration.DataProtectionBlob);
    }

    public BlobClient CreateBlobClient(BlobServiceClient blobServiceClient)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        return blobServiceClient
            .GetBlobContainerClient(ContainerName)
            .GetBlobClient(BlobName);
    }
}
