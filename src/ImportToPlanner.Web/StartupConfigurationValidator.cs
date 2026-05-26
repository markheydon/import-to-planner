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
            ["DeploymentMode"] = "Remove 'DeploymentMode:*'. Authority behaviour now comes from 'AzureAd:TenantId'.",
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
            validationErrors.Add("Set 'AzureAd:TenantId' to 'organizations' or a specific tenant identifier.");
        }
        else if (tenantId.StartsWith("__REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            validationErrors.Add("Replace the placeholder value for 'AzureAd:TenantId'.");
        }

        ValidateRequiredSetting("Storage:TenantMetadataTable");
        ValidateRequiredSetting("Storage:DataProtectionContainer");
        ValidateRequiredSetting("Storage:DataProtectionBlob");

        if (validationErrors.Count > 0)
        {
            var bulletList = string.Join(Environment.NewLine, validationErrors.Select(error => $"- {error}"));
            throw new InvalidOperationException($"Application startup configuration is invalid:{Environment.NewLine}{bulletList}");
        }

        return;

        void ValidateRequiredSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(configuration[key]))
            {
                validationErrors.Add($"Set '{key}'.");
            }
        }
    }
}
