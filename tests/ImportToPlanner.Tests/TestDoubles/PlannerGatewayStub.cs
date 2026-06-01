using ImportToPlanner.Application.Common.Abstractions;
using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Application.TenantContext.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Tests.TestDoubles;

public sealed class PlannerGatewayStub : IPlannerGateway
{
    private readonly List<PlannerPlan> plans = [];
    private readonly Dictionary<string, List<PlannerBucket>> buckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasks = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerContainer>>([]);

    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<PlannerPlan?>(plans.FirstOrDefault(plan => string.Equals(plan.Id, planId, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerPlan>>(plans.Where(plan => string.Equals(plan.ContainerId, containerId, StringComparison.OrdinalIgnoreCase)).ToArray());

    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerBucket>>(buckets.GetValueOrDefault(planId, []));

    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
    {
        if (!buckets.TryGetValue(planId, out var planBuckets))
        {
            planBuckets = [];
            buckets[planId] = planBuckets;
        }

        var existing = planBuckets.FirstOrDefault(bucket => string.Equals(bucket.Name, bucketName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var bucket = new PlannerBucket(Guid.NewGuid().ToString("N"), bucketName, planId);
        planBuckets.Add(bucket);
        return Task.FromResult(bucket);
    }

    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>(tasks.GetValueOrDefault(planId, []));

    public Task<PlannerTaskSnapshot> CreateTaskAsync(string planId, string bucketId, string taskName, string? description, int? priority, string? goal, CancellationToken cancellationToken)
    {
        if (!tasks.TryGetValue(planId, out var planTasks))
        {
            planTasks = [];
            tasks[planId] = planTasks;
        }

        var existing = planTasks.FirstOrDefault(task => string.Equals(task.Title, taskName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var task = new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId);
        planTasks.Add(task);
        return Task.FromResult(task);
    }

    public void AddPlan(string planId, string containerId, ContainerType containerType, string planName)
    {
        plans.Add(new PlannerPlan(planId, planName, containerId, containerType));
        buckets.TryAdd(planId, []);
        tasks.TryAdd(planId, []);
    }
}

public sealed class TenantOperationalMetadataStoreStub : ITenantOperationalMetadataStore
{
    private readonly Dictionary<string, TenantOperationalMetadata> values = new(StringComparer.OrdinalIgnoreCase);

    public Task<TenantOperationalMetadata?> GetAsync(string tenantId, CancellationToken cancellationToken)
    {
        values.TryGetValue(tenantId, out var value);
        return Task.FromResult(value);
    }

    public Task UpsertAsync(TenantOperationalMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        values[metadata.TenantId] = metadata;
        return Task.CompletedTask;
    }
}

public sealed class CurrentTenantContextAccessorStub : ICurrentTenantContextAccessor
{
    public TenantContext Context { get; set; } = new(
        "tenant-a",
        "tenant-key-a",
        "user-a",
        SupportedAccountType.WorkOrSchool,
        "Tenant A");

    public TenantContext GetRequiredContext() => Context;
}
