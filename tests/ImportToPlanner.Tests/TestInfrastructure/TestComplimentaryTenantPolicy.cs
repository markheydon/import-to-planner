using ImportToPlanner.Commercial.Abstractions;

namespace ImportToPlanner.Tests.TestInfrastructure;

/// <summary>
/// Test policy with a fixed allowlist of tenant identifiers.
/// </summary>
public sealed class TestComplimentaryTenantPolicy : IComplimentaryTenantPolicy
{
    private readonly HashSet<string> allowlistedTenantIds;

    public TestComplimentaryTenantPolicy(params string[] tenantIds)
    {
        allowlistedTenantIds = new HashSet<string>(tenantIds, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public bool IsComplimentary(string tenantId) => allowlistedTenantIds.Contains(tenantId);
}
