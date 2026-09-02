using System.Collections.Concurrent;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Models;

namespace ImportToPlanner.Tests.TestInfrastructure;

/// <summary>
/// In-memory credit ledger store for unit tests.
/// </summary>
public sealed class InMemoryCreditLedgerStore : ICreditLedgerStore
{
    private readonly ConcurrentDictionary<string, TenantLedgerState> tenants = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<IReadOnlyList<CreditLot>> GetLotsAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            return Task.FromResult<IReadOnlyList<CreditLot>>(state.Lots.Values.ToList());
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasMonthGrantMarkerAsync(
        string tenantId,
        string utcYearMonth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            return Task.FromResult(state.Markers.ContainsKey(utcYearMonth));
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExpireFreeLotAsync(
        CreditLot lot,
        DateTimeOffset occurredUtc,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(lot.TenantId);
        lock (state.Sync)
        {
            if (!state.Lots.TryGetValue(lot.LotId, out var current) || current.RemainingQuantity <= 0)
            {
                return Task.FromResult(true);
            }

            var quantity = current.RemainingQuantity;
            state.Lots[lot.LotId] = current with { RemainingQuantity = 0 };
            state.Transactions.Add(new CreditLedgerTransaction(
                Guid.NewGuid().ToString("N"),
                lot.TenantId,
                occurredUtc,
                CreditEntryType.FreeExpiry,
                quantity,
                lot.LotId,
                lot.LotType,
                ActorUserId: actorUserId));
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<CreditGrantAttemptOutcome> TryGrantFreeMonthlyAsync(
        string tenantId,
        string utcYearMonth,
        int grantQuantity,
        DateTimeOffset grantedAtUtc,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            if (state.Markers.ContainsKey(utcYearMonth))
            {
                return Task.FromResult<CreditGrantAttemptOutcome>(new CreditGrantAttemptOutcome.AlreadyGranted());
            }

            var lotId = Guid.NewGuid().ToString("N");
            state.Markers[utcYearMonth] = new CreditMonthGrantMarker(tenantId, utcYearMonth, grantedAtUtc, lotId);
            state.Lots[lotId] = new CreditLot(
                lotId,
                tenantId,
                LotType.FreeMonthly,
                grantQuantity,
                grantQuantity,
                grantedAtUtc,
                CommercialCreditPolicy.GetFreeLotExpiresAtUtc(grantedAtUtc));
            state.Transactions.Add(new CreditLedgerTransaction(
                Guid.NewGuid().ToString("N"),
                tenantId,
                grantedAtUtc,
                CreditEntryType.FreeGrant,
                grantQuantity,
                lotId,
                LotType.FreeMonthly,
                ActorUserId: actorUserId));
            return Task.FromResult<CreditGrantAttemptOutcome>(new CreditGrantAttemptOutcome.Applied());
        }
    }

    /// <inheritdoc/>
    public Task<RecordCreditUsageOutcome> RecordUsageAsync(
        RecordCreditUsageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ImportRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CreatedPlannerTaskId);

        var state = GetTenantState(request.TenantId);
        lock (state.Sync)
        {
            var idempotencyKey = BuildUsageIdempotencyKey(request.ImportRunId, request.CreatedPlannerTaskId);
            if (state.UsageIdempotencyKeys.Contains(idempotencyKey))
            {
                var remaining = CreditLotSelector.SumConsumableRemaining(state.Lots.Values.ToList(), request.OccurredUtc);
                return Task.FromResult<RecordCreditUsageOutcome>(new RecordCreditUsageOutcome.Success(remaining));
            }

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var lot = CreditLotSelector.SelectConsumableLot(state.Lots.Values.ToList(), request.OccurredUtc);
                if (lot is null)
                {
                    return Task.FromResult<RecordCreditUsageOutcome>(
                        new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.Exhausted));
                }

                if (!state.Lots.TryGetValue(lot.LotId, out var current)
                    || current.RemainingQuantity <= 0
                    || !CreditLotSelector.IsConsumable(current, request.OccurredUtc))
                {
                    continue;
                }

                state.UsageIdempotencyKeys.Add(idempotencyKey);
                state.Lots[lot.LotId] = current with { RemainingQuantity = current.RemainingQuantity - 1 };
                state.Transactions.Add(new CreditLedgerTransaction(
                    Guid.NewGuid().ToString("N"),
                    request.TenantId,
                    request.OccurredUtc,
                    CreditEntryType.Usage,
                    1,
                    lot.LotId,
                    lot.LotType,
                    request.ImportRunId,
                    request.CreatedPlannerTaskId,
                    request.ActorUserId));

                var remaining = CreditLotSelector.SumConsumableRemaining(state.Lots.Values.ToList(), request.OccurredUtc);
                return Task.FromResult<RecordCreditUsageOutcome>(new RecordCreditUsageOutcome.Success(remaining));
            }

            return Task.FromResult<RecordCreditUsageOutcome>(
                new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.Exhausted));
        }
    }

    /// <summary>
    /// Gets recorded transactions for assertions.
    /// </summary>
    public IReadOnlyList<CreditLedgerTransaction> GetTransactions(string tenantId)
    {
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            return state.Transactions.ToList();
        }
    }

    private static string BuildUsageIdempotencyKey(string importRunId, string createdPlannerTaskId)
        => $"{importRunId}|{createdPlannerTaskId}";

    private TenantLedgerState GetTenantState(string tenantId)
        => tenants.GetOrAdd(tenantId, _ => new TenantLedgerState());

    private sealed class TenantLedgerState
    {
        public object Sync { get; } = new();

        public Dictionary<string, CreditLot> Lots { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, CreditMonthGrantMarker> Markers { get; } = new(StringComparer.Ordinal);

        public HashSet<string> UsageIdempotencyKeys { get; } = new(StringComparer.Ordinal);

        public List<CreditLedgerTransaction> Transactions { get; } = [];
    }
}
