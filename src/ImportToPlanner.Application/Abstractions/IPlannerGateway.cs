using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Provides Planner data access operations used by import use cases.
/// </summary>
public interface IPlannerGateway
{
    /// <summary>
    /// Gets containers that can be used for Planner plans by the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of available containers.</returns>
    Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a plan by name within the specified container.
    /// </summary>
    /// <param name="containerId">The container identifier.</param>
    /// <param name="planName">The plan title.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plan if found; otherwise <see langword="null"/>.</returns>
    Task<PlannerPlan?> FindPlanByNameAsync(string containerId, string planName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a plan in the specified container.
    /// </summary>
    /// <param name="containerId">The container identifier.</param>
    /// <param name="planName">The plan title.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created plan.</returns>
    Task<PlannerPlan> CreatePlanAsync(string containerId, string planName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all buckets for a plan.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The buckets for the plan.</returns>
    Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a bucket in the specified plan.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="bucketName">The bucket name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created bucket.</returns>
    Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all tasks in the specified plan.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task snapshots for idempotency checks.</returns>
    Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a task in the specified plan.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="bucketId">The destination bucket identifier.</param>
    /// <param name="taskName">The task name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="priority">The optional priority.</param>
    /// <param name="goal">The optional goal value from CSV.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created task snapshot.</returns>
    Task<PlannerTaskSnapshot> CreateTaskAsync(
        string planId,
        string bucketId,
        string taskName,
        string? description,
        int? priority,
        string? goal,
        CancellationToken cancellationToken);
}
