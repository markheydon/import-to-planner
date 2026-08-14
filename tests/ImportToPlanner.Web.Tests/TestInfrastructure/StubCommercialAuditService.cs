using ImportToPlanner.Commercial.Features.CommercialAccess.Models;
using ImportToPlanner.Commercial.Features.CommercialAccess.Services;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class StubCommercialAuditService(CommercialAuditStoreStub auditStore) : ICommercialAuditService
{
    public Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        return auditStore.AppendAsync(auditEvent.RetentionExpiresUtc, cancellationToken);
    }

    public Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AccountAuditEvent>>([]);

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
        => auditStore.PurgeExpiredAsync(asOfUtc, batchSize, cancellationToken);
}
