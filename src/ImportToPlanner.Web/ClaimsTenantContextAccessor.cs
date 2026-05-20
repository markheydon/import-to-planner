using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web;

/// <summary>
/// Resolves the active tenant context from the current authenticated principal.
/// </summary>
internal sealed class ClaimsTenantContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    DeploymentModeConfiguration deploymentModeConfiguration) : ICurrentTenantContextAccessor
{
    private const string CommonAuthorityTenant = "common";
    private const string OrganizationsAuthorityTenant = "organizations";
    private const string LegacyObjectIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string LegacyTenantIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

    /// <inheritdoc/>
    public TenantContext GetRequiredContext()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("An authenticated user context is required to resolve the active tenant context.");
        }

        var tenantId = ResolveTenantId(user);
        var accountType = ResolveAccountType(user, tenantId);
        if (deploymentModeConfiguration.Mode == DeploymentMode.HostedSharedMultiTenant
            && accountType != SupportedAccountType.WorkOrSchool)
        {
            throw new InvalidOperationException("Unsupported account type. Sign in with a supported work or school account.");
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("Unable to resolve the tenant identifier from the current sign-in.");
        }

        var objectId = ResolveUserIdentifier(user);
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new InvalidOperationException("Unable to resolve the user identifier from the current sign-in.");
        }

        return new TenantContext(
            tenantId,
            BuildTenantKey(tenantId),
            objectId,
            deploymentModeConfiguration.Mode,
            accountType,
            user.FindFirstValue("tenant_display_name"));
    }

    private string? ResolveTenantId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var tenantId = user.FindFirstValue("tid")
            ?? user.FindFirstValue(LegacyTenantIdentifierClaimType);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        if (deploymentModeConfiguration.Mode != DeploymentMode.SelfHostedSingleTenant)
        {
            return null;
        }

        return ResolveSelfHostedAuthorityTenant();
    }

    private SupportedAccountType ResolveAccountType(ClaimsPrincipal user, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (deploymentModeConfiguration.Mode != DeploymentMode.HostedSharedMultiTenant)
        {
            return string.IsNullOrWhiteSpace(tenantId)
                ? SupportedAccountType.Unknown
                : SupportedAccountType.WorkOrSchool;
        }

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.Equals(tenantId, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return SupportedAccountType.Unsupported;
        }

        var identityProvider = user.FindFirstValue("idp");
        if (!string.IsNullOrWhiteSpace(identityProvider)
            && identityProvider.Contains("live.com", StringComparison.OrdinalIgnoreCase))
        {
            return SupportedAccountType.Unsupported;
        }

        return SupportedAccountType.WorkOrSchool;
    }

    private string? ResolveSelfHostedAuthorityTenant()
    {
        var authorityTenant = deploymentModeConfiguration.AuthorityTenant;
        if (string.IsNullOrWhiteSpace(authorityTenant)
            || string.Equals(authorityTenant, CommonAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(authorityTenant, OrganizationsAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(authorityTenant, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorityTenant;
    }

    private static string? ResolveUserIdentifier(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.FindFirstValue("oid")
            ?? user.FindFirstValue(LegacyObjectIdentifierClaimType)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
    }

    private static string BuildTenantKey(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId));
        return Convert.ToHexStringLower(hash[..8]);
    }
}
