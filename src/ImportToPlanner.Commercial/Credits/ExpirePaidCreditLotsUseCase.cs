using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Models;
using Microsoft.Extensions.Logging;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Expires unused quantity on paid lots whose twelve-month window has ended.
/// </summary>
public sealed class ExpirePaidCreditLotsUseCase(
    ICreditLedgerStore ledgerStore,
    IUtcClock clock,
    ILogger<ExpirePaidCreditLotsUseCase> logger) : IExpirePaidCreditLotsUseCase
{
    /// <inheritdoc/>
    public async Task<ExpirePaidCreditLotsResult> ExecuteAsync(
        DateTimeOffset? asOfUtc,
        CancellationToken cancellationToken)
    {
        var effectiveAsOfUtc = asOfUtc ?? clock.UtcNow;
        IReadOnlyList<CreditLot> dueLots;
        try
        {
            dueLots = await ledgerStore
                .ListDuePaidCreditLotsAsync(effectiveAsOfUtc, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Paid credit expiry sweep could not list due lots at {AsOfUtc}.", effectiveAsOfUtc);
            return new ExpirePaidCreditLotsResult(0, 1);
        }

        var expiredCount = 0;
        var failureCount = 0;
        foreach (var lot in dueLots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var expired = await ledgerStore
                    .ExpirePaidLotAsync(lot, effectiveAsOfUtc, actorUserId: null, cancellationToken)
                    .ConfigureAwait(false);
                if (expired)
                {
                    expiredCount++;
                }
                else
                {
                    failureCount++;
                    logger.LogWarning(
                        "Paid credit expiry failed for tenant {TenantId} lot {LotId} at {AsOfUtc}.",
                        lot.TenantId,
                        lot.LotId,
                        effectiveAsOfUtc);
                }
            }
            catch (Exception exception)
            {
                failureCount++;
                logger.LogError(
                    exception,
                    "Paid credit expiry threw for tenant {TenantId} lot {LotId} at {AsOfUtc}.",
                    lot.TenantId,
                    lot.LotId,
                    effectiveAsOfUtc);
            }
        }

        return new ExpirePaidCreditLotsResult(expiredCount, failureCount);
    }
}

/// <summary>
/// Port for the paid credit expiry sweep (timer adapter and tests).
/// </summary>
public interface IExpirePaidCreditLotsUseCase
{
    /// <summary>
    /// Expires due paid lots as of the supplied instant, or the injected clock when omitted.
    /// </summary>
    Task<ExpirePaidCreditLotsResult> ExecuteAsync(DateTimeOffset? asOfUtc, CancellationToken cancellationToken);
}

/// <summary>
/// Outcome counters for a paid credit expiry sweep.
/// </summary>
/// <param name="ExpiredLotCount">Lots successfully expired.</param>
/// <param name="FailureCount">Lots that could not be expired.</param>
public sealed record ExpirePaidCreditLotsResult(int ExpiredLotCount, int FailureCount);
