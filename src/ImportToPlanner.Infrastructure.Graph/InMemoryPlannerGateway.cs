using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Infrastructure.Graph;

/// <summary>
/// Provides an in-memory Planner gateway used during early implementation.
/// </summary>
public sealed class InMemoryPlannerGateway : IPlannerGateway
{
    private readonly Dictionary<string, List<PlannerPlan>> plansByGroup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PlannerBucket>> bucketsByPlan = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasksByPlan = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> goalsByPlan = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<PlannerGroup> Groups =
    [
        new PlannerGroup("group-alpha", "Alpha Team"),
        new PlannerGroup("group-bravo", "Bravo Team"),
    ];

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlannerGroup>> GetAvailableGroupsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Groups);
    }

    /// <inheritdoc/>
    public Task<PlannerPlan?> FindPlanByNameAsync(string groupId, string planName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(groupId, nameof(groupId));
        ValidateRequired(planName, nameof(planName));

        if (!plansByGroup.TryGetValue(groupId, out var plans))
        {
            return Task.FromResult<PlannerPlan?>(null);
        }

        var plan = plans.FirstOrDefault(existing => string.Equals(existing.Title, planName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<PlannerPlan?>(plan);
    }

    /// <inheritdoc/>
    public Task<PlannerPlan> CreatePlanAsync(string groupId, string planName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(groupId, nameof(groupId));
        ValidateRequired(planName, nameof(planName));

        if (!plansByGroup.TryGetValue(groupId, out var plans))
        {
            plans = [];
            plansByGroup[groupId] = plans;
        }

        var existing = plans.FirstOrDefault(plan => string.Equals(plan.Title, planName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var plan = new PlannerPlan(Guid.NewGuid().ToString("N"), planName, groupId);
        plans.Add(plan);

        bucketsByPlan.TryAdd(plan.Id, []);
        tasksByPlan.TryAdd(plan.Id, []);
        goalsByPlan.TryAdd(plan.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(plan);
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
    public Task<IReadOnlySet<string>> GetGoalsAsync(string planId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));

        if (!goalsByPlan.TryGetValue(planId, out var goals))
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlySet<string>>(goals);
    }

    /// <inheritdoc/>
    public Task<IReadOnlySet<string>> EnsureGoalsAsync(
        string planId,
        IReadOnlyCollection<string> goals,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequired(planId, nameof(planId));
        ArgumentNullException.ThrowIfNull(goals);

        if (!goalsByPlan.TryGetValue(planId, out var existing))
        {
            existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            goalsByPlan[planId] = existing;
        }

        var created = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var goal in goals)
        {
            ValidateRequired(goal, nameof(goals));

            if (existing.Add(goal))
            {
                created.Add(goal);
            }
        }

        return Task.FromResult<IReadOnlySet<string>>(created);
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
}
