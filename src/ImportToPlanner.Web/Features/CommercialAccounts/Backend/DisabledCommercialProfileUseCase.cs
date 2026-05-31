using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class DisabledCommercialProfileUseCase : ICommercialProfileUseCase
{
    public Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return Task.FromResult<CommercialAccount?>(null);
    }

    public Task DeleteAccountAsync(SessionIdentityContext sessionIdentity, DateTimeOffset occurredUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return Task.CompletedTask;
    }

    public Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return Task.FromResult(CommercialAccountRestoreResult.AccountNotFound);
    }

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}
