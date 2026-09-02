using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
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
        var useCase = new ImportExecutionUseCase(gateway, new NoOpImportTaskCreationQuota());
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

        var inMemoryExecution = new ImportExecutionUseCase(plannerGatewayStub, new NoOpImportTaskCreationQuota());
        var fakeExecution = new ImportExecutionUseCase(fakeGateway, new NoOpImportTaskCreationQuota());
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
        var useCase = new ImportExecutionUseCase(gateway, new NoOpImportTaskCreationQuota());
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

    [Fact]
    public async Task HandleAsync_WithMetering_RecordsOneCreditPerCreatedTask()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = BuildSingleCreateRequest();
        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 2));
        quota.RecordResults.Enqueue(new TaskCreationQuotaRecordResult(true, RemainingCredits: 1));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                planningOutput.Response!,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(1, quota.BeforeCreateCallCount);
        Assert.Equal(1, quota.RecordCallCount);
        Assert.Equal(1, output.Response!.CreditsUsed);
    }

    [Fact]
    public async Task HandleAsync_WithMetering_StopsFurtherCreatesWhenQuotaExhausted()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = BuildSingleCreateRequest();
        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Exhausted, "credits.exhausted", 0));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                planningOutput.Response!,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Empty(output.Response!.CreatedItems);
        Assert.Contains(output.Response.FailureItems, failure => failure.DiagnosticCode == "credits.exhausted");
    }

    [Fact]
    public async Task HandleAsync_WithMetering_DoesNotCallQuotaForBucketReuseOrTaskSkip()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        var opsBucket = await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        await gateway.CreateTaskAsync("plan-alpha", opsBucket.Id, "Existing Task", null, null, null, CancellationToken.None);

        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [
                new CsvTaskRow(2, "Existing Task", null, 3, "Ops", null),
                new CsvTaskRow(3, "Skipped Task", null, 3, "Ops", null),
                new CsvTaskRow(4, "Brand New", null, 3, "NewBucket", null),
            ]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var preview = planningOutput.Response! with
        {
            BucketActions = new Dictionary<string, PlannedEntityAction>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ops"] = PlannedEntityAction.Reuse,
                ["NewBucket"] = PlannedEntityAction.Create,
            },
            TaskActions =
            [
                new ImportTaskPlanItem(2, "Existing Task", "Ops", null, PlannedEntityAction.Reuse),
                new ImportTaskPlanItem(3, "Skipped Task", "Ops", null, PlannedEntityAction.Skip),
                new ImportTaskPlanItem(4, "Brand New", "NewBucket", null, PlannedEntityAction.Create),
            ],
        };

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 5));
        quota.RecordResults.Enqueue(new TaskCreationQuotaRecordResult(true, RemainingCredits: 4));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                preview,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(1, quota.BeforeCreateCallCount);
        Assert.Equal(1, quota.RecordCallCount);
        Assert.Equal(1, output.Response!.CreditsUsed);
        Assert.Equal(2, output.Response.CreatedItems.Count);
        Assert.Contains(output.Response.CreatedItems, item => item.Target == PlannerFailureTarget.Task);
        Assert.Contains(output.Response.CreatedItems, item => item.Target == PlannerFailureTarget.Bucket);
        Assert.Equal(4, output.Response.ReusedOrSkippedItems.Count);
    }

    [Fact]
    public async Task HandleAsync_WithMetering_KeepsPlannerTaskWhenUsageRecordFails()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = BuildSingleCreateRequest();
        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 1));
        quota.RecordResults.Enqueue(new TaskCreationQuotaRecordResult(false, "credits.usage_record_failed"));
        quota.RecordResults.Enqueue(new TaskCreationQuotaRecordResult(false, "credits.usage_record_failed"));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                planningOutput.Response!,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Single(output.Response!.CreatedItems);
        Assert.Contains(output.Response.FailureItems, failure => failure.DiagnosticCode == "credits.usage_record_failed");
    }

    [Fact]
    public async Task HandleAsync_WithMetering_StopsFurtherCreatesAfterPartialCreatesWhenQuotaExhausted()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [
                new CsvTaskRow(2, "Task A", null, 3, "Ops", null),
                new CsvTaskRow(3, "Task B", null, 3, "Ops", null),
            ]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var preview = planningOutput.Response! with
        {
            TaskActions =
            [
                new ImportTaskPlanItem(2, "Task A", "Ops", null, PlannedEntityAction.Create),
                new ImportTaskPlanItem(3, "Task B", "Ops", null, PlannedEntityAction.Create),
            ],
        };

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 1));
        quota.RecordResults.Enqueue(new TaskCreationQuotaRecordResult(true, RemainingCredits: 0));
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Exhausted, "credits.exhausted", 0));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                preview,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Single(output.Response!.CreatedItems);
        Assert.Equal(1, output.Response.CreditsUsed);
        Assert.Equal(0, output.Response.RemainingCredits);
        Assert.Contains(output.Response.FailureItems, failure =>
            failure.DiagnosticCode == "credits.exhausted"
            && string.Equals(failure.Reference, "Task B", StringComparison.Ordinal));
        Assert.True(output.Response.RemainingCredits >= 0);
    }

    [Fact]
    public async Task HandleAsync_WithMetering_StopsWhenQuotaUnavailableAtBeforeCreate()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = BuildSingleCreateRequest();
        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(
            TaskCreationQuotaStatus.Unavailable,
            "credits.ledger_unavailable"));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                planningOutput.Response!,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Empty(output.Response!.CreatedItems);
        Assert.Equal(0, quota.RecordCallCount);
        Assert.Contains(output.Response.FailureItems, failure => failure.DiagnosticCode == "credits.ledger_unavailable");
    }

    [Fact]
    public async Task HandleAsync_WithMetering_DoesNotRecordCreditWhenPlannerCreateFails()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        gateway.CreateTaskException = new PlannerOperationException(new PlannerOperationFailure(
            PlannerFailureCategory.Unavailable,
            PlannerFailureTarget.Task,
            "Task A",
            "Planner provider is unavailable.",
            true,
            "Unavailable"));

        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = BuildSingleCreateRequest();
        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 1));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                planningOutput.Response!,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(0, quota.RecordCallCount);
        Assert.Equal(0, output.Response!.CreditsUsed);
        Assert.Empty(output.Response.CreatedItems);
    }

    [Fact]
    public async Task HandleAsync_WithMetering_WhenNoTasksCreated_ReportsRemainingCreditsFromLedger()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [
                new CsvTaskRow(2, "Existing Task", null, 3, "Ops", null),
                new CsvTaskRow(3, "Another Existing", null, 3, "Ops", null),
            ]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var preview = planningOutput.Response! with
        {
            TaskActions =
            [
                new ImportTaskPlanItem(2, "Existing Task", "Ops", null, PlannedEntityAction.Skip),
                new ImportTaskPlanItem(3, "Another Existing", "Ops", null, PlannedEntityAction.Skip),
            ],
        };

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(TaskCreationQuotaStatus.Allow, RemainingCredits: 25));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                preview,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(1, quota.BeforeCreateCallCount);
        Assert.Equal(0, quota.RecordCallCount);
        Assert.Equal(0, output.Response!.CreditsUsed);
        Assert.Equal(25, output.Response.RemainingCredits);
    }

    [Fact]
    public async Task HandleAsync_WithMetering_WhenNoTasksCreatedAndLedgerUnavailable_ReportsWorkflowWarning()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [new CsvTaskRow(2, "Existing Task", null, 3, "Ops", null)]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var preview = planningOutput.Response! with
        {
            TaskActions =
            [
                new ImportTaskPlanItem(2, "Existing Task", "Ops", null, PlannedEntityAction.Skip),
            ],
        };

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(
            TaskCreationQuotaStatus.Unavailable,
            "credits.ledger_unavailable"));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                preview,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(1, quota.BeforeCreateCallCount);
        Assert.Equal(0, quota.RecordCallCount);
        Assert.Equal(0, output.Response!.CreditsUsed);
        Assert.Null(output.Response.RemainingCredits);
        Assert.Contains(output.Response.FailureItems, failure =>
            failure.Target == PlannerFailureTarget.Workflow
            && failure.DiagnosticCode == "credits.balance_report_unavailable");
        Assert.True(output.Response.OutcomeSummary.IsPartialSuccess);
    }

    [Fact]
    public async Task HandleAsync_WithMetering_WhenNoTasksCreatedAndCreditsExhausted_ReportsZeroRemainingCredits()
    {
        var gateway = new FakePlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);
        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [new CsvTaskRow(2, "Existing Task", null, 3, "Ops", null)]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var preview = planningOutput.Response! with
        {
            TaskActions =
            [
                new ImportTaskPlanItem(2, "Existing Task", "Ops", null, PlannedEntityAction.Skip),
            ],
        };

        var quota = new ConfigurableImportTaskCreationQuota();
        quota.BeforeCreateResults.Enqueue(new TaskCreationQuotaResult(
            TaskCreationQuotaStatus.Exhausted,
            "credits.exhausted",
            0));

        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                preview,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(1, quota.BeforeCreateCallCount);
        Assert.Equal(0, quota.RecordCallCount);
        Assert.Equal(0, output.Response!.CreditsUsed);
        Assert.Equal(0, output.Response.RemainingCredits);
        Assert.Empty(output.Response.FailureItems);
    }

    private static ImportPlanningRequest BuildSingleCreateRequest()
        => new(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [new CsvTaskRow(2, "Task A", null, 3, "Ops", null)]);

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

        public Exception? CreateTaskException { get; set; }

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
            if (CreateTaskException is not null)
            {
                return Task.FromException<PlannerTaskSnapshot>(CreateTaskException);
            }

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
