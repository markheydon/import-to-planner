namespace ImportToPlanner.Commercial.Abstractions;

/// <summary>
/// Determines whether a commercial tenant has complimentary unlimited credit access.
/// </summary>
public interface IComplimentaryTenantPolicy
{
    /// <summary>
    /// Returns whether the supplied tenant identifier is on the complimentary allowlist.
    /// </summary>
    /// <param name="tenantId">Authenticated organisation tenant identifier.</param>
    bool IsComplimentary(string tenantId);
}
