using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class ImportTaskCreationCreditQuotaTests
{
    [Fact]
    public async Task RecordSuccessfulCreateAsync_WritesOneUsagePerCreatedTask()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store, new ImportExecutionCreditBalanceCache());
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Execute),
            CancellationToken.None);

        var context = new ImportTaskCreationQuotaContext(
            "tenant-001",
            "user-001",
            occurredUtc,
            "run-1",
            "Task A",
            "planner-task-1");

        var recordResult = await quota.RecordSuccessfulCreateAsync(context, CancellationToken.None);

        Assert.True(recordResult.Succeeded);
        Assert.Equal(24, recordResult.RemainingCredits);
        Assert.Single(store.GetTransactions("tenant-001"), transaction => transaction.EntryType == CreditEntryType.Usage);
    }

    [Fact]
    public async Task BeforeCreateAsync_WhenRemainingIsZero_ReturnsExhaustedWithoutNegativeBalance()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store, new ImportExecutionCreditBalanceCache());
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Execute),
            CancellationToken.None);

        for (var index = 0; index < 25; index++)
        {
            await quota.RecordSuccessfulCreateAsync(
                new ImportTaskCreationQuotaContext(
                    "tenant-001",
                    "user-001",
                    occurredUtc,
                    "run-1",
                    $"Task {index}",
                    $"planner-task-{index}"),
                CancellationToken.None);
        }

        var beforeCreate = await quota.BeforeCreateAsync(
            new ImportTaskCreationQuotaContext("tenant-001", "user-001", occurredUtc, "run-1", "Blocked task"),
            CancellationToken.None);

        Assert.Equal(TaskCreationQuotaStatus.Exhausted, beforeCreate.Status);
        Assert.Equal(0, beforeCreate.RemainingCredits);
        var lots = await store.GetLotsAsync("tenant-001", CancellationToken.None);
        Assert.True(lots.All(lot => lot.RemainingQuantity >= 0));
    }

    [Fact]
    public async Task RecordSuccessfulCreateAsync_RetriesOnceWhenFirstUsageRecordFails()
    {
        var inner = new InMemoryCreditLedgerStore();
        var store = new FailOnceRecordUsageCreditLedgerStore(inner);
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store, new ImportExecutionCreditBalanceCache());
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Execute),
            CancellationToken.None);

        var context = new ImportTaskCreationQuotaContext(
            "tenant-001",
            "user-001",
            occurredUtc,
            "run-1",
            "Task A",
            "planner-task-1");

        var recordResult = await quota.RecordSuccessfulCreateAsync(context, CancellationToken.None);

        Assert.True(recordResult.Succeeded);
        Assert.Equal(24, recordResult.RemainingCredits);
        Assert.Equal(2, store.RecordUsageCallCount);
        Assert.Single(inner.GetTransactions("tenant-001"), transaction => transaction.EntryType == CreditEntryType.Usage);
    }

    [Fact]
    public async Task RecordSuccessfulCreateAsync_WhenCalledTwiceForSameTask_RecordsSingleUsage()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store, new ImportExecutionCreditBalanceCache());
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest("tenant-001", "user-001", occurredUtc, EnsureBalanceReason.Execute),
            CancellationToken.None);

        var context = new ImportTaskCreationQuotaContext(
            "tenant-001",
            "user-001",
            occurredUtc,
            "run-1",
            "Task A",
            "planner-task-1");

        var firstRecord = await quota.RecordSuccessfulCreateAsync(context, CancellationToken.None);
        var secondRecord = await quota.RecordSuccessfulCreateAsync(context, CancellationToken.None);

        Assert.True(firstRecord.Succeeded);
        Assert.True(secondRecord.Succeeded);
        Assert.Equal(firstRecord.RemainingCredits, secondRecord.RemainingCredits);
        Assert.Single(store.GetTransactions("tenant-001"), transaction => transaction.EntryType == CreditEntryType.Usage);
    }

    [Fact]
    public async Task BeforeCreateAsync_ReusesCachedBalanceWithoutSecondEnsureCall()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureCallCount = 0;
        var ensureUseCase = new CountingEnsureCurrentCreditBalanceUseCase(
            new EnsureCurrentCreditBalanceUseCase(store),
            () => ensureCallCount++);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store, new ImportExecutionCreditBalanceCache());
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var context = new ImportTaskCreationQuotaContext("tenant-001", "user-001", occurredUtc, "run-1", "Task A");

        var first = await quota.BeforeCreateAsync(context, CancellationToken.None);
        var second = await quota.BeforeCreateAsync(context, CancellationToken.None);

        Assert.Equal(TaskCreationQuotaStatus.Allow, first.Status);
        Assert.Equal(TaskCreationQuotaStatus.Allow, second.Status);
        Assert.Equal(1, ensureCallCount);
    }

    private sealed class CountingEnsureCurrentCreditBalanceUseCase(
        IEnsureCurrentCreditBalanceUseCase inner,
        Action onEnsure) : IEnsureCurrentCreditBalanceUseCase
    {
        public Task<EnsureCurrentCreditBalanceOutcome> EnsureAsync(
            EnsureCurrentCreditBalanceRequest request,
            CancellationToken cancellationToken)
        {
            onEnsure();
            return inner.EnsureAsync(request, cancellationToken);
        }
    }

    private sealed class FailOnceRecordUsageCreditLedgerStore(InMemoryCreditLedgerStore inner) : ICreditLedgerStore
    {
        private bool hasFailedOnce;

        public int RecordUsageCallCount { get; private set; }

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
        {
            RecordUsageCallCount++;
            if (!hasFailedOnce)
            {
                hasFailedOnce = true;
                return Task.FromResult<RecordCreditUsageOutcome>(
                    new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.UsageRecordFailed));
            }

            return inner.RecordUsageAsync(request, cancellationToken);
        }
    }
}
