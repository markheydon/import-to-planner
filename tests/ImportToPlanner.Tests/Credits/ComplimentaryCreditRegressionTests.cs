using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class ComplimentaryCreditRegressionTests
{
    [Fact]
    public async Task BeforeCreateAsync_WithEmptyAllowlist_StillReturnsExhaustedAtZeroRemaining()
    {
        var store = new InMemoryCreditLedgerStore();
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, DisabledComplimentaryTenantPolicy.Instance);
        var quota = new ImportTaskCreationCreditQuota(
            ensureUseCase,
            store,
            DisabledComplimentaryTenantPolicy.Instance,
            new ImportExecutionCreditBalanceCache());
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
    }
}
