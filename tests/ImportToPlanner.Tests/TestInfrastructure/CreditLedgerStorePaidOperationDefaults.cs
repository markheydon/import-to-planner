using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Tests.TestInfrastructure;

/// <summary>
/// Default paid-ledger operations for test doubles that only override free-credit behaviour.
/// </summary>
public static class CreditLedgerStorePaidOperationDefaults
{
    public static Task<IReadOnlyList<CreditLot>> ListDuePaidCreditLotsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<CreditLot>>([]);

    public static Task<CreditLot?> GetPaidLotByExternalPaymentIdAsync(
        string tenantId,
        string externalPaymentId,
        CancellationToken cancellationToken)
        => Task.FromResult<CreditLot?>(null);

    public static Task<CreditGrantAttemptOutcome> TryGrantPaidPurchaseAsync(
        string tenantId,
        string externalPaymentId,
        string externalEventId,
        int grantQuantity,
        DateTimeOffset grantedAtUtc,
        string actorUserId,
        CancellationToken cancellationToken)
        => Task.FromResult<CreditGrantAttemptOutcome>(
            new CreditGrantAttemptOutcome.Failure(CommercialCreditFailureCodes.GrantFailed));

    public static Task<bool> ExpirePaidLotAsync(
        CreditLot lot,
        DateTimeOffset occurredUtc,
        string? actorUserId,
        CancellationToken cancellationToken)
        => Task.FromResult(true);

    public static Task<PaidCreditAdjustmentAttemptOutcome> TryAdjustPaidCreditsAsync(
        string tenantId,
        string externalPaymentId,
        string externalAdjustmentId,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
        => Task.FromResult<PaidCreditAdjustmentAttemptOutcome>(
            new PaidCreditAdjustmentAttemptOutcome.Failure(PaidCreditPurchaseFailureCodes.LedgerCommitFailed));
}
