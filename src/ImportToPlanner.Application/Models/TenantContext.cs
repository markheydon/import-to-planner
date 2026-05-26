namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents supported account classifications for tenant resolution.
/// </summary>
public enum SupportedAccountType
{
    Unknown,
    WorkOrSchool,
    Unsupported,
}

/// <summary>
/// Represents the active tenant context for the current signed-in user session.
/// </summary>
/// <param name="TenantId">The authoritative tenant identifier.</param>
/// <param name="TenantKey">A support-safe stable tenant key.</param>
/// <param name="UserObjectId">The signed-in user object identifier.</param>
/// <param name="AccountType">The resolved account type.</param>
/// <param name="DisplayName">The optional tenant display name.</param>
public sealed record TenantContext(
    string TenantId,
    string TenantKey,
    string UserObjectId,
    SupportedAccountType AccountType,
    string? DisplayName);
