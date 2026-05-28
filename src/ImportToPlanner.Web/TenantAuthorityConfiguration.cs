namespace ImportToPlanner.Web;

internal enum TenantAuthorityKind
{
    SharedOrganisations,
    SpecificTenant,
}

internal sealed record TenantAuthorityConfiguration(
    string TenantId,
    TenantAuthorityKind AuthorityKind,
    IReadOnlyList<string> RequiredScopes,
    Uri? AdminConsentUri,
    string AppRegistrationTenantId,
    string? HomeTenantId)
{
    internal const string MultipleHomeTenantAlias = "multiple";
    private const string OrganizationsAuthorityTenant = "organizations";

    public bool IsSharedOrganisations => AuthorityKind == TenantAuthorityKind.SharedOrganisations;

    public static TenantAuthorityConfiguration FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var tenantId = configuration["AzureAd:TenantId"]?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("Application startup configuration is invalid: set 'AzureAd:TenantId' to 'organizations' or your tenant identifier.");
        }

        if (tenantId.StartsWith("__REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Application startup configuration is invalid: replace the placeholder value for 'AzureAd:TenantId'.");
        }

        var appRegistrationTenantId = configuration["AzureAd:AppRegistrationTenantId"]?.Trim();
        if (string.IsNullOrWhiteSpace(appRegistrationTenantId))
        {
            appRegistrationTenantId = tenantId;
        }

        var homeTenantId = configuration["AzureAd:HomeTenantId"]?.Trim();

        var authorityKind = string.Equals(tenantId, OrganizationsAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            ? TenantAuthorityKind.SharedOrganisations
            : TenantAuthorityKind.SpecificTenant;

        var requiredScopes = configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>() ?? ["User.Read"];
        var adminConsentUri = BuildAdminConsentUri(configuration, tenantId);

        return new TenantAuthorityConfiguration(tenantId, authorityKind, requiredScopes, adminConsentUri, appRegistrationTenantId, homeTenantId);
    }

    public static string NormalizeHomeTenantId(string homeTenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(homeTenantId);

        var normalizedHomeTenantId = homeTenantId.Trim();
        return string.Equals(normalizedHomeTenantId, MultipleHomeTenantAlias, StringComparison.OrdinalIgnoreCase)
            ? OrganizationsAuthorityTenant
            : normalizedHomeTenantId;
    }

    private static Uri? BuildAdminConsentUri(IConfiguration configuration, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var configured = configuration["AzureAd:AdminConsentUri"];
        if (Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri))
        {
            return configuredUri;
        }

        var clientId = configuration["AzureAd:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var instance = configuration["AzureAd:Instance"];
        if (!Uri.TryCreate(instance, UriKind.Absolute, out var authorityInstance))
        {
            return null;
        }

        var adminConsentBuilder = new UriBuilder(authorityInstance)
        {
            Path = $"{tenantId.Trim('/')}/v2.0/adminconsent",
            Query = $"client_id={Uri.EscapeDataString(clientId)}",
        };

        return adminConsentBuilder.Uri;
    }
}
