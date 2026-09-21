using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Tests.TestDoubles;

public static class TenantContextAccessorSubstitute
{
    public static ICurrentTenantContextAccessor Create(TenantContext? context = null)
    {
        var substitute = Substitute.For<ICurrentTenantContextAccessor>();
        substitute.GetRequiredContext().Returns(context ?? CreateDefaultContext());
        return substitute;
    }

    private static TenantContext CreateDefaultContext()
        => new(
            "tenant-a",
            "tenant-key-a",
            "user-a",
            SupportedAccountType.WorkOrSchool,
            "Tenant A");
}
