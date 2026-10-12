using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Tests.TestInfrastructure;

namespace ImportToPlanner.Tests.Credits;

public sealed class ComplimentaryCreditQuotaTests
{
    private static readonly Guid AllowlistedTenant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task BeforeCreateAsync_ForComplimentaryTenant_NeverReturnsExhausted()
    {
        var store = new InMemoryCreditLedgerStore();
        var policy = new TestComplimentaryTenantPolicy(AllowlistedTenant.ToString());
        var ensureUseCase = new EnsureCurrentCreditBalanceUseCase(store, policy);
        var quota = new ImportTaskCreationCreditQuota(ensureUseCase, store, policy, new ImportExecutionCreditBalanceCache());
        var occurredUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 30; index++)
        {
            var beforeCreate = await quota.BeforeCreateAsync(
                new ImportTaskCreationQuotaContext(
                    AllowlistedTenant.ToString(),
                    "user-001",
                    occurredUtc,
                    "run-1",
                    $"Task {index}"),
                CancellationToken.None);

            Assert.Equal(TaskCreationQuotaStatus.Allow, beforeCreate.Status);
            Assert.NotEqual(TaskCreationQuotaStatus.Exhausted, beforeCreate.Status);

            await quota.RecordSuccessfulCreateAsync(
                new ImportTaskCreationQuotaContext(
                    AllowlistedTenant.ToString(),
                    "user-001",
                    occurredUtc,
                    "run-1",
                    $"Task {index}",
                    $"planner-task-{index}"),
                CancellationToken.None);
        }
    }
}
