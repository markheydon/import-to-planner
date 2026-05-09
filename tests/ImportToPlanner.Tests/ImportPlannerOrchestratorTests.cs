using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Domain;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Tests.TestData;
using System.Diagnostics;

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
    public async Task BuildPreviewAsync_WithExistingTaskNameOnlyMatch_SkipsRowWithAlreadyExistsReason()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan(ImportScenarioConstants.PlanId, ImportScenarioConstants.ContainerId, ContainerType.Group, ImportScenarioConstants.PlanName);
        var bucket = await gateway.CreateBucketAsync(ImportScenarioConstants.PlanId, "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync(ImportScenarioConstants.PlanId, bucket.Id, ImportScenarioConstants.ExistingTaskName, null, null, "Goal A", CancellationToken.None);

        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            ImportScenarioConstants.ContainerId,
            ContainerType.Group,
            ImportScenarioConstants.PlanId,
            ImportScenarioConstants.PlanName,
            [new CsvTaskRow(2, ImportScenarioConstants.ExistingTaskName, "Different description", null, "Other Bucket", "Goal A")]);

        // Act
        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Assert
        var skippedTask = Assert.Single(preview.TaskActions);
        Assert.Equal(PlannedEntityAction.Skip, skippedTask.Action);
        Assert.Equal("already exists", skippedTask.Reason);
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
    public async Task BuildPreviewAsync_AddsFreshnessMetadataForStaleVerification()
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

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(preview.RequestFingerprint));
        Assert.False(string.IsNullOrWhiteSpace(preview.PlannerStateFingerprint));
        Assert.True(preview.GeneratedAtUtc > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreviewHasValidationErrors_ThrowsInvalidOperationException()
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
            [new CsvTaskRow(2, "Task A", null, null, "Ops", "Goal A")]);

        var preview = new ImportPlanPreview
        {
            ContainerId = "group-a",
            PlanId = "plan-a",
            PlanName = "Plan A",
            PlanAction = PlannedEntityAction.Reuse,
            HasValidationErrors = true,
            RequestFingerprint = "req",
            PlannerStateFingerprint = "state",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            BucketActions = new Dictionary<string, PlannedEntityAction>(StringComparer.OrdinalIgnoreCase),
            TaskActions = [new ImportTaskPlanItem(2, "Task A", "Ops", ["Goal A"], PlannedEntityAction.Create)],
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExecuteAsync(request, preview, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreviewStateIsStale_ThrowsInvalidOperationException()
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

        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);
        gateway.SimulateStaleStateOnTaskRead = true;

        // Act + Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExecuteAsync(request, preview, CancellationToken.None));

        Assert.Contains("fresh preview", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneRowFails_ContinuesAndReturnsPartialResultSummary()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.FailTaskNames.Add("Task 2");
        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [
                new CsvTaskRow(2, "Task 1", null, 3, "Ops", null),
                new CsvTaskRow(3, "Task 2", null, 3, "Ops", null),
                new CsvTaskRow(4, "Task 3", null, 3, "Ops", null),
            ]);

        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Act
        var result = await orchestrator.ExecuteAsync(request, preview, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Created.Count(line => line.StartsWith("Task:", StringComparison.Ordinal)));
        Assert.Single(result.Errors);
        Assert.NotNull(result.OutcomeSummary);
        Assert.True(result.OutcomeSummary.IsPartialSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeModeParity_InMemoryAndFakeGraphReturnEquivalentOutcomes()
    {
        // Arrange
        var request = new ImportRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [
                new CsvTaskRow(2, "Existing Task", null, 3, "Ops", "Goal A"),
                new CsvTaskRow(3, "New Task", null, 3, "Ops", "Goal B"),
            ]);

        var inMemoryGateway = new InMemoryPlannerGateway();
        var inMemoryPlan = (await inMemoryGateway.GetPlansAsync("group-alpha", ContainerType.Group, CancellationToken.None)).Single();
        var inMemoryBucket = await inMemoryGateway.CreateBucketAsync(inMemoryPlan.Id, "Ops", CancellationToken.None);
        await inMemoryGateway.CreateTaskAsync(inMemoryPlan.Id, inMemoryBucket.Id, "Existing Task", null, null, null, CancellationToken.None);

        var fakeGateway = new FakePlannerGateway();
        fakeGateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        var fakeBucket = await fakeGateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        await fakeGateway.CreateTaskAsync("plan-alpha", fakeBucket.Id, "Existing Task", null, null, null, CancellationToken.None);

        var inMemoryOrchestrator = new ImportPlannerOrchestrator(inMemoryGateway);
        var fakeOrchestrator = new ImportPlannerOrchestrator(fakeGateway);

        var inMemoryRequest = request with { PlanId = inMemoryPlan.Id, PlanName = inMemoryPlan.Title };
        var fakeRequest = request;

        // Act
        var inMemoryPreview = await inMemoryOrchestrator.BuildPreviewAsync(inMemoryRequest, CancellationToken.None);
        var fakePreview = await fakeOrchestrator.BuildPreviewAsync(fakeRequest, CancellationToken.None);

        var inMemoryResult = await inMemoryOrchestrator.ExecuteAsync(inMemoryRequest, inMemoryPreview, CancellationToken.None);
        var fakeResult = await fakeOrchestrator.ExecuteAsync(fakeRequest, fakePreview, CancellationToken.None);

        // Assert
        Assert.Equal(inMemoryPreview.TaskActions.Count(action => action.Action == PlannedEntityAction.Skip), fakePreview.TaskActions.Count(action => action.Action == PlannedEntityAction.Skip));
        Assert.Equal(inMemoryPreview.TaskActions.Count(action => action.Action == PlannedEntityAction.Create), fakePreview.TaskActions.Count(action => action.Action == PlannedEntityAction.Create));
        Assert.Equal(inMemoryResult.Created.Count(line => line.StartsWith("Task:", StringComparison.Ordinal)), fakeResult.Created.Count(line => line.StartsWith("Task:", StringComparison.Ordinal)));
        Assert.Equal(inMemoryResult.Errors.Count, fakeResult.Errors.Count);
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

    [Fact]
    public async Task ExecuteAsync_WithExistingTaskGoalCaseDifferences_DeduplicatesManualLinkActions()
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
            [new CsvTaskRow(2, "Task A", null, null, "Ops", "Goal A")]);

        var basePreview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);
        var preview = basePreview with
        {
            TaskActions =
            [
                new ImportTaskPlanItem(2, "Task A", "Ops", ["Goal A"], PlannedEntityAction.Skip, "already exists"),
                new ImportTaskPlanItem(3, "task a", "Ops", ["goal a"], PlannedEntityAction.Skip, "already exists"),
            ],
        };

        // Act
        var result = await orchestrator.ExecuteAsync(request, preview, CancellationToken.None);

        // Assert
        Assert.Single(result.ManualActions, action =>
            action.ActionType == "EnsureGoalExists" &&
            string.Equals(action.GoalName, "Goal A", StringComparison.OrdinalIgnoreCase));
        Assert.Single(result.ManualActions, action =>
            action.ActionType == "LinkTaskToGoal" &&
            string.Equals(action.GoalName, "Goal A", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action.TaskName, "Task A", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_ComposesAggregateReportAcrossCreatedSkippedErrorsAndManualActions()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.FailTaskNames.Add("Fails");
        var existingBucket = await gateway.CreateBucketAsync("plan-a", "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync("plan-a", existingBucket.Id, "Existing", null, null, null, CancellationToken.None);

        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [
                new CsvTaskRow(2, "Existing", null, null, "Ops", "Goal A"),
                new CsvTaskRow(3, "Creates", null, null, "Ops", "Goal B"),
                new CsvTaskRow(4, "Fails", null, null, "Ops", null),
            ]);

        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Act
        var result = await orchestrator.ExecuteAsync(request, preview, CancellationToken.None);

        // Assert
        Assert.Contains(result.Created, line => line.Contains("Creates", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ReusedOrSkipped, line => line.Contains("Existing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, line => line.Contains("Fails", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(result.ManualActions);
        Assert.NotNull(result.OutcomeSummary);
        Assert.Equal(result.Created.Count, result.OutcomeSummary.CreatedCount);
        Assert.Equal(result.ReusedOrSkipped.Count, result.OutcomeSummary.ReusedOrSkippedCount);
        Assert.Equal(result.Errors.Count, result.OutcomeSummary.ErrorCount);
        Assert.Equal(result.ManualActions.Count, result.OutcomeSummary.ManualActionCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGatewayThrowsSensitiveError_MapsToUserSafeMessage()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        gateway.FailTaskNames.Add("Task A");
        gateway.FailureExceptionFactory = () => new PlannerPermissionException("tenant=abc123 secret=xyz789 cert=thumbprint");

        var orchestrator = new ImportPlannerOrchestrator(gateway);
        var request = new ImportRequest(
            "group-a",
            ContainerType.Group,
            "plan-a",
            "Plan A",
            [new CsvTaskRow(2, "Task A", null, 3, "Ops", null)]);

        var preview = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);

        // Act
        var result = await orchestrator.ExecuteAsync(request, preview, CancellationToken.None);

        // Assert
        Assert.Single(result.Errors);
        Assert.DoesNotContain("tenant", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cert", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildPreviewAsync_WithFiveHundredRows_MeetsP95UnderTenSeconds()
    {
        // Arrange
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-a", "group-a", ContainerType.Group, "Plan A");
        var orchestrator = new ImportPlannerOrchestrator(gateway);

        var rows = Enumerable.Range(1, 500)
            .Select(index => new CsvTaskRow(index + 1, $"Task {index}", "Desc", 3, "Ops", null))
            .ToArray();

        var request = new ImportRequest("group-a", ContainerType.Group, "plan-a", "Plan A", rows);
        var durations = new List<long>();

        // Act
        for (var run = 0; run < 20; run++)
        {
            var timer = Stopwatch.StartNew();
            _ = await orchestrator.BuildPreviewAsync(request, CancellationToken.None);
            timer.Stop();
            durations.Add(timer.ElapsedMilliseconds);
        }

        durations.Sort();
        var p95 = durations[(int)Math.Ceiling(durations.Count * 0.95) - 1];
        Console.WriteLine($"Preview p95 for 500 rows: {p95}ms");

        // Assert
        Assert.True(p95 < 10_000, $"Expected p95 under 10 seconds, actual p95={p95}ms.");
    }

    private sealed class FakePlannerGateway : IPlannerGateway
    {
        private readonly List<PlannerPlan> plans = [];
        private readonly Dictionary<string, List<PlannerBucket>> buckets = new();
        private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasks = new();

        public HashSet<string> FailTaskNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Func<Exception>? FailureExceptionFactory { get; set; }

        public bool SimulateStaleStateOnTaskRead { get; set; }

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
            if (SimulateStaleStateOnTaskRead)
            {
                var planTasks = tasks.GetValueOrDefault(planId, []);
                if (!planTasks.Any(task => string.Equals(task.Title, "__drift__", StringComparison.OrdinalIgnoreCase)))
                {
                    planTasks.Add(new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), "__drift__", planId));
                    tasks[planId] = planTasks;
                }

                SimulateStaleStateOnTaskRead = false;
            }

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
            if (FailTaskNames.Contains(taskName))
            {
                throw FailureExceptionFactory?.Invoke() ?? new InvalidOperationException("Simulated row failure.");
            }

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
