using ImportToPlanner.CommercialService.Models;

namespace ImportToPlanner.CommercialService.CommercialAccounts;

/// <summary>
/// Orchestrates profile and lifecycle operations for commercial accounts.
/// </summary>
public sealed class GetCommercialProfileUseCase(
    ICommercialAccountStore commercialAccountStore,
    DeleteCommercialAccountUseCase deleteCommercialAccountUseCase,
    RestoreCommercialAccountUseCase restoreCommercialAccountUseCase,
    PurgeExpiredCommercialAccountsUseCase purgeExpiredCommercialAccountsUseCase) : ICommercialProfileUseCase
{
    /// <inheritdoc/>
    public Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionIdentity.UserId);

        return commercialAccountStore.GetAsync(sessionIdentity.TenantId, sessionIdentity.UserId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
        => deleteCommercialAccountUseCase.ExecuteAsync(sessionIdentity, occurredUtc, cancellationToken);

    /// <inheritdoc/>
    public Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
        => restoreCommercialAccountUseCase.ExecuteAsync(sessionIdentity, occurredUtc, cancellationToken);

    /// <inheritdoc/>
    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
        => purgeExpiredCommercialAccountsUseCase.ExecuteAsync(asOfUtc, batchSize, cancellationToken);
}
