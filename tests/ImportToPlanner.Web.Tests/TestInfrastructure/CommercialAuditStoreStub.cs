namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class CommercialAuditStoreStub
{
    private readonly List<DateTimeOffset> retentionExpirations = [];

    public Task AppendAsync(DateTimeOffset retentionExpiresUtc, CancellationToken cancellationToken)
    {
        retentionExpirations.Add(retentionExpiresUtc);
        return Task.CompletedTask;
    }

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        var purgeCandidates = retentionExpirations
            .Where(retentionExpiresUtc => retentionExpiresUtc <= asOfUtc)
            .Take(Math.Max(0, batchSize))
            .ToArray();

        foreach (var candidate in purgeCandidates)
        {
            retentionExpirations.Remove(candidate);
        }

        return Task.FromResult(purgeCandidates.Length);
    }
}
