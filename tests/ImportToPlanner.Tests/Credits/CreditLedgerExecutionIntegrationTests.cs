using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Tests.TestDoubles;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class CreditLedgerExecutionIntegrationTests
{
    [Fact]
    public async Task HandleAsync_WithRealCreditQuota_RecordsUsageMatchingLedgerDerivedBalance()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store);
        var gateway = new ExecutionPlannerGateway();
        gateway.AddPlan("plan-alpha", "group-alpha", ContainerType.Group, "Alpha Team Plan");
        await gateway.CreateBucketAsync("plan-alpha", "Ops", CancellationToken.None);

        var planningUseCase = CreatePlanningUseCase(gateway);
        var planningOutput = new CapturePlanningOutputBoundary();
        var request = new ImportPlanningRequest(
            "group-alpha",
            ContainerType.Group,
            "plan-alpha",
            "Alpha Team Plan",
            [new CsvTaskRow(2, "Task A", null, 3, "Ops", null)]);

        await planningUseCase.HandleAsync(request, planningOutput, CancellationToken.None);

        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var useCase = new ImportExecutionUseCase(gateway, quota);
        var output = new CaptureExecutionOutputBoundary();

        await useCase.HandleAsync(
            new ImportExecutionRequest(
                request,
                planningOutput.Response!,
                new ImportExecutionMeteringContext("tenant-001", "user-001")),
            output,
            CancellationToken.None);

        Assert.Equal(1, output.Response!.CreditsUsed);
        Assert.Equal(24, output.Response.RemainingCredits);

        var balanceOutcome = await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Preview),
            CancellationToken.None);

        var balance = Assert.IsType<EnsureCurrentCreditBalanceOutcome.Succeeded>(balanceOutcome).Result;
        Assert.Equal(output.Response.RemainingCredits, balance.RemainingCredits);
        Assert.Equal(1, output.Response.CreditsUsed);
        Assert.Equal(
            1,
            store.GetTransactions("tenant-001").Count(transaction => transaction.EntryType == CreditEntryType.Usage));
    }

    [Fact]
    public async Task HandleAsync_WithRealCreditQuota_WhenUsageRecordFailsTwice_KeepsPlannerTaskAndStopsRun()
    {
        var inner = new InMemoryCreditLedgerStore();
        var store = new AlwaysFailRecordUsageCreditLedgerStore(inner);
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store);
        var gateway = new ExecutionPlannerGateway();
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

        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
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
        Assert.Equal(0, output.Response.CreditsUsed);
        Assert.Contains(output.Response.FailureItems, failure => failure.DiagnosticCode == "credits.usage_record_failed");
        Assert.DoesNotContain(inner.GetTransactions("tenant-001"), transaction => transaction.EntryType == CreditEntryType.Usage);
    }

    [Fact]
    public async Task HandleAsync_WithRealCreditQuota_StopsFurtherCreatesWhenBalanceExhaustedMidRun()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store);
        var gateway = new ExecutionPlannerGateway();
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

        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        await store.TryGrantFreeMonthlyAsync(
            "tenant-001",
            "202609",
            1,
            occurredUtc,
            "user-001",
            CancellationToken.None);

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
        Assert.Equal(1, output.Response.CreditsUsed);
        Assert.Equal(0, output.Response.RemainingCredits);
        Assert.Contains(output.Response.FailureItems, failure =>
            failure.DiagnosticCode == "credits.exhausted"
            && string.Equals(failure.Reference, "Task B", StringComparison.Ordinal));
        Assert.True(output.Response.RemainingCredits >= 0);
    }

    private sealed class AlwaysFailRecordUsageCreditLedgerStore(InMemoryCreditLedgerStore inner) : ICreditLedgerStore
    {
        public Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
            => inner.GetLotsAsync(tenantId, cancellationToken);

        public Task<bool> HasMonthGrantMarkerAsync(string tenantId, string utcYearMonth, CancellationToken cancellationToken)
            => inner.HasMonthGrantMarkerAsync(tenantId, utcYearMonth, cancellationToken);

        public Task<bool> ExpireFreeLotAsync(CreditLot lot, DateTimeOffset occurredUtc, string? actorUserId, CancellationToken cancellationToken)
            => inner.ExpireFreeLotAsync(lot, occurredUtc, actorUserId, cancellationToken);

        public Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
            string tenantId,
            string utcYearMonth,
            int grantQuantity,
            DateTimeOffset grantedAtUtc,
            string? actorUserId,
            CancellationToken cancellationToken)
            => inner.TryGrantFreeMonthlyAsync(tenantId, utcYearMonth, grantQuantity, grantedAtUtc, actorUserId, cancellationToken);

        public Task<RecordCreditUsageOutcome> RecordUsageAsync(RecordCreditUsageRequest request, CancellationToken cancellationToken)
            => Task.FromResult<RecordCreditUsageOutcome>(
                new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.UsageRecordFailed));
    }

    private static ImportPlanningUseCase CreatePlanningUseCase(IPlannerGateway plannerGateway)
        => new(
            plannerGateway,
            new CurrentTenantContextAccessorStub(),
            new TenantOperationalMetadataStoreStub(),
            new ConsentResolutionDefaults(
                ["Tasks.ReadWrite"],
                new Uri("https://example.test/admin-consent")));

    private sealed class CapturePlanningOutputBoundary : IImportPlanningOutputBoundary
    {
        public ImportPlanPreview? Response { get; private set; }

        public Task PresentAsync(ImportPlanPreview response, CancellationToken cancellationToken)
        {
            Response = response;
            return Task.CompletedTask;
        }
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

    private sealed class ExecutionPlannerGateway : IPlannerGateway
    {
        private readonly List<PlannerPlan> plans = [];
        private readonly Dictionary<string, List<PlannerBucket>> buckets = new();

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
            => Task.FromResult<IReadOnlyList<PlannerTaskSnapshot>>([]);

        public Task<PlannerTaskSnapshot> CreateTaskAsync(string planId, string bucketId, string taskName, string? description, int? priority, string? goal, CancellationToken cancellationToken)
            => Task.FromResult(new PlannerTaskSnapshot(Guid.NewGuid().ToString("N"), taskName, planId));

        public void AddPlan(string planId, string containerId, ContainerType containerType, string planName)
        {
            plans.Add(new PlannerPlan(planId, planName, containerId, containerType));
            buckets.TryAdd(planId, []);
        }
    }
}
