using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Tests.TestDoubles;

public sealed class PlannerGatewayStub : IPlannerGateway
{
    private readonly List<PlannerPlan> plans = [];
    private readonly Dictionary<string, List<PlannerBucket>> buckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PlanMember> members = [];

    public Exception? GetPlanMembersException { get; set; }

    public IReadOnlyList<string>? LastCreateAssigneeIds { get; private set; }

    public int GetPlanMembersCallCount { get; private set; }

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

    public Task<IReadOnlyList<PlanMember>> GetPlanMembersAsync(
        string containerId,
        ContainerType containerType,
        CancellationToken cancellationToken)
    {
        GetPlanMembersCallCount++;

        if (GetPlanMembersException is not null)
        {
            return Task.FromException<IReadOnlyList<PlanMember>>(GetPlanMembersException);
        }

        return Task.FromResult<IReadOnlyList<PlanMember>>(members);
    }

    public Task<CreatedPlannerTask> CreateTaskAsync(
        string planId,
        string bucketId,
        string taskName,
        string? description,
        int? priority,
        string? goal,
        DateOnly? dueDate,
        IReadOnlyList<string> assigneeUserIds,
        CancellationToken cancellationToken)
    {
        LastCreateAssigneeIds = assigneeUserIds;

        if (!tasks.TryGetValue(planId, out var planTasks))
        {
            planTasks = [];
            tasks[planId] = planTasks;
        }

        var existing = planTasks.FirstOrDefault(task => string.Equals(task.Title, taskName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(new CreatedPlannerTask(existing, assigneeUserIds));
        }

        var task = new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId);
        planTasks.Add(task);
        return Task.FromResult(new CreatedPlannerTask(task, assigneeUserIds));
    }

    public void AddPlan(string planId, string containerId, ContainerType containerType, string planName)
    {
        plans.Add(new PlannerPlan(planId, planName, containerId, containerType));
        buckets.TryAdd(planId, []);
        tasks.TryAdd(planId, []);
    }

    public void AddMember(PlanMember member)
    {
        members.Add(member);
    }

    public void SeedDefaultMembers()
    {
        members.Clear();
        members.Add(new PlanMember("user-1", "a@contoso.com", "a@contoso.com"));
        members.Add(new PlanMember("guest-1", "guest@external.com", "guest_external.com#EXT#@contoso.com"));
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
