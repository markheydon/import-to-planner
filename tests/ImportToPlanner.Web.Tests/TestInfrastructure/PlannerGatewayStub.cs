using ImportToPlanner.Application.Common.Abstractions;
using ImportToPlanner.Application.Common.Exceptions;
using ImportToPlanner.Application.Common.Models;
using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Application.TenantContext.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class PlannerGatewayStub : IPlannerGateway
{
    public Exception? GetAvailableContainersException { get; set; }

    public Exception? GetPlansException { get; set; }

    public Exception? CreateTaskException { get; set; }

    public IReadOnlyList<PlannerContainer> Containers { get; set; } =
    [
        new PlannerContainer("container-1", "Test Container", ContainerType.Group),
    ];

    public IReadOnlyList<PlannerPlan> Plans { get; set; } =
    [
        new PlannerPlan("plan-1", "Test Plan", "container-1", ContainerType.Group),
    ];

    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
    {
        if (GetAvailableContainersException is not null)
        {
            return Task.FromException<IReadOnlyList<PlannerContainer>>(GetAvailableContainersException);
        }

        return Task.FromResult(Containers);
    }

    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult(Plans.FirstOrDefault(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
    {
        if (GetPlansException is not null)
        {
            return Task.FromException<IReadOnlyList<PlannerPlan>>(GetPlansException);
        }

        return Task.FromResult(Plans);
    }

    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerBucket>>([]);

    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
        => Task.FromResult(new PlannerBucket(Guid.NewGuid().ToString("N"), bucketName, planId));

    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>([]);

    public Task<PlannerTaskSnapshot> CreateTaskAsync(string planId, string bucketId, string taskName, string? description, int? priority, string? goal, CancellationToken cancellationToken)
    {
        if (CreateTaskException is not null)
        {
            return Task.FromException<PlannerTaskSnapshot>(CreateTaskException);
        }

        return Task.FromResult(new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId));
    }

    public static PlannerOperationException AuthenticationFailure()
    {
        return new PlannerOperationException(new PlannerOperationFailure(
            PlannerFailureCategory.Authentication,
            PlannerFailureTarget.Workflow,
            null,
            "Authentication failed.",
            false,
            "Authentication"));
    }
}

internal sealed class TenantOperationalMetadataStoreStub : ITenantOperationalMetadataStore
{
    private readonly Dictionary<string, TenantOperationalMetadata> values = new(StringComparer.OrdinalIgnoreCase);

    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        values.TryGetValue(tenantId, out var value);
        return Task.FromResult(value);
    }

    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        values[metadata.TenantId] = metadata;
        return Task.CompletedTask;
    }
}

internal sealed class CurrentTenantContextAccessorStub : ICurrentTenantContextAccessor
{
    public Exception? GetRequiredContextException { get; set; }

    public TenantContext Context { get; set; } = new(
        "tenant-a",
        "tenant-key-a",
        "user-a",
        SupportedAccountType.WorkOrSchool,
        "Tenant A");

    public TenantContext GetRequiredContext()
    {
        if (GetRequiredContextException is not null)
        {
            throw GetRequiredContextException;
        }

        return Context;
    }
}
