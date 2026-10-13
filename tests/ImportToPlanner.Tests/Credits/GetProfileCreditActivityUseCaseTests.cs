using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class GetProfileCreditActivityUseCaseTests
{
    [Fact]
    public async Task GetAsync_TwoImportRuns_ReturnsTwoGroupedUsageRows()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = CreateActivityUseCase(store);
        var occurredUtc = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        const string tenantId = "tenant-001";

        await SeedGrantAndUsageAsync(store, tenantId, occurredUtc);

        var runAOldest = occurredUtc.AddHours(-4);
        for (var index = 0; index < 4; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest(
                    tenantId,
                    "user-001",
                    runAOldest.AddMinutes(index),
                    "run-alpha",
                    $"alpha-task-{index}"),
                CancellationToken.None);
        }

        var runBOldest = occurredUtc.AddDays(-2);
        for (var index = 0; index < 2; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest(
                    tenantId,
                    "user-001",
                    runBOldest.AddMinutes(index),
                    "run-beta",
                    $"beta-task-{index}"),
                CancellationToken.None);
        }

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        var usageRows = result.Entries
            .Where(entry => entry.ActivityKind == ProfileCreditActivityKind.ImportRunUsage)
            .ToList();

        Assert.Equal(2, usageRows.Count);
        Assert.Contains(usageRows, row => row.ImportRunId == "run-alpha" && row.Quantity == 4);
        Assert.Contains(usageRows, row => row.ImportRunId == "run-beta" && row.Quantity == 2);
        Assert.Equal(runAOldest.AddMinutes(3), usageRows.Single(row => row.ImportRunId == "run-alpha").OccurredUtc);
    }

    [Fact]
    public async Task GetAsync_NonComplimentaryTenant_IncludesGrantAndExpiryRows()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        var activityUseCase = CreateActivityUseCase(store);
        const string tenantId = "tenant-001";
        var august = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var september = new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);

        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(tenantId, "user-001", august, EnsureBalanceReason.Profile),
            CancellationToken.None);

        for (var index = 0; index < 5; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest(tenantId, "user-001", august, "run-aug", $"task-{index}"),
                CancellationToken.None);
        }

        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(tenantId, "user-001", september, EnsureBalanceReason.Profile),
            CancellationToken.None);

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", september),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        Assert.Contains(
            result.Entries,
            entry => entry.ActivityKind == ProfileCreditActivityKind.FreeMonthlyGrant
                && entry.IsCreditIncrease);
        Assert.Contains(
            result.Entries,
            entry => entry.ActivityKind == ProfileCreditActivityKind.FreeCreditExpiry
                && !entry.IsCreditIncrease);
    }

    [Fact]
    public async Task GetAsync_PreWindowEvents_OmittedWithHasOlderHistoryTrue()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = CreateActivityUseCase(store);
        const string tenantId = "tenant-001";
        var occurredUtc = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var windowStartUtc = occurredUtc.AddMonths(-12);
        var preWindowUtc = windowStartUtc.AddDays(-10);
        var inWindowUtc = windowStartUtc.AddDays(1);

        store.SeedTransaction(new CreditLedgerTransaction(
            "pre-window-grant",
            tenantId,
            preWindowUtc,
            CreditEntryType.FreeGrant,
            25,
            "lot-pre",
            LotType.FreeMonthly));
        store.SeedTransaction(new CreditLedgerTransaction(
            "in-window-usage",
            tenantId,
            inWindowUtc,
            CreditEntryType.Usage,
            3,
            "lot-in",
            LotType.FreeMonthly,
            "run-in-window",
            "task-1"));

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        Assert.Equal(windowStartUtc, result.ActivityWindowStartUtc);
        Assert.True(result.HasOlderHistory);
        Assert.DoesNotContain(result.Entries, entry => entry.OccurredUtc == preWindowUtc);
        Assert.Contains(
            result.Entries,
            entry => entry.ActivityKind == ProfileCreditActivityKind.ImportRunUsage && entry.ImportRunId == "run-in-window");
    }

    [Fact]
    public async Task GetAsync_ComplimentaryTenant_ReturnsUsageOnlyRows()
    {
        var store = new InMemoryCreditLedgerStore();
        const string tenantId = "tenant-complimentary";
        var policy = new TestComplimentaryTenantPolicy(tenantId);
        var activityUseCase = CreateActivityUseCase(store, policy);
        var occurredUtc = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

        store.SeedTransaction(new CreditLedgerTransaction(
            "grant-should-hide",
            tenantId,
            occurredUtc.AddDays(-2),
            CreditEntryType.FreeGrant,
            25,
            "lot-free",
            LotType.FreeMonthly));
        store.SeedTransaction(new CreditLedgerTransaction(
            "expiry-should-hide",
            tenantId,
            occurredUtc.AddDays(-1),
            CreditEntryType.FreeExpiry,
            10,
            "lot-free",
            LotType.FreeMonthly));
        store.SeedTransaction(new CreditLedgerTransaction(
            "purchase-should-hide",
            tenantId,
            occurredUtc.AddHours(-3),
            CreditEntryType.PaidPurchase,
            100,
            "lot-paid",
            LotType.Paid));

        for (var index = 0; index < 3; index++)
        {
            await store.RecordComplimentaryUsageAsync(
                new RecordCreditUsageRequest(
                    tenantId,
                    "user-001",
                    occurredUtc.AddHours(-index),
                    "run-comp",
                    $"task-{index}"),
                CancellationToken.None);
        }

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        Assert.All(result.Entries, entry => Assert.Equal(ProfileCreditActivityKind.ImportRunUsage, entry.ActivityKind));
        Assert.Single(result.Entries);
        Assert.Equal(3, result.Entries[0].Quantity);
        Assert.Equal("run-comp", result.Entries[0].ImportRunId);
    }

    [Fact]
    public async Task GetAsync_UsageWithoutImportRunId_ReturnsOneRowPerUsageTransaction()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = CreateActivityUseCase(store);
        const string tenantId = "tenant-001";
        var occurredUtc = new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.Zero);
        var firstUtc = occurredUtc.AddHours(-2);
        var secondUtc = occurredUtc.AddHours(-1);

        store.SeedTransaction(new CreditLedgerTransaction(
            "usage-no-run-1",
            tenantId,
            firstUtc,
            CreditEntryType.Usage,
            2,
            "lot-1",
            LotType.FreeMonthly));
        store.SeedTransaction(new CreditLedgerTransaction(
            "usage-no-run-2",
            tenantId,
            secondUtc,
            CreditEntryType.Usage,
            1,
            "lot-1",
            LotType.FreeMonthly));

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        var usageRows = result.Entries
            .Where(entry => entry.ActivityKind == ProfileCreditActivityKind.ImportRunUsage)
            .ToList();

        Assert.Equal(2, usageRows.Count);
        Assert.Equal(2, usageRows.Single(row => row.OccurredUtc == firstUtc).Quantity);
        Assert.Equal(1, usageRows.Single(row => row.OccurredUtc == secondUtc).Quantity);
        Assert.All(usageRows, row => Assert.Null(row.ImportRunId));
    }

    [Fact]
    public async Task GetAsync_GroupedImportRunUsage_QuantityEqualsPerRunTotalNotPerTask()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = CreateActivityUseCase(store);
        const string tenantId = "tenant-001";
        var occurredUtc = new DateTimeOffset(2026, 9, 8, 11, 0, 0, TimeSpan.Zero);

        await SeedGrantAndUsageAsync(store, tenantId, occurredUtc);

        const int taskCount = 7;
        for (var index = 0; index < taskCount; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest(
                    tenantId,
                    "user-001",
                    occurredUtc.AddMinutes(-index),
                    "run-single",
                    $"task-{index}"),
                CancellationToken.None);
        }

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        var usageRows = result.Entries
            .Where(entry => entry.ActivityKind == ProfileCreditActivityKind.ImportRunUsage)
            .ToList();

        var groupedRun = Assert.Single(usageRows, row => row.ImportRunId == "run-single");
        Assert.Equal(taskCount, groupedRun.Quantity);
        Assert.NotEqual(taskCount, result.Entries.Count(entry => entry.ActivityKind == ProfileCreditActivityKind.ImportRunUsage));
    }

    [Fact]
    public async Task GetAsync_NonComplimentaryWithPaidAdjustment_IncludesAdjustmentRow()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = CreateActivityUseCase(store);
        const string tenantId = "tenant-001";
        var occurredUtc = new DateTimeOffset(2026, 9, 20, 16, 0, 0, TimeSpan.Zero);
        var adjustmentUtc = occurredUtc.AddDays(-2);

        store.SeedTransaction(new CreditLedgerTransaction(
            "paid-adjustment",
            tenantId,
            adjustmentUtc,
            CreditEntryType.PaidAdjustment,
            12,
            "paid-lot-1",
            LotType.Paid));

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        Assert.Contains(
            result.Entries,
            entry => entry.ActivityKind == ProfileCreditActivityKind.PaidAdjustment
                && entry.Quantity == 12
                && !entry.IsCreditIncrease);
    }

    [Fact]
    public async Task GetAsync_NonComplimentaryWithPaidLedgerRows_IncludesPaidPurchaseAndPaidLotExpiry()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = CreateActivityUseCase(store);
        const string tenantId = "tenant-001";
        var occurredUtc = new DateTimeOffset(2026, 9, 20, 16, 0, 0, TimeSpan.Zero);
        var purchaseUtc = occurredUtc.AddDays(-3);
        var paidExpiryUtc = occurredUtc.AddDays(-1);

        store.SeedTransaction(new CreditLedgerTransaction(
            "paid-purchase",
            tenantId,
            purchaseUtc,
            CreditEntryType.PaidPurchase,
            250,
            "paid-lot-1",
            LotType.Paid));
        store.SeedTransaction(new CreditLedgerTransaction(
            "paid-expiry",
            tenantId,
            paidExpiryUtc,
            CreditEntryType.PaidExpiry,
            40,
            "paid-lot-1",
            LotType.Paid));

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        Assert.Contains(
            result.Entries,
            entry => entry.ActivityKind == ProfileCreditActivityKind.PaidPurchase
                && entry.Quantity == 250
                && entry.IsCreditIncrease);
        Assert.Contains(
            result.Entries,
            entry => entry.ActivityKind == ProfileCreditActivityKind.PaidLotExpiry
                && entry.Quantity == 40
                && !entry.IsCreditIncrease);
        Assert.True(result.Entries[0].OccurredUtc >= result.Entries[1].OccurredUtc);
    }

    [Fact]
    public async Task GetAsync_WhenListTransactionsFails_ReturnsLedgerUnavailableWithNoEntries()
    {
        var inner = new InMemoryCreditLedgerStore();
        var store = new FaultInjectingCreditLedgerStore(inner) { FailListTransactions = true };
        var activityUseCase = CreateActivityUseCase(store);

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest("tenant-001", "user-001", DateTimeOffset.UtcNow),
            CancellationToken.None);

        var failure = Assert.IsType<GetProfileCreditActivityOutcome.Failed>(outcome);
        Assert.Equal(CommercialCreditFailureCodes.LedgerUnavailable, failure.Failure.FailureCode);
    }

    [Fact]
    public async Task GetAsync_WhenHasTransactionsBeforeFails_ReturnsLedgerUnavailableWithNoEntries()
    {
        var inner = new InMemoryCreditLedgerStore();
        inner.SeedTransaction(new CreditLedgerTransaction(
            "in-window",
            "tenant-001",
            DateTimeOffset.UtcNow.AddDays(-1),
            CreditEntryType.Usage,
            1,
            "lot-1",
            LotType.FreeMonthly,
            "run-1",
            "task-1"));
        var store = new FaultInjectingCreditLedgerStore(inner) { FailHasTransactionsBefore = true };
        var activityUseCase = CreateActivityUseCase(store);

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest("tenant-001", "user-001", DateTimeOffset.UtcNow),
            CancellationToken.None);

        var failure = Assert.IsType<GetProfileCreditActivityOutcome.Failed>(outcome);
        Assert.Equal(CommercialCreditFailureCodes.LedgerUnavailable, failure.Failure.FailureCode);
    }

    private static GetProfileCreditActivityUseCase CreateActivityUseCase(
        ICreditLedgerStore store,
        IComplimentaryTenantPolicy? policy = null)
        => new(store, policy ?? DisabledComplimentaryTenantPolicy.Instance);

    private static async Task SeedGrantAndUsageAsync(
        InMemoryCreditLedgerStore store,
        string tenantId,
        DateTimeOffset occurredUtc)
    {
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        await ensureUseCase.EnsureAsync(
            new EnsureCurrentCreditBalanceRequest(tenantId, "user-001", occurredUtc, EnsureBalanceReason.Profile),
            CancellationToken.None);
    }

    private sealed class FaultInjectingCreditLedgerStore(InMemoryCreditLedgerStore inner) : ICreditLedgerStore
    {
        public bool FailListTransactions { get; init; }

        public bool FailHasTransactionsBefore { get; init; }

        public Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
            => inner.GetLotsAsync(tenantId, cancellationToken);

        public Task<bool> HasMonthGrantMarkerAsync(
            string tenantId,
            string utcYearMonth,
            CancellationToken cancellationToken)
            => inner.HasMonthGrantMarkerAsync(tenantId, utcYearMonth, cancellationToken);

        public Task<bool> ExpireFreeLotAsync(
            CreditLot lot,
            DateTimeOffset occurredUtc,
            string? actorUserId,
            CancellationToken cancellationToken)
            => inner.ExpireFreeLotAsync(lot, occurredUtc, actorUserId, cancellationToken);

        public Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
            string tenantId,
            string utcYearMonth,
            int grantQuantity,
            DateTimeOffset grantedAtUtc,
            string? actorUserId,
            CancellationToken cancellationToken)
            => inner.TryGrantFreeMonthlyAsync(
                tenantId,
                utcYearMonth,
                grantQuantity,
                grantedAtUtc,
                actorUserId,
                cancellationToken);

        public Task<RecordCreditUsageOutcome> RecordUsageAsync(
            RecordCreditUsageRequest request,
            CancellationToken cancellationToken)
            => inner.RecordUsageAsync(request, cancellationToken);

        public Task<RecordCreditUsageOutcome> RecordComplimentaryUsageAsync(
            RecordCreditUsageRequest request,
            CancellationToken cancellationToken)
            => inner.RecordComplimentaryUsageAsync(request, cancellationToken);

        public Task<IReadOnlyList<CreditLedgerTransaction>> ListTransactionsAsync(
            string tenantId,
            DateTimeOffset occurredSinceUtc,
            CancellationToken cancellationToken)
        {
            if (FailListTransactions)
            {
                throw new InvalidOperationException("Simulated list failure.");
            }

            return inner.ListTransactionsAsync(tenantId, occurredSinceUtc, cancellationToken);
        }

        public Task<bool> HasTransactionsBeforeAsync(
            string tenantId,
            DateTimeOffset beforeOccurredUtc,
            CancellationToken cancellationToken)
        {
            if (FailHasTransactionsBefore)
            {
                throw new InvalidOperationException("Simulated existence-check failure.");
            }

            return inner.HasTransactionsBeforeAsync(tenantId, beforeOccurredUtc, cancellationToken);
        }

        public Task<IReadOnlyList<CreditLot>> ListDuePaidCreditLotsAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken)
            => inner.ListDuePaidCreditLotsAsync(asOfUtc, cancellationToken);

        public Task<CreditLot?> GetPaidLotByExternalPaymentIdAsync(string tenantId, string externalPaymentId, CancellationToken cancellationToken)
            => inner.GetPaidLotByExternalPaymentIdAsync(tenantId, externalPaymentId, cancellationToken);

        public Task<CreditGrantAttemptOutcome> TryGrantPaidPurchaseAsync(
            string tenantId,
            string externalPaymentId,
            string externalEventId,
            int grantQuantity,
            DateTimeOffset grantedAtUtc,
            string actorUserId,
            CancellationToken cancellationToken)
            => inner.TryGrantPaidPurchaseAsync(
                tenantId,
                externalPaymentId,
                externalEventId,
                grantQuantity,
                grantedAtUtc,
                actorUserId,
                cancellationToken);

        public Task<bool> ExpirePaidLotAsync(CreditLot lot, DateTimeOffset occurredUtc, string? actorUserId, CancellationToken cancellationToken)
            => inner.ExpirePaidLotAsync(lot, occurredUtc, actorUserId, cancellationToken);

        public Task<PaidCreditAdjustmentAttemptOutcome> TryAdjustPaidCreditsAsync(
            string tenantId,
            string externalPaymentId,
            string externalAdjustmentId,
            DateTimeOffset occurredUtc,
            CancellationToken cancellationToken)
            => inner.TryAdjustPaidCreditsAsync(
                tenantId,
                externalPaymentId,
                externalAdjustmentId,
                occurredUtc,
                cancellationToken);
    }
}
