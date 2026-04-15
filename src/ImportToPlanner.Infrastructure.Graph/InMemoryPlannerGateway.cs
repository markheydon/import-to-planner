using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Infrastructure.Graph;

/// <summary>
/// Provides an in-memory Planner gateway used during early implementation.
/// </summary>
public sealed class InMemoryPlannerGateway : IPlannerGateway
{
    private readonly Dictionary<string, List<PlannerPlan>> plansByContainer = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PlannerBucket>> bucketsByPlan = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasksByPlan = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<PlannerContainer> Containers =
    [
        new PlannerContainer("group-alpha", "Alpha Team", ContainerType.Group),
        new PlannerContainer("group-bravo", "Bravo Team", ContainerType.Group),
        new PlannerContainer("user-me", "My Personal Plans", ContainerType.User),
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPlannerGateway"/> class.
    /// </summary>
    public InMemoryPlannerGateway()
    {
        SeedPlan("group-alpha", ContainerType.Group, "Alpha Team Plan");
        SeedPlan("group-bravo", ContainerType.Group, "Bravo Team Plan");
        SeedPlan("user-me", ContainerType.User, "My Tasks");
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Containers);
    }

    /// <inheritdoc/>
    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        var plan = plansByContainer.Values
            .SelectMany(existingPlans => existingPlans)
            .FirstOrDefault(existing => string.Equals(existing.Id, planId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<PlannerPlan?>(plan);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(containerId, nameof(containerId));

        if (!plansByContainer.TryGetValue(containerId, out var plans))
        {
            return Task.FromResult<IReadOnlyList<PlannerPlan>>([]);
        }

        return Task.FromResult<IReadOnlyList<PlannerPlan>>(plans);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        if (!bucketsByPlan.TryGetValue(planId, out var buckets))
        {
            return Task.FromResult<IReadOnlyList<PlannerBucket>>([]);
        }

        return Task.FromResult<IReadOnlyList<PlannerBucket>>(buckets);
    }

    /// <inheritdoc/>
    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));
        ValidateRequired(bucketName, nameof(bucketName));

        if (!bucketsByPlan.TryGetValue(planId, out var buckets))
        {
            buckets = [];
            bucketsByPlan[planId] = buckets;
        }

        var existing = buckets.FirstOrDefault(bucket => string.Equals(bucket.Name, bucketName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var bucket = new PlannerBucket(Guid.NewGuid().ToString("N"), bucketName, planId);
        buckets.Add(bucket);
        return Task.FromResult(bucket);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        if (!tasksByPlan.TryGetValue(planId, out var tasks))
        {
            return Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>([]);
        }

        return Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>(tasks);
    }

    /// <inheritdoc/>
    public Task<PlannerTaskSnapshot> CreateTaskAsync(
        string planId,
        string bucketId,
        string taskName,
        string? description,
        int? priority,
        string? goal,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));
        ValidateRequired(bucketId, nameof(bucketId));
        ValidateRequired(taskName, nameof(taskName));

        if (!tasksByPlan.TryGetValue(planId, out var tasks))
        {
            tasks = [];
            tasksByPlan[planId] = tasks;
        }

        var existing = tasks.FirstOrDefault(task => string.Equals(task.Title, taskName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var task = new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId);
        tasks.Add(task);
        return Task.FromResult(task);
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }
    }

    private void SeedPlan(string containerId, ContainerType containerType, string planName)
    {
        if (!plansByContainer.TryGetValue(containerId, out var plans))
        {
            plans = [];
            plansByContainer[containerId] = plans;
        }

        var created = new PlannerPlan(Guid.NewGuid().ToString("N"), planName, containerId, containerType);
        plans.Add(created);
        bucketsByPlan.TryAdd(created.Id, []);
        tasksByPlan.TryAdd(created.Id, []);
    }
}
