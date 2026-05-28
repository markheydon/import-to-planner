using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Infrastructure.Graph.CommercialAccounts;

/// <summary>
/// No-op audit store used when commercial mode is disabled.
/// </summary>
internal sealed class NoOpCommercialAuditStore : ICommercialAuditStore
{
    public Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AccountAuditEvent>>([]);

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
        => Task.FromResult(0);
}
