namespace ImportToPlanner.Web.Infrastructure;

/// <summary>
/// Storage-related settings for the hosted web application.
/// </summary>
internal sealed class StorageOptions
{
    public const string ConfigurationSectionName = "Storage";

    public string DataProtectionContainer { get; set; } = string.Empty;

    public string DataProtectionBlob { get; set; } = string.Empty;

    public StorageConfiguration ToConfiguration()
        => new(DataProtectionContainer, DataProtectionBlob);
}
