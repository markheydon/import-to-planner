using ImportToPlanner.CommercialService.CommercialAccounts;
using ImportToPlanner.CommercialService.Models;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CommercialAuditStoreStub : ICommercialAuditStore
{
    private readonly List<AccountAuditEvent> events = [];

    public IReadOnlyList<AccountAuditEvent> Events => events;

    public Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = events
            .Where(auditEvent => auditEvent.RetentionExpiresUtc <= asOfUtc)
            .Take(Math.Max(0, batchSize))
            .ToArray();

        return Task.FromResult<IReadOnlyList<AccountAuditEvent>>(expired);
    }

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var purgeCandidates = events
            .Where(auditEvent => auditEvent.RetentionExpiresUtc <= asOfUtc)
            .Take(Math.Max(0, batchSize))
            .ToArray();

        foreach (var candidate in purgeCandidates)
        {
            events.Remove(candidate);
        }

        return Task.FromResult(purgeCandidates.Length);
    }
}
