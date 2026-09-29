using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Routes planner operations to synthetic fixtures while demonstration mode is active.
/// </summary>
public sealed class DemoAwarePlannerGateway(
    IPlannerGateway liveGateway,
    IDemoModeSession demoModeSession) : IPlannerGateway
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.Containers)
            : liveGateway.GetAvailableContainersAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.Plans.FirstOrDefault(plan => string.Equals(plan.Id, planId, StringComparison.OrdinalIgnoreCase)))
            : liveGateway.GetPlanByIdAsync(planId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.Plans)
            : liveGateway.GetPlansAsync(containerId, containerType, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.Buckets)
            : liveGateway.GetBucketsAsync(planId, cancellationToken);

    /// <inheritdoc />
    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(new PlannerBucket($"demo-bucket-{bucketName}", bucketName, planId))
            : liveGateway.CreateBucketAsync(planId, bucketName, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.ExistingTasks)
            : liveGateway.GetTasksAsync(planId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PlanMember>> GetPlanMembersAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
        => demoModeSession.IsActive
            ? Task.FromResult(DemoDataset.PlanMembers)
            : liveGateway.GetPlanMembersAsync(containerId, containerType, cancellationToken);

    /// <inheritdoc />
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
        if (!demoModeSession.IsActive)
        {
            return liveGateway.CreateTaskAsync(
                planId,
                bucketId,
                taskName,
                description,
                priority,
                goal,
                dueDate,
                assigneeUserIds,
                cancellationToken);
        }

        var snapshot = new PlannerTaskSnapshot($"demo-task-{Guid.NewGuid():N}", taskName, planId);
        return Task.FromResult(new CreatedPlannerTask(snapshot, assigneeUserIds));
    }
}
