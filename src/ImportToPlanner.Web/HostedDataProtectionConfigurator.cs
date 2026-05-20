using Azure.Storage.Blobs;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace ImportToPlanner.Web;

internal static class HostedDataProtectionConfigurator
{
    internal const string HostedApplicationDiscriminator = "ImportToPlanner.Hosted";
    internal static readonly TimeSpan HostedKeyLifetime = TimeSpan.FromDays(14);

    public static void Configure(
        IServiceCollection services,
        IConfiguration configuration,
        DeploymentModeConfiguration deploymentModeConfiguration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(deploymentModeConfiguration);

        if (deploymentModeConfiguration.Mode != DeploymentMode.HostedSharedMultiTenant
            || !deploymentModeConfiguration.HostedStorageEnabled)
        {
            return;
        }

        var storageSettings = HostedDataProtectionStorageSettings.FromConfiguration(configuration);

        services
            .AddDataProtection()
            .PersistKeysToAzureBlobStorage(
                storageSettings.ConnectionString,
                storageSettings.ContainerName,
                storageSettings.BlobName);

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
    string ConnectionString,
    string ContainerName,
    string BlobName)
{
    public static HostedDataProtectionStorageSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration["HostedStorage:ConnectionString"];
        var containerName = configuration["HostedStorage:DataProtectionContainer"];
        var blobName = configuration["HostedStorage:DataProtectionBlob"];

        var missingSettings = new List<string>();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            missingSettings.Add("HostedStorage:ConnectionString");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            missingSettings.Add("HostedStorage:DataProtectionContainer");
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            missingSettings.Add("HostedStorage:DataProtectionBlob");
        }

        if (missingSettings.Count > 0)
        {
            throw new InvalidOperationException(
                $"Hosted shared multi-tenant mode with hosted storage enabled requires {string.Join(", ", missingSettings.Select(setting => $"'{setting}'"))} to configure durable Data Protection key persistence.");
        }

        return new HostedDataProtectionStorageSettings(connectionString!, containerName!, blobName!);
    }

    public BlobContainerClient CreateContainerClient()
        => new(ConnectionString, ContainerName);
}

internal sealed class HostedDataProtectionContainerBootstrapper(HostedDataProtectionStorageSettings storageSettings) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // The Azure blob Data Protection provider expects the target container to exist already.
        await storageSettings
            .CreateContainerClient()
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
