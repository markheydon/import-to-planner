using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

namespace ImportToPlanner.CommercialService.Tests.TestDoubles;

public sealed class CommercialAuditStoreStub : CommercialAuditService
{
    private readonly List<AccountAuditEvent> events = [];

    public IReadOnlyList<AccountAuditEvent> Events => events;

    public override Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public override Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
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

    public override Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var purgeCandidates = events
            .Where(auditEvent => auditEvent.RetentionExpiresUtc <= asOfUtc)
            .Take(Math.Max(0, batchSize))
            .ToArray();

        foreach (var purgeCandidate in purgeCandidates)
        {
            events.Remove(purgeCandidate);
        }

        return Task.FromResult(purgeCandidates.Length);
    }
}
