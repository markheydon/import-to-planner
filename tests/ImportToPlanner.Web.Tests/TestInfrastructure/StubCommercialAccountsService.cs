using ImportToPlanner.Commercial.Features.CommercialAccess.Models;
using ImportToPlanner.Commercial.Features.CommercialAccess.Services;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class StubCommercialAccountsService(CommercialAccountStoreStub accountStore) : ICommercialAccountsService
{
    public Task<CommercialAccount?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken)
        => accountStore.GetAsync(tenantId, userId, cancellationToken);

    public Task CreateAsync(CommercialAccount account, CancellationToken cancellationToken)
        => accountStore.CreateAsync(account, cancellationToken);

    public Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken)
        => accountStore.MarkDeletedAsync(tenantId, userId, deletedUtc, retentionExpiresUtc, cancellationToken);

    public Task RestoreAsync(
        string tenantId,
        string userId,
        DateTimeOffset restoredUtc,
        CancellationToken cancellationToken)
        => accountStore.RestoreAsync(tenantId, userId, restoredUtc, cancellationToken);

    public Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
        => accountStore.ListExpiredDeletedAsync(asOfUtc, batchSize, cancellationToken);

    public Task PurgeAsync(string tenantId, string userId, CancellationToken cancellationToken)
        => accountStore.PurgeAsync(tenantId, userId, cancellationToken);
}
