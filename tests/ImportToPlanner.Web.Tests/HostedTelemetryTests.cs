using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Tests;

public sealed class HostedTelemetryTests
{
    [Fact]
    public void BuildHostedTelemetryDimensions_EmitsExpectedHostedDimensions()
    {
        var deployment = new DeploymentModeConfiguration(
            DeploymentMode.HostedSharedMultiTenant,
            "organizations",
            true,
            true,
            "SingleActiveReplica",
            ["Tasks.ReadWrite"],
            new Uri("https://example.test/admin-consent"));
        var tenantContext = new TenantContext(
            "tenant-a",
            "tenant-key-a",
            "user-a",
            DeploymentMode.HostedSharedMultiTenant,
            SupportedAccountType.WorkOrSchool,
            "Tenant A");

        var dimensions = HostedTelemetryHelper.BuildHostedTelemetryDimensions(
            deployment,
            tenantContext,
            ConsentResolutionStatus.AdminConsentRequired,
            "Authentication");

        Assert.Equal("HostedSharedMultiTenant", dimensions["deployment.mode"]);
        Assert.Equal("tenant-key-a", dimensions["tenant.key"]);
        Assert.Equal("AdminConsentRequired", dimensions["consent.status"]);
        Assert.Equal("Graph", dimensions["planner.gateway.mode"]);
        Assert.Equal("Authentication", dimensions["failure.category"]);
    }

    [Fact]
    public void BuildHostedTelemetryDimensions_DoesNotIncludeSensitiveValues()
    {
        var deployment = DeploymentModeConfiguration.Default;
        var dimensions = HostedTelemetryHelper.BuildHostedTelemetryDimensions(
            deployment,
            null,
            ConsentResolutionStatus.Unknown,
            null);

        Assert.DoesNotContain(dimensions.Keys, key => key.Contains("token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dimensions.Keys, key => key.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dimensions.Keys, key => key.Contains("password", StringComparison.OrdinalIgnoreCase));
    }
}
