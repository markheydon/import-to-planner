using System.Security.Claims;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Http;

namespace ImportToPlanner.Web.Tests;

public sealed class ClaimsTenantContextAccessorTests
{
    [Fact]
    public void GetRequiredContext_WhenHostedAndLegacyTenantClaimExists_ResolvesWorkOrSchoolTenantContext()
    {
        var user = CreateAuthenticatedPrincipal(
            new Claim("http://schemas.microsoft.com/identity/claims/tenantid", "tenant-legacy"),
            new Claim(ClaimTypes.NameIdentifier, "user-nameid"));
        var accessor = CreateAccessor(
            user,
            CreateDeploymentModeConfiguration(DeploymentMode.HostedSharedMultiTenant, "organizations"));

        var context = accessor.GetRequiredContext();

        Assert.Equal("tenant-legacy", context.TenantId);
        Assert.Equal("user-nameid", context.UserObjectId);
        Assert.Equal(SupportedAccountType.WorkOrSchool, context.AccountType);
    }

    [Fact]
    public void GetRequiredContext_WhenSelfHostedAndTenantClaimMissing_UsesConfiguredAuthorityTenant()
    {
        var user = CreateAuthenticatedPrincipal(
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-object"));
        var accessor = CreateAccessor(
            user,
            CreateDeploymentModeConfiguration(DeploymentMode.SelfHostedSingleTenant, "tenant-self-hosted"));

        var context = accessor.GetRequiredContext();

        Assert.Equal("tenant-self-hosted", context.TenantId);
        Assert.Equal("user-object", context.UserObjectId);
        Assert.Equal(SupportedAccountType.WorkOrSchool, context.AccountType);
    }

    [Fact]
    public void GetRequiredContext_WhenSelfHostedAndAuthorityTenantIsCommon_ThrowsTenantIdentifierError()
    {
        var user = CreateAuthenticatedPrincipal(new Claim("oid", "user-123"));
        var accessor = CreateAccessor(
            user,
            CreateDeploymentModeConfiguration(DeploymentMode.SelfHostedSingleTenant, "common"));

        var exception = Assert.Throws<InvalidOperationException>(() => accessor.GetRequiredContext());

        Assert.Contains("Unable to resolve the tenant identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequiredContext_WhenHostedAndConsumerTenant_ThrowsUnsupportedAccountError()
    {
        var user = CreateAuthenticatedPrincipal(
            new Claim("tid", AuthTenantConstants.ConsumerTenantId),
            new Claim("oid", "user-123"));
        var accessor = CreateAccessor(
            user,
            CreateDeploymentModeConfiguration(DeploymentMode.HostedSharedMultiTenant, "organizations"));

        var exception = Assert.Throws<InvalidOperationException>(() => accessor.GetRequiredContext());

        Assert.Contains("Unsupported account type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ICurrentTenantContextAccessor CreateAccessor(ClaimsPrincipal user, DeploymentModeConfiguration deploymentModeConfiguration)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };

        var accessorType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.ClaimsTenantContextAccessor", throwOnError: true)!;
        return (ICurrentTenantContextAccessor)Activator.CreateInstance(accessorType, httpContextAccessor, deploymentModeConfiguration)!;
    }

    private static DeploymentModeConfiguration CreateDeploymentModeConfiguration(DeploymentMode mode, string authorityTenant)
        => new(
            mode,
            authorityTenant,
            true,
            false,
            "SingleActiveReplica",
            ["User.Read"],
            null);

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "test-auth");
        return new ClaimsPrincipal(identity);
    }
}
