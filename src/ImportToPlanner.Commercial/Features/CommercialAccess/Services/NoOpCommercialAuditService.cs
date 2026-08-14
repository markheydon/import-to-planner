using ImportToPlanner.Commercial.Features.CommercialAccess.Models;

namespace ImportToPlanner.Commercial.Features.CommercialAccess.Services;

internal sealed class NoOpCommercialAuditService : ICommercialAuditService
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
