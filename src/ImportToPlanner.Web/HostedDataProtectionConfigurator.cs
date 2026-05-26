using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace ImportToPlanner.Web;

internal static class HostedDataProtectionConfigurator
{
    internal const string HostedApplicationDiscriminator = "ImportToPlanner.Hosted";
    internal static readonly TimeSpan HostedKeyLifetime = TimeSpan.FromDays(14);

    public static void Configure(
        IServiceCollection services,
        StorageConfiguration storageConfiguration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storageConfiguration);

        var storageSettings = HostedDataProtectionStorageSettings.FromStorageConfiguration(storageConfiguration);

        services
            .AddDataProtection()
            .PersistKeysToAzureBlobStorage(serviceProvider =>
                storageSettings.CreateBlobClient(serviceProvider.GetRequiredService<BlobServiceClient>()));

        services.Configure<DataProtectionOptions>(options =>
        {
            options.ApplicationDiscriminator = HostedApplicationDiscriminator;
        });

        services.Configure<KeyManagementOptions>(options =>
        {
            options.NewKeyLifetime = HostedKeyLifetime;
        });

        services.AddSingleton(storageSettings);
        services.AddHostedService<HostedDataProtectionContainerBootstrapper>();
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

    public BlobContainerClient CreateContainerClient(BlobServiceClient blobServiceClient)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        return blobServiceClient.GetBlobContainerClient(ContainerName);
    }

    public BlobClient CreateBlobClient(BlobServiceClient blobServiceClient)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        return CreateContainerClient(blobServiceClient).GetBlobClient(BlobName);
    }
}

internal sealed class HostedDataProtectionContainerBootstrapper(
    HostedDataProtectionStorageSettings storageSettings,
    BlobServiceClient blobServiceClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // The Azure blob Data Protection provider expects the target container to exist already.
        await storageSettings
            .CreateContainerClient(blobServiceClient)
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
