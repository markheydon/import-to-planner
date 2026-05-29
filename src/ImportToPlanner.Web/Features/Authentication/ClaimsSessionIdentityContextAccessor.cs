using System.Security.Claims;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.Authentication;

internal interface ISessionIdentityContextAccessor
{
    SessionIdentityContext? TryGetCurrent();
}

/// <summary>
/// Resolves the commercial session identity context from authenticated claims.
/// </summary>
internal sealed class ClaimsSessionIdentityContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    TenantAuthorityConfiguration tenantAuthorityConfiguration) : ISessionIdentityContextAccessor
{
    private const string LegacyObjectIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string LegacyTenantIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
    private const string LegacyTenantDisplayNameClaimType = "http://schemas.microsoft.com/identity/claims/tenantdisplayname";

    public SessionIdentityContext? TryGetCurrent()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var tenantId = ResolveTenantId(user);
        var userId = ResolveUserId(user);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var emailAddress = user.FindFirstValue("preferred_username")
            ?? user.FindFirstValue("email")
            ?? user.FindFirstValue("unique_name")
            ?? user.FindFirstValue(ClaimTypes.Upn)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name;

        var tenantName = user.FindFirstValue("tenant_display_name")
            ?? user.FindFirstValue("tenant_name")
            ?? user.FindFirstValue(LegacyTenantDisplayNameClaimType);

        return new SessionIdentityContext(tenantId, userId, emailAddress, tenantName);
    }

    private string? ResolveTenantId(ClaimsPrincipal principal)
    {
        var tenantId = principal.FindFirstValue("tid")
            ?? principal.FindFirstValue(LegacyTenantIdentifierClaimType);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        if (tenantAuthorityConfiguration.AuthorityKind != TenantAuthorityKind.SpecificTenant)
        {
            return null;
        }

        var configuredTenant = tenantAuthorityConfiguration.TenantId;
        if (string.IsNullOrWhiteSpace(configuredTenant)
            || string.Equals(configuredTenant, TenantAuthorityConfiguration.CommonAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuredTenant, TenantAuthorityConfiguration.OrganizationsAuthorityTenant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuredTenant, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return configuredTenant;
    }

    private static string? ResolveUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("oid")
            ?? principal.FindFirstValue(LegacyObjectIdentifierClaimType)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
    }
}
