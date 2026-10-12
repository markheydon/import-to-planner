using ImportToPlanner.Commercial.Abstractions;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Complimentary policy that never grants unlimited access.
/// </summary>
public sealed class DisabledComplimentaryTenantPolicy : IComplimentaryTenantPolicy
{
    /// <summary>
    /// Gets the shared disabled policy instance.
    /// </summary>
    public static DisabledComplimentaryTenantPolicy Instance { get; } = new();

    /// <inheritdoc/>
    public bool IsComplimentary(string tenantId) => false;
}
