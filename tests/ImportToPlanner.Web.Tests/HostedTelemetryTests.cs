using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Tests;

public sealed class HostedTelemetryTests
{
    [Fact]
    public void BuildHostedTelemetryDimensions_EmitsExpectedHostedDimensions()
    {
        var authority = new TenantAuthorityConfiguration(
            "organizations",
            TenantAuthorityKind.SharedOrganisations,
            ["Tasks.ReadWrite"],
            new Uri("https://example.test/admin-consent"));
        var tenantContext = new TenantContext(
            "tenant-a",
            "tenant-key-a",
            "user-a",
            SupportedAccountType.WorkOrSchool,
            "Tenant A");

        var dimensions = HostedTelemetryHelper.BuildHostedTelemetryDimensions(
            authority,
            tenantContext,
            ConsentResolutionStatus.AdminConsentRequired,
            "Authentication");

        Assert.Equal("SharedOrganisations", dimensions["authority.kind"]);
        Assert.Equal("tenant-key-a", dimensions["tenant.key"]);
        Assert.Equal("AdminConsentRequired", dimensions["consent.status"]);
        Assert.Equal("Authentication", dimensions["failure.category"]);
    }

    [Fact]
    public void BuildHostedTelemetryDimensions_DoesNotIncludeSensitiveValues()
    {
        var authority = new TenantAuthorityConfiguration(
            "tenant-a",
            TenantAuthorityKind.SpecificTenant,
            ["User.Read"],
            null);
        var dimensions = HostedTelemetryHelper.BuildHostedTelemetryDimensions(
            authority,
            null,
            ConsentResolutionStatus.Unknown,
            null);

        Assert.DoesNotContain(dimensions.Keys, key => key.Contains("token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dimensions.Keys, key => key.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dimensions.Keys, key => key.Contains("password", StringComparison.OrdinalIgnoreCase));
    }
}
