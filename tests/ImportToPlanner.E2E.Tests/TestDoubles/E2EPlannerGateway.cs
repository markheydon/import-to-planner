using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.E2E.Tests.TestDoubles;

internal sealed class E2EPlannerGateway : IPlannerGateway
{
    public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerContainer>>([]);

    public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<PlannerPlan?>(null);

    public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(
        string containerId,
        ContainerType containerType,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerPlan>>([]);

    public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerBucket>>([]);

    public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
        => Task.FromResult(new PlannerBucket("e2e-bucket-id", bucketName, planId));

    public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>([]);

    public Task<IReadOnlyList<PlanMember>> GetPlanMembersAsync(
        string containerId,
        ContainerType containerType,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlanMember>>([]);

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
        => Task.FromResult(new CreatedPlannerTask(
            new PlannerTaskSnapshot("e2e-task-id", taskName, planId),
            assigneeUserIds));
}
