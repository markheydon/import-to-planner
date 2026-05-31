namespace ImportToPlanner.ApiService.Commercial;

public static class StartupConfigurationValidator
{
    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var validationErrors = new List<string>();

        var commercialModeEnabled = configuration.GetValue<bool>("Features:CommercialMode:Enabled");
        if (!commercialModeEnabled)
        {
            validationErrors.Add("Set 'Features:CommercialMode:Enabled' to 'true' when running commercial API service.");
        }

        ValidateRequiredSetting("Storage:TenantMetadataTable");
        ValidateRequiredSetting("Storage:CommercialAccountsTable");
        ValidateRequiredSetting("Storage:CommercialAuditTable");

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("tables")))
        {
            validationErrors.Add("Set 'ConnectionStrings:tables'.");
        }

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
