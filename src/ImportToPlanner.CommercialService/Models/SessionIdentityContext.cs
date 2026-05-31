namespace ImportToPlanner.CommercialService.Models;

/// <summary>
/// Represents the signed-in identity context used to resolve commercial account access.
/// </summary>
/// <param name="TenantId">The tenant identifier for account resolution.</param>
/// <param name="UserId">The user identifier for account resolution.</param>
/// <param name="EmailAddress">The optional session email address for display only.</param>
/// <param name="TenantName">The optional session tenant name for display only.</param>
public sealed record SessionIdentityContext(
    string TenantId,
    string UserId,
    string? EmailAddress,
    string? TenantName);
