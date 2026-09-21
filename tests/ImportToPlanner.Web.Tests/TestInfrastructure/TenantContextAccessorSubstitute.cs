using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class TenantContextAccessorSubstitute
{
    public TenantContextAccessorSubstitute()
    {
        Instance = Substitute.For<ICurrentTenantContextAccessor>();
        Instance.GetRequiredContext().Returns(_ => GetRequiredContext());
    }

    public ICurrentTenantContextAccessor Instance { get; }

    public Exception? GetRequiredContextException { get; set; }

    public TenantContext Context { get; set; } = new(
        "tenant-a",
        "tenant-key-a",
        "user-a",
        SupportedAccountType.WorkOrSchool,
        "Tenant A");

    private TenantContext GetRequiredContext()
    {
        if (GetRequiredContextException is not null)
        {
            throw GetRequiredContextException;
        }

        return Context;
    }
}
