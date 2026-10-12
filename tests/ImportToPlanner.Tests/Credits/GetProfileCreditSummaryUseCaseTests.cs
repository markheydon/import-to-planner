using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class GetProfileCreditSummaryUseCaseTests
{
    [Fact]
    public async Task GetAsync_AfterPartialUsage_RemainingMatchesLedgerPostEnsure()
    {
        var (store, useCase) = CreateUseCase();
        var occurredUtc = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        const string tenantId = "tenant-001";

        await useCase.GetAsync(
            new ProfileCreditSummaryRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        for (var index = 0; index < 10; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest(tenantId, "user-001", occurredUtc, "run-1", $"task-{index}"),
                CancellationToken.None);
        }

        var outcome = await useCase.GetAsync(
            new ProfileCreditSummaryRequest(tenantId, "user-001", occurredUtc.AddHours(1)),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        var lots = await store.GetLotsAsync(tenantId, CancellationToken.None);
        var expectedRemaining = lots
            .Where(lot => lot.LotType is LotType.FreeMonthly or LotType.Paid)
            .Sum(lot => lot.RemainingQuantity);

        Assert.Equal(expectedRemaining, result.RemainingCredits);
        Assert.Equal(15, result.RemainingCredits);
    }

    [Fact]
    public async Task GetAsync_CurrentMonthFreeLot_MapsGrantedRemainingAndMonthEndExpiry()
    {
        var (_, useCase) = CreateUseCase();
        var occurredUtc = new DateTimeOffset(2026, 9, 3, 8, 30, 0, TimeSpan.Zero);

        var outcome = await useCase.GetAsync(
            new ProfileCreditSummaryRequest("tenant-001", "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        Assert.NotNull(result.FreeMonthlyLot);
        var freeLot = result.FreeMonthlyLot!;

        Assert.Equal(CommercialCreditPolicy.FreeMonthlyAllowance, freeLot.GrantedQuantity);
        Assert.Equal(CommercialCreditPolicy.FreeMonthlyAllowance, freeLot.RemainingQuantity);
        Assert.Equal(occurredUtc, freeLot.GrantedAtUtc);
        Assert.Equal(
            CommercialCreditPolicy.GetFreeLotExpiresAtUtc(occurredUtc),
            freeLot.ExpiresAtUtc);
        Assert.InRange(freeLot.RemainingQuantity, 0, freeLot.GrantedQuantity);
    }

    [Fact]
    public async Task GetAsync_CallsEnsureWithProfileReason()
    {
        var store = new InMemoryCreditLedgerStore();
        EnsureBalanceReason? capturedReason = null;
        var ensureUseCase = new ReasonCapturingEnsureUseCase(
            new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance),
            reason => capturedReason = reason);
        var useCase = new GetProfileCreditSummaryUseCase(ensureUseCase, store);
        var occurredUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

        await useCase.GetAsync(
            new ProfileCreditSummaryRequest("tenant-001", "user-001", occurredUtc),
            CancellationToken.None);

        Assert.Equal(EnsureBalanceReason.Profile, capturedReason);
    }

    [Fact]
    public async Task GetAsync_UsesRequestTenantIdOnly()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        var useCase = new GetProfileCreditSummaryUseCase(ensureUseCase, store);
        var occurredUtc = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);

        store.SeedLot(new CreditLot(
            "paid-other-tenant",
            "tenant-other",
            LotType.Paid,
            500,
            500,
            occurredUtc.AddMonths(-1),
            occurredUtc.AddYears(1)));

        var outcome = await useCase.GetAsync(
            new ProfileCreditSummaryRequest("tenant-001", "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        Assert.Equal(CommercialCreditPolicy.FreeMonthlyAllowance, result.RemainingCredits);
        Assert.Empty(result.PaidLots);
        Assert.Equal(500, (await store.GetLotsAsync("tenant-other", CancellationToken.None)).Single().RemainingQuantity);
    }

    [Fact]
    public async Task GetAsync_AtMonthBoundary_ExpiresPriorFreeLotAndMapsCurrentMonthLot()
    {
        var (store, useCase) = CreateUseCase();
        var august = new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero);
        const string tenantId = "tenant-001";

        await useCase.GetAsync(
            new ProfileCreditSummaryRequest(tenantId, "user-001", august),
            CancellationToken.None);

        for (var index = 0; index < 15; index++)
        {
            await store.RecordUsageAsync(
                new RecordCreditUsageRequest(tenantId, "user-001", august, "run-aug", $"task-{index}"),
                CancellationToken.None);
        }

        var september = new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);
        var outcome = await useCase.GetAsync(
            new ProfileCreditSummaryRequest(tenantId, "user-001", september),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        Assert.True(result.EnsureExpiryApplied);
        Assert.True(result.EnsureGrantApplied);
        Assert.Equal(CommercialCreditPolicy.FreeMonthlyAllowance, result.RemainingCredits);

        Assert.NotNull(result.FreeMonthlyLot);
        var freeLot = result.FreeMonthlyLot!;
        Assert.Equal(september, freeLot.GrantedAtUtc);
        Assert.Equal(CommercialCreditPolicy.GetUtcYearMonth(september), CommercialCreditPolicy.GetUtcYearMonth(freeLot.GrantedAtUtc));
        Assert.Equal(
            CommercialCreditPolicy.GetFreeLotExpiresAtUtc(september),
            freeLot.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetAsync_WhenMonthNotYetGranted_GrantsBeforeReturningSummary()
    {
        var (store, useCase) = CreateUseCase();
        var occurredUtc = new DateTimeOffset(2026, 9, 5, 11, 0, 0, TimeSpan.Zero);
        const string tenantId = "tenant-dormant";

        Assert.Empty(await store.GetLotsAsync(tenantId, CancellationToken.None));

        var outcome = await useCase.GetAsync(
            new ProfileCreditSummaryRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        Assert.True(result.EnsureGrantApplied);
        Assert.Equal(CommercialCreditPolicy.FreeMonthlyAllowance, result.RemainingCredits);
        Assert.NotNull(result.FreeMonthlyLot);
        Assert.True(await store.HasMonthGrantMarkerAsync(
            tenantId,
            CommercialCreditPolicy.GetUtcYearMonth(occurredUtc),
            CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_WhenLedgerContainsPaidLots_MapsOpenPaidLotsWithTwelveMonthExpiry()
    {
        var (store, useCase) = CreateUseCase();
        var occurredUtc = new DateTimeOffset(2026, 9, 12, 14, 0, 0, TimeSpan.Zero);
        const string tenantId = "tenant-001";
        var olderGrantedAt = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);
        var newerGrantedAt = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
        var olderPaidLotId = Guid.NewGuid().ToString("N");
        var newerPaidLotId = Guid.NewGuid().ToString("N");

        store.SeedLot(new CreditLot(
            olderPaidLotId,
            tenantId,
            LotType.Paid,
            50,
            40,
            olderGrantedAt,
            olderGrantedAt.AddYears(1)));
        store.SeedLot(new CreditLot(
            newerPaidLotId,
            tenantId,
            LotType.Paid,
            100,
            100,
            newerGrantedAt,
            newerGrantedAt.AddYears(1)));

        var outcome = await useCase.GetAsync(
            new ProfileCreditSummaryRequest(tenantId, "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        Assert.Equal(2, result.PaidLots.Count);
        Assert.Equal(newerPaidLotId, result.PaidLots[0].LotId);
        Assert.Equal(100, result.PaidLots[0].GrantedQuantity);
        Assert.Equal(100, result.PaidLots[0].RemainingQuantity);
        Assert.Equal(newerGrantedAt, result.PaidLots[0].GrantedAtUtc);
        Assert.Equal(newerGrantedAt.AddYears(1), result.PaidLots[0].ExpiresAtUtc);

        Assert.Equal(olderPaidLotId, result.PaidLots[1].LotId);
        Assert.Equal(50, result.PaidLots[1].GrantedQuantity);
        Assert.Equal(40, result.PaidLots[1].RemainingQuantity);
        Assert.Equal(olderGrantedAt.AddYears(1), result.PaidLots[1].ExpiresAtUtc);

        Assert.Equal(
            CommercialCreditPolicy.FreeMonthlyAllowance + 140,
            result.RemainingCredits);
    }

    private static (InMemoryCreditLedgerStore Store, GetProfileCreditSummaryUseCase UseCase) CreateUseCase()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        var useCase = new GetProfileCreditSummaryUseCase(ensureUseCase, store);
        return (store, useCase);
    }

    private sealed class ReasonCapturingEnsureUseCase(
        IEnsureCurrentCreditBalanceUseCase inner,
        Action<EnsureBalanceReason> onEnsure) : IEnsureCurrentCreditBalanceUseCase
    {
        public Task<EnsureCurrentCreditBalanceOutcome> EnsureAsync(
            EnsureCurrentCreditBalanceRequest request,
            CancellationToken cancellationToken)
        {
            onEnsure(request.Reason);
            return inner.EnsureAsync(request, cancellationToken);
        }
    }
}
