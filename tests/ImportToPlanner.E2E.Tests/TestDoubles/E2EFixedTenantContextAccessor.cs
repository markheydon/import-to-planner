using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.E2E.Tests.TestDoubles;

/// <summary>
/// Supplies a stable tenant context when Blazor circuits do not have an HTTP context.
/// </summary>
internal sealed class E2EFixedTenantContextAccessor : ICurrentTenantContextAccessor
{
    public TenantContext GetRequiredContext()
        => new(
            "00000000-0000-0000-0000-000000000001",
            "e2e-tenant-key",
            "e2e-test-user",
            SupportedAccountType.WorkOrSchool,
            "Demonstration tenant");
}
