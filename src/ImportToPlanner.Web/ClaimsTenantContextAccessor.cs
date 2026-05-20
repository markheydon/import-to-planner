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
    private const string ConsumerTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    /// <inheritdoc/>
    public TenantContext GetRequiredContext()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("An authenticated user context is required to resolve the active tenant context.");
        }

        var tenantId = user.FindFirstValue("tid");
        var accountType = ResolveAccountType(user, tenantId);
        if (accountType != SupportedAccountType.WorkOrSchool)
        {
            throw new InvalidOperationException("Unsupported account type. Sign in with a supported work or school account.");
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("Unable to resolve the tenant identifier from the current sign-in.");
        }

        var objectId = user.FindFirstValue("oid") ?? user.FindFirstValue("sub");
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

    private static SupportedAccountType ResolveAccountType(ClaimsPrincipal user, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.Equals(tenantId, ConsumerTenantId, StringComparison.OrdinalIgnoreCase))
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

    private static string BuildTenantKey(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId));
        return Convert.ToHexStringLower(hash[..8]);
    }
}
