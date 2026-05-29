namespace ImportToPlanner.Web.Infrastructure;

internal sealed record StorageConfiguration(
    string TenantMetadataTable,
    string DataProtectionContainer,
    string DataProtectionBlob)
{
    public static StorageConfiguration FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var tenantMetadataTable = configuration["Storage:TenantMetadataTable"];
        if (string.IsNullOrWhiteSpace(tenantMetadataTable))
        {
            throw new InvalidOperationException("Application startup configuration is invalid: set 'Storage:TenantMetadataTable'.");
        }

        var dataProtectionContainer = configuration["Storage:DataProtectionContainer"];
        if (string.IsNullOrWhiteSpace(dataProtectionContainer))
        {
            throw new InvalidOperationException("Application startup configuration is invalid: set 'Storage:DataProtectionContainer'.");
        }

        var dataProtectionBlob = configuration["Storage:DataProtectionBlob"];
        if (string.IsNullOrWhiteSpace(dataProtectionBlob))
        {
            throw new InvalidOperationException("Application startup configuration is invalid: set 'Storage:DataProtectionBlob'.");
        }

        return new StorageConfiguration(tenantMetadataTable, dataProtectionContainer, dataProtectionBlob);
    }
}
