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
    /// Gets a plan by identifier.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching plan if found; otherwise <see langword="null"/>.</returns>
    Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets plans available in the specified container.
    /// </summary>
    /// <param name="containerId">The container identifier.</param>
    /// <param name="containerType">The container type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plans in the selected container.</returns>
    Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken);

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
