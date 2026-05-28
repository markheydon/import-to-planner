using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Infrastructure.Graph.CommercialAccounts;

/// <summary>
/// No-op account store used when commercial mode is disabled.
/// </summary>
internal sealed class NoOpCommercialAccountStore : ICommercialAccountStore
{
    public Task<CommercialAccount?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken)
        => Task.FromResult<CommercialAccount?>(null);

    public Task CreateAsync(CommercialAccount account, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task RestoreAsync(string tenantId, string userId, DateTimeOffset restoredUtc, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<CommercialAccount>>([]);

    public Task PurgeAsync(string tenantId, string userId, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
