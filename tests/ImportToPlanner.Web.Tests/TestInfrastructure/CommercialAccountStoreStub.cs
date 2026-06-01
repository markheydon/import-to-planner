using ImportToPlanner.Web.Features.CommercialAccounts.Backend;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CommercialAccountStoreStub
{
    private readonly Dictionary<string, CommercialAccount> values = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<CommercialAccount> Accounts => values.Values;

    public Task<CommercialAccount?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        values.TryGetValue(BuildKey(tenantId, userId), out var account);
        return Task.FromResult(account);
    }

    public Task CreateAsync(CommercialAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        values[BuildKey(account.TenantId, account.UserId)] = account;
        return Task.CompletedTask;
    }

    public Task MarkDeletedAsync(
        string tenantId,
        string userId,
        DateTimeOffset deletedUtc,
        DateTimeOffset retentionExpiresUtc,
        CancellationToken cancellationToken)
    {
        if (!values.TryGetValue(BuildKey(tenantId, userId), out var current))
        {
            return Task.CompletedTask;
        }

        values[BuildKey(tenantId, userId)] = current with
        {
            Status = CommercialAccountStatus.Deleted,
            DeletedUtc = deletedUtc,
            RetentionExpiresUtc = retentionExpiresUtc,
        };

        return Task.CompletedTask;
    }

    public Task RestoreAsync(string tenantId, string userId, DateTimeOffset restoredUtc, CancellationToken cancellationToken)
    {
        if (!values.TryGetValue(BuildKey(tenantId, userId), out var current))
        {
            return Task.CompletedTask;
        }

        values[BuildKey(tenantId, userId)] = current with
        {
            Status = CommercialAccountStatus.Active,
            DeletedUtc = null,
            RetentionExpiresUtc = null,
            RestoredUtc = restoredUtc,
        };

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CommercialAccount>> ListExpiredDeletedAsync(
        DateTimeOffset asOfUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = values.Values
            .Where(account => account.Status == CommercialAccountStatus.Deleted
                && account.RetentionExpiresUtc is not null
                && account.RetentionExpiresUtc <= asOfUtc)
            .Take(Math.Max(0, batchSize))
            .ToArray();

        return Task.FromResult<IReadOnlyList<CommercialAccount>>(expired);
    }

    public Task PurgeAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        values.Remove(BuildKey(tenantId, userId));
        return Task.CompletedTask;
    }

    private static string BuildKey(string tenantId, string userId) => $"{tenantId}|{userId}";
}
