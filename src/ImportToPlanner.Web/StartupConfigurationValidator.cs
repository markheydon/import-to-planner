namespace ImportToPlanner.Web;

internal static class StartupConfigurationValidator
{
    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var validationErrors = new List<string>();

        var obsoleteConfigurationRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PlannerGateway"] = "Remove 'PlannerGateway:*'. The app now always uses the Graph planner gateway.",
            ["HostedStorage"] = "Remove 'HostedStorage:*'. Use 'Storage:*' and AppHost storage references instead.",
            ["DeploymentMode"] = "Remove 'DeploymentMode:*'. Authority behaviour now comes from 'AzureAd:HomeTenantId' or the legacy 'AzureAd:TenantId' fallback.",
        };

        foreach (var rule in obsoleteConfigurationRules)
        {
            if (configuration.GetSection(rule.Key).Exists())
            {
                validationErrors.Add(rule.Value);
            }
        }

        var tenantId = configuration["AzureAd:TenantId"];
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            validationErrors.Add("Set 'AzureAd:TenantId' to the tenant that owns your app registration.");
        }
        else if (tenantId.StartsWith("__REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            validationErrors.Add("Replace the placeholder value for 'AzureAd:TenantId'.");
        }

        var homeTenantId = configuration["AzureAd:HomeTenantId"];
        if (!string.IsNullOrWhiteSpace(homeTenantId))
        {
            if (homeTenantId.StartsWith("__REPLACE", StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add("Replace the placeholder value for 'AzureAd:HomeTenantId'.");
            }
            else if (string.Equals(homeTenantId, TenantAuthorityConfiguration.OrganizationsAuthorityTenant, StringComparison.OrdinalIgnoreCase)
                || string.Equals(homeTenantId, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add($"Set 'AzureAd:HomeTenantId' to '{TenantAuthorityConfiguration.MultipleHomeTenantAlias}' or a specific tenant identifier.");
            }
        }

        ValidateRequiredSetting("Storage:TenantMetadataTable");
        ValidateRequiredSetting("Storage:DataProtectionContainer");
        ValidateRequiredSetting("Storage:DataProtectionBlob");

        if (validationErrors.Count > 0)
        {
            var bulletList = string.Join(Environment.NewLine, validationErrors.Select(error => $"- {error}"));
            throw new InvalidOperationException($"Application startup configuration is invalid:{Environment.NewLine}{bulletList}");
        }

        void ValidateRequiredSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(configuration[key]))
            {
                validationErrors.Add($"Set '{key}'.");
            }
        }
    }
}
