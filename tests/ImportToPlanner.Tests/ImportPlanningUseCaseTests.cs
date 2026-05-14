using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Tests;

public sealed class ImportPlanningUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WithDuplicateRows_SkipsSecondDuplicate()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = new ImportPlanningUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();

        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [
                new CsvTaskRow(2, "Task A", "One", 3, "Ops", "Goal 1"),
                new CsvTaskRow(3, "Task A", "Two", 3, "Ops", "Goal 1"),
            ]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.NotNull(output.Response);
        var preview = output.Response!;
        Assert.Equal(2, preview.TaskActions.Count);
        Assert.Equal(PlannedEntityAction.Create, preview.TaskActions[0].Action);
        Assert.Equal(PlannedEntityAction.Skip, preview.TaskActions[1].Action);
    }

    [Fact]
    public async Task HandleAsync_WithMissingBucket_UsesGeneralBucket()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var useCase = new ImportPlanningUseCase(gateway);
        var output = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);

        await useCase.HandleAsync(request, output, CancellationToken.None);

        Assert.NotNull(output.Response);
        var preview = output.Response!;
        Assert.Contains(preview.BucketActions, bucket =>
            string.Equals(bucket.Key, "General", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.TaskActions, task =>
            string.Equals(task.Bucket, "General", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CapturePlanningOutputBoundary : IImportPlanningOutputBoundary
    {
        public ImportPlanPreview? Response { get; private set; }

        public Task PresentAsync(ImportPlanPreview response, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Response = response;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlannerGateway : IPlannerGateway
    {
        private readonly List<PlannerPlan> plans = [];
        private readonly Dictionary<string, List<PlannerBucket>> buckets = new();
        private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasks = new();

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
}
