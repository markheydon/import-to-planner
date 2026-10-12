using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class ProfileCreditTenancyTests
{
    [Fact]
    public async Task GetProfileCreditSummary_HonoursRequestTenantId()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        var summaryUseCase = new GetProfileCreditSummaryUseCase(ensureUseCase, store);
        var occurredUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        store.SeedLot(new CreditLot(
            "paid-other",
            "tenant-other",
            LotType.Paid,
            500,
            500,
            occurredUtc,
            occurredUtc.AddMonths(12)));

        var outcome = await summaryUseCase.GetAsync(
            new ProfileCreditSummaryRequest("tenant-001", "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditSummaryOutcome.Succeeded>(outcome).Result;
        Assert.Equal(25, result.RemainingCredits);
        Assert.Empty(result.PaidLots);
    }

    [Fact]
    public async Task GetProfileCreditActivity_HonoursRequestTenantId()
    {
        var store = new InMemoryCreditLedgerStore();
        var activityUseCase = new GetProfileCreditActivityUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        var occurredUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        store.SeedTransaction(new CreditLedgerTransaction(
            "tx-other",
            "tenant-other",
            occurredUtc,
            CreditEntryType.FreeGrant,
            25,
            "lot-other",
            LotType.FreeMonthly));

        var outcome = await activityUseCase.GetAsync(
            new ProfileCreditActivityRequest("tenant-001", "user-001", occurredUtc),
            CancellationToken.None);

        var result = Assert.IsType<GetProfileCreditActivityOutcome.Succeeded>(outcome).Result;
        Assert.Empty(result.Entries);
    }
}
