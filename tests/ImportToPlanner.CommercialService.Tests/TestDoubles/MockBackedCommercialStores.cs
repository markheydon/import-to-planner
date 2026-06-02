using Azure.Data.Tables;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using Moq;

namespace ImportToPlanner.CommercialService.Tests.TestDoubles;

internal sealed class MockBackedCommercialAccountsService(ICommercialAccountsService inner)
    : CommercialAccountsService(Mock.Of<TableServiceClient>())
{
    public override Task<CommercialAccount?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken)
        => inner.GetAsync(tenantId, userId, cancellationToken);

    public override Task CreateAsync(CommercialAccount account, CancellationToken cancellationToken)
        => inner.CreateAsync(account, cancellationToken);

    public override Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken)
        => inner.MarkDeletedAsync(tenantId, userId, deletedUtc, retentionExpiresUtc, cancellationToken);

    public override Task RestoreAsync(
        string tenantId,
        string userId,
        DateTimeOffset restoredUtc,
        CancellationToken cancellationToken)
        => inner.RestoreAsync(tenantId, userId, restoredUtc, cancellationToken);

    public override Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
        => inner.ListExpiredDeletedAsync(asOfUtc, batchSize, cancellationToken);

    public override Task PurgeAsync(string tenantId, string userId, CancellationToken cancellationToken)
        => inner.PurgeAsync(tenantId, userId, cancellationToken);
}

internal sealed class MockBackedCommercialAuditService(ICommercialAuditService inner)
    : CommercialAuditService(Mock.Of<TableServiceClient>())
{
    public override Task AppendAsync(AccountAuditEvent auditEvent, CancellationToken cancellationToken)
        => inner.AppendAsync(auditEvent, cancellationToken);

    public override Task<IReadOnlyList<AccountAuditEvent>> ListExpiredAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
        => inner.ListExpiredAsync(asOfUtc, batchSize, cancellationToken);

    public override Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
        => inner.PurgeExpiredAsync(asOfUtc, batchSize, cancellationToken);
}
