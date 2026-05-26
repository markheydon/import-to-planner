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
    TenantAuthorityConfiguration tenantAuthorityConfiguration,
    ILogger<ClaimsTenantContextAccessor> logger,
    UserFacingFailureDiagnostics failureDiagnostics) : ICurrentTenantContextAccessor
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
            throw CreateAndRecordFailure(
                "An authenticated user context is required to resolve the active tenant context.",
                PlannerFailureCategory.Authentication.ToString(),
                "tenant_context.unauthenticated");
        }

        var tenantId = ResolveTenantId(user);
        var accountType = ResolveAccountType(user, tenantId);
        if (tenantAuthorityConfiguration.IsSharedOrganisations
            && accountType != SupportedAccountType.WorkOrSchool)
        {
            throw CreateAndRecordFailure(
                "Unsupported account type. Sign in with a supported work or school account.",
                PlannerFailureCategory.Authentication.ToString(),
                "tenant_context.unsupported_account",
                tenantId);
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw CreateAndRecordFailure(
                "Unable to resolve the tenant identifier from the current sign-in.",
                PlannerFailureCategory.Authentication.ToString(),
                "tenant_context.tenant_id_missing");
        }

        var objectId = ResolveUserIdentifier(user);
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw CreateAndRecordFailure(
                "Unable to resolve the user identifier from the current sign-in.",
                PlannerFailureCategory.Authentication.ToString(),
                "tenant_context.user_id_missing",
                tenantId);
        }

        return new TenantContext(
            tenantId,
            BuildTenantKey(tenantId),
            objectId,
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

        if (tenantAuthorityConfiguration.AuthorityKind != TenantAuthorityKind.SpecificTenant)
        {
            return null;
        }

        return ResolveSpecificTenantAuthorityTenant();
    }

    private SupportedAccountType ResolveAccountType(ClaimsPrincipal user, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!tenantAuthorityConfiguration.IsSharedOrganisations)
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

    private string? ResolveSpecificTenantAuthorityTenant()
    {
        var authorityTenant = tenantAuthorityConfiguration.TenantId;
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

    private InvalidOperationException CreateAndRecordFailure(
        string message,
        string failureCategory,
        string failureCode,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        var exception = new InvalidOperationException(message);
        _ = failureDiagnostics.RecordHandledFailure(
            logger,
            exception,
            "tenant_context.resolve",
            failureCategory,
            message,
            LogLevel.Warning,
            consentStatus: ConsentResolutionStatus.Unknown,
            failureCode: failureCode,
            tenantKeyOverride: string.IsNullOrWhiteSpace(tenantId) ? null : BuildTenantKey(tenantId));

        return exception;
    }
}
