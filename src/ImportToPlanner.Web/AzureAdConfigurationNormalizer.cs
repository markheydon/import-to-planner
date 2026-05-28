namespace ImportToPlanner.Web;

internal static class AzureAdConfigurationNormalizer
{
    private const string AzureAdTenantIdKey = "AzureAd:TenantId";
    private const string AzureAdHomeTenantIdKey = "AzureAd:HomeTenantId";
    private const string AzureAdAppRegistrationTenantIdKey = "AzureAd:AppRegistrationTenantId";

    public static void Apply(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration is not IConfigurationManager configurationManager)
        {
            return;
        }

        var configuredHomeTenantId = configuration[AzureAdHomeTenantIdKey]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredHomeTenantId))
        {
            return;
        }

        var configuredAppRegistrationTenantId = configuration[AzureAdTenantIdKey]?.Trim();
        var effectiveAuthorityTenantId = TenantAuthorityConfiguration.NormalizeHomeTenantId(configuredHomeTenantId);

        configurationManager.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [AzureAdAppRegistrationTenantIdKey] = configuredAppRegistrationTenantId,
                [AzureAdTenantIdKey] = effectiveAuthorityTenantId,
            });
    }
}
