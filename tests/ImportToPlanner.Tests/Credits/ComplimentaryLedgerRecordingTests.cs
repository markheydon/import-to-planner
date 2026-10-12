using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class ComplimentaryLedgerRecordingTests
{
    private static readonly Guid AllowlistedTenant = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task RecordComplimentaryUsageAsync_WritesPairedUsageAndGrantWithoutNegativeBalance()
    {
        var store = new InMemoryCreditLedgerStore();
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);
        const int createCount = 3;

        for (var index = 0; index < createCount; index++)
        {
            var outcome = await store.RecordComplimentaryUsageAsync(
                new RecordCreditUsageRequest(
                    AllowlistedTenant.ToString(),
                    "user-001",
                    occurredUtc,
                    "run-1",
                    $"planner-task-{index}"),
                CancellationToken.None);

            Assert.IsType<RecordCreditUsageOutcome.Success>(outcome);
        }

        var transactions = store.GetTransactions(AllowlistedTenant.ToString());
        Assert.Equal(createCount, transactions.Count(transaction => transaction.EntryType == CreditEntryType.Usage));
        Assert.Equal(createCount, transactions.Count(transaction => transaction.EntryType == CreditEntryType.ComplimentaryGrant));

        var lots = await store.GetLotsAsync(AllowlistedTenant.ToString(), CancellationToken.None);
        var (remaining, _, _) = EnsureCurrentCreditBalanceUseCase.DeriveBalance(lots);
        Assert.True(remaining >= 0);
    }
}
