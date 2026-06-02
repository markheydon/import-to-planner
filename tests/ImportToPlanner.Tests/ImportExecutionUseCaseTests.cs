using ImportToPlanner.Application.Common.Abstractions;
using ImportToPlanner.Application.Common.Exceptions;
using ImportToPlanner.Application.Common.Models;
using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Application.CsvImport.Models;
using ImportToPlanner.Application.ImportExecution.Abstractions;
using ImportToPlanner.Application.ImportExecution.Models;
using ImportToPlanner.Application.ImportExecution.Services;
using ImportToPlanner.Application.ImportPlanning.Abstractions;
using ImportToPlanner.Application.ImportPlanning.Models;
using ImportToPlanner.Application.ImportPlanning.Services;
using ImportToPlanner.Domain;
using ImportToPlanner.Tests.TestDoubles;

namespace ImportToPlanner.Tests;

public sealed class ImportExecutionUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WhenPreviewHasValidationErrors_ThrowsInvalidOperationException()
    {
        var gateway = new PlannerGatewayStub();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        var useCase = new ImportExecutionUseCase(gateway);
        var output = new CaptureExecutionOutputBoundary();

        var planningRequest = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [new CsvTaskRow(2, "Task A", null, null, "Ops", null)]);

        var preview = new ImportPlanPreview
        {
            ContainerId = planningRequest.ContainerId,
            PlanId = planningRequest.PlanId,
            PlanName = planningRequest.PlanName,
            PlanAction = PlannedEntityAction.Reuse,
            HasValidationErrors = true,
            ValidationFindings = [new ImportValidationError(2, "Task", "Invalid")],
            RequestFingerprint = "req",
            PlannerStateFingerprint = "state",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            BucketActions = new Dictionary<string, PlannedEntityAction>(StringComparer.OrdinalIgnoreCase),
            TaskActions = [new ImportTaskPlanItem(2, "Task A", "Ops", null, PlannedEntityAction.Create)],
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.HandleAsync(new ImportExecutionRequest(planningRequest, preview), output, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithBoundaryDoubles_ProducesEquivalentOutcomeCounts()
    {
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [
                new CsvTaskRow(2, "Existing Task", null, 3, "Ops", "Goal A"),
                new CsvTaskRow(3, "New Task", null, 3, "Ops", "Goal B"),
            ]);

        var plannerGatewayStub = new PlannerGatewayStub();
        plannerGatewayStub.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        var plannerStubBucket = await plannerGatewayStub.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        await plannerGatewayStub.CreateTaskAsync("plan-alpha", plannerStubBucket.Id, "Existing Task", null, null, null, CancellationToken.None);

        var fakeGateway = new FakePlannerGateway();
        fakeGateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        var fakeBucket = await fakeGateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        await fakeGateway.CreateTaskAsync("plan-alpha", fakeBucket.Id, "Existing Task", null, null, null, CancellationToken.None);

        var inMemoryPlanning = CreatePlanningUseCase(plannerGatewayStub);
        var fakePlanning = CreatePlanningUseCase(fakeGateway);

        var inMemoryPlanningOutput = new CapturePlanningOutputBoundary();
        var fakePlanningOutput = new CapturePlanningOutputBoundary();

        await inMemoryPlanning.HandleAsync(request, inMemoryPlanningOutput, CancellationToken.None);
        await fakePlanning.HandleAsync(request, fakePlanningOutput, CancellationToken.None);

        var inMemoryExecution = new ImportExecutionUseCase(plannerGatewayStub);
        var fakeExecution = new ImportExecutionUseCase(fakeGateway);
        var inMemoryOutput = new CaptureExecutionOutputBoundary();
        var fakeOutput = new CaptureExecutionOutputBoundary();

        await inMemoryExecution.HandleAsync(
            new ImportExecutionRequest(request, inMemoryPlanningOutput.Response!),
            inMemoryOutput,
            CancellationToken.None);
        await fakeExecution.HandleAsync(
            new ImportExecutionRequest(request, fakePlanningOutput.Response!),
            fakeOutput,
            CancellationToken.None);

        Assert.Equal(inMemoryOutput.Response!.CreatedItems.Count, fakeOutput.Response!.CreatedItems.Count);
        Assert.Equal(inMemoryOutput.Response.ReusedOrSkippedItems.Count, fakeOutput.Response.ReusedOrSkippedItems.Count);
        Assert.Equal(inMemoryOutput.Response.FailureItems.Count, fakeOutput.Response.FailureItems.Count);
    }

    [Fact]
    public async Task HandleAsync_WhenPlanLookupFails_ReturnsStructuredFailureResult()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var useCase = new ImportExecutionUseCase(gateway);
        var output = new CaptureExecutionOutputBoundary();

        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [new CsvTaskRow(2, "Task A", null, 3, "Ops", null)]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        gateway.GetPlanByIdException = new PlannerOperationException(new PlannerOperationFailure(
            PlannerFailureCategory.Unavailable,
            PlannerFailureTarget.Workflow,
            null,
            "Planner provider is unavailable.",
            true,
            "Unavailable"));

        await useCase.HandleAsync(new ImportExecutionRequest(request, planningOutput.Response!), output, CancellationToken.None);

        Assert.NotNull(output.Response);
        Assert.Equal("plan-alpha", output.Response!.PlanId);
        Assert.Empty(output.Response.CreatedItems);
        Assert.Empty(output.Response.ReusedOrSkippedItems);
        var failure = Assert.Single(output.Response.FailureItems);
        Assert.Equal(PlannerFailureTarget.Workflow, failure.Target);
        Assert.True(output.Response.OutcomeSummary.IsFullFailure);
    }

    private sealed class CapturePlanningOutputBoundary : IImportPlanningOutputBoundary
    {
        public ImportPlanPreview? Response { get; private set; }

        public Task PresentAsync(ImportPlanPreview response, CancellationToken cancellationToken)
        {
            Response = response;
            return Task.CompletedTask;
        }
    }

    private static ImportPlanningUseCase CreatePlanningUseCase(IPlannerGateway plannerGateway)
    {
        return new ImportPlanningUseCase(
            plannerGateway,
            new CurrentTenantContextAccessorStub(),
            new TenantOperationalMetadataStoreStub(),
            new ConsentResolutionDefaults(
                ["Tasks.ReadWrite"],
                new Uri("https://example.test/admin-consent")));
    }

    private sealed class CaptureExecutionOutputBoundary : IImportExecutionOutputBoundary
    {
        public ImportExecutionResult? Response { get; private set; }

        public Task PresentAsync(ImportExecutionResult response, CancellationToken cancellationToken)
        {
            Response = response;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlannerGateway : IPlannerGateway
    {
        private readonly List<PlannerPlan> plans = [];
        private readonly Dictionary<string, List<PlannerBucket>> buckets = new();
        private readonly Dictionary<string, List<PlannerTaskSnapshot>> tasks = new();

        public Exception? GetPlanByIdException { get; set; }

        public Exception? GetBucketsException { get; set; }

        public Task<IReadOnlyList<PlannerContainer>> GetAvailableContainersAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PlannerContainer>>([]);

        public Task<PlannerPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken)
        {
            if (GetPlanByIdException is not null)
            {
                return Task.FromException<PlannerPlan?>(GetPlanByIdException);
            }

            return Task.FromResult<PlannerPlan?>(plans.FirstOrDefault(plan => string.Equals(plan.Id, planId, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyList<PlannerPlan>> GetPlansAsync(string containerId, ContainerType containerType, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PlannerPlan>>(plans.Where(plan => string.Equals(plan.ContainerId, containerId, StringComparison.OrdinalIgnoreCase)).ToArray());

        public Task<IReadOnlyList<PlannerBucket>> GetBucketsAsync(string planId, CancellationToken cancellationToken)
        {
            if (GetBucketsException is not null)
            {
                return Task.FromException<IReadOnlyList<PlannerBucket>>(GetBucketsException);
            }

            return Task.FromResult<IReadOnlyList<PlannerBucket>>(buckets.GetValueOrDefault(planId, []));
        }

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

        public Task<PlannerTaskSnapshot> CreateTaskAsync(string planId, string bucketId, string taskName, string? description, int? priority, string? goal, CancellationToken cancellationToken)
        {
            if (!tasks.TryGetValue(planId, out var planTasks))
            {
                planTasks = [];
                tasks[planId] = planTasks;
            }

            var existing = planTasks.FirstOrDefault(task => string.Equals(task.Title, taskName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                return Task.FromResult(existing);
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
