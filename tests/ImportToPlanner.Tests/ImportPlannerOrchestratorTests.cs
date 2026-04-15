using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Tests;

public sealed class ImportPlannerOrchestratorTests
{
    [Fact]
    public async Task BuildPreviewAsync_WithDuplicateRows_SkipsSecondDuplicate()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var orchestrator = new ImportPlannerOrchestrator(gateway);

        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [
                new CsvTaskRow(2, "Task A", "One", 3, "Ops", "Goal 1"),
                new CsvTaskRow(3, "Task A", "Two", 3, "Ops", "Goal 1"),
            ]);

        // Act
        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("plan-a", preview.PlanId);
        Assert.Equal(PlannedEntityAction.Reuse, preview.PlanAction);
        Assert.Equal(2, preview.TaskActions.Count);
        Assert.Equal(PlannedEntityAction.Create, preview.TaskActions[0].Action);
        Assert.Equal(PlannedEntityAction.Skip, preview.TaskActions[1].Action);
        Assert.Contains("Duplicate", preview.TaskActions[1].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTaskExists_SkipsTaskCreationAndKeepsGoalManualActions()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var bucket = await gateway.CreateBucketAsync("plan-a", "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync("plan-a", bucket.Id, "Task A", null, null, "Goal A", CancellationToken.None);

        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest("group-a", ContainerType.Group, "plan-a", "Plan A", [new CsvTaskRow(2, "Task A", null, null, "Ops", "Goal A")]);

        // Act
        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);
        var result = await orchestrator.ExecuteAsync(request, preview, CancellationToken.None);

        // Assert
        Assert.Contains(preview.TaskActions, task => task.Action == PlannedEntityAction.Skip);
        Assert.Empty(result.Errors);
        Assert.Contains(result.ManualActions, action =>
            action.ActionType == "EnsureGoalExists" &&
            action.GoalName == "Goal A");
        Assert.Contains(result.ManualActions, action =>
            action.ActionType == "LinkTaskToGoal" &&
            action.GoalName == "Goal A" &&
            action.TaskName == "Task A");
        Assert.Contains(result.ReusedOrSkipped, line => line.Contains("Task A", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildPreviewAsync_WithMissingBucket_UsesGeneralBucket()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);

        // Act
        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Assert
        Assert.Contains(preview.BucketActions, bucket =>
            string.Equals(bucket.Key, "General", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.TaskActions, task =>
            string.Equals(task.Bucket, "General", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildPreviewAsync_WithOnlySkippedTasks_DoesNotPlanBucketCreation()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var existingBucket = await gateway.CreateBucketAsync("plan-a", "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync("plan-a", existingBucket.Id, "Task A", null, null, null, CancellationToken.None);

        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, null, "New Bucket", null)]);

        // Act
        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Assert
        Assert.Empty(preview.BucketActions);
        Assert.Contains(preview.TaskActions, task => task.Action == PlannedEntityAction.Skip);
    }

    [Fact]
    public async Task ExecuteAsync_WithGoal_AddsManualGoalActions()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, 3, "Ops", "Goal A")]);

        // Act
        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);
        var result = await orchestrator.ExecuteAsync(request, preview, CancellationToken.None);

        // Assert
        Assert.Contains(result.ManualActions, action =>
            action.ActionType == "EnsureGoalExists" &&
            action.GoalName == "Goal A");
        Assert.Contains(result.ManualActions, action =>
            action.ActionType == "LinkTaskToGoal" &&
            action.GoalName == "Goal A" &&
            action.TaskName == "Task A");
    }

    private sealed class FakePlannerGateway : IPlannerGateway
    {
        private readonly List<PlannerPlan> plans = [];
        private readonly Dictionary<string, List<PlannerBucket>> buckets = new();
        private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasks = new();

        public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PlannerContainer>>([new PlannerContainer("group-a", "Group A", ContainerType.Group)]);
        }

        public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        {
            var plan = plans.FirstOrDefault(existing =>
                string.Equals(existing.Id, planId, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult<PlannerPlan?>(plan);
        }

        public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
        {
            var result = plans.Where(existing =>
                    string.Equals(existing.ContainerId, containerId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult<IReadOnlyList<PlannerPlan>>(result);
        }

        public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PlannerBucket>>(buckets.GetValueOrDefault(planId, []));
        }

        public Task<PlannerBucket> CreateBucketAsync(string planId, string bucketName, CancellationToken cancellationToken)
        {
            if (!buckets.TryGetValue(planId, out var planBuckets))
            {
                planBuckets = [];
                buckets[planId] = planBuckets;
            }

            var existing = planBuckets.FirstOrDefault(bucket =>
                string.Equals(bucket.Name, bucketName, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                return Task.FromResult(existing);
            }

            var created = new PlannerBucket(Guid.NewGuid().ToString("N"), bucketName, planId);
            planBuckets.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyList<PlannerTaskSnapshot>> GetTasksAsync(string planId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>(tasks.GetValueOrDefault(planId, []));
        }

        public Task<PlannerTaskSnapshot> CreateTaskAsync(
            string planId,
            string bucketId,
            string taskName,
            string? description,
            int? priority,
            string? goal,
            CancellationToken cancellationToken)
        {
            if (!tasks.TryGetValue(planId, out var planTasks))
            {
                planTasks = [];
                tasks[planId] = planTasks;
            }

            var existing = planTasks.FirstOrDefault(task =>
                string.Equals(task.Title, taskName, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                return Task.FromResult(existing);
            }

            var created = new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId);
            planTasks.Add(created);
            return Task.FromResult(created);
        }

        public void AddPlan(string planId, string containerId, ContainerType containerType, string planName)
        {
            if (plans.Any(existing => string.Equals(existing.Id, planId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            plans.Add(new PlannerPlan(planId, planName, containerId, containerType));
            buckets.TryAdd(planId, []);
            tasks.TryAdd(planId, []);
        }
    }
}
