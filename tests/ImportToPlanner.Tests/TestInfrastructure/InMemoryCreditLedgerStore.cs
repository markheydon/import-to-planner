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
                return Task.FromResult<RecordCreditUsageOutcome>(
                    new RecordCreditUsageOutcome.Success(remaining, ConsumedPaidCredit: false));
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
                return Task.FromResult<RecordCreditUsageOutcome>(
                    new RecordCreditUsageOutcome.Success(remaining, ConsumedPaidCredit: lot.LotType == LotType.Paid));
            }

            return Task.FromResult<RecordCreditUsageOutcome>(
                new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.Exhausted));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CreditLot>> ListDuePaidCreditLotsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dueLots = tenants.Values
            .SelectMany(tenant => tenant.Lots.Values)
            .Where(lot => lot.LotType == LotType.Paid
                && lot.RemainingQuantity > 0
                && lot.ExpiresAtUtc <= asOfUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<CreditLot>>(dueLots);
    }

    /// <inheritdoc/>
    public Task<RecordCreditUsageOutcome> RecordComplimentaryUsageAsync(
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
                return Task.FromResult<RecordCreditUsageOutcome>(
                    new RecordCreditUsageOutcome.Success(0, ConsumedPaidCredit: false));
            }

            var lotId = ComplimentaryCreditConstants.ComplimentaryLotId;
            if (!state.Lots.TryGetValue(lotId, out var complimentaryLot))
            {
                complimentaryLot = new CreditLot(
                    lotId,
                    request.TenantId,
                    LotType.Complimentary,
                    0,
                    0,
                    request.OccurredUtc,
                    DateTimeOffset.MaxValue);
                state.Lots[lotId] = complimentaryLot;
            }

            state.UsageIdempotencyKeys.Add(idempotencyKey);
            state.Transactions.Add(new CreditLedgerTransaction(
                Guid.NewGuid().ToString("N"),
                request.TenantId,
                request.OccurredUtc,
                CreditEntryType.Usage,
                1,
                complimentaryLot.LotId,
                LotType.Complimentary,
                request.ImportRunId,
                request.CreatedPlannerTaskId,
                request.ActorUserId));
            state.Transactions.Add(new CreditLedgerTransaction(
                Guid.NewGuid().ToString("N"),
                request.TenantId,
                request.OccurredUtc,
                CreditEntryType.ComplimentaryGrant,
                1,
                complimentaryLot.LotId,
                LotType.Complimentary,
                request.ImportRunId,
                request.CreatedPlannerTaskId,
                request.ActorUserId));

            return Task.FromResult<RecordCreditUsageOutcome>(new RecordCreditUsageOutcome.Success(0, ConsumedPaidCredit: false));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CreditLedgerTransaction>> ListTransactionsAsync(
        string tenantId,
        DateTimeOffset occurredSinceUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            var transactions = state.Transactions
                .Where(transaction => transaction.OccurredUtc >= occurredSinceUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<CreditLedgerTransaction>>(transactions);
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasTransactionsBeforeAsync(
        string tenantId,
        DateTimeOffset beforeOccurredUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            var hasOlder = state.Transactions.Any(transaction => transaction.OccurredUtc < beforeOccurredUtc);
            return Task.FromResult(hasOlder);
        }
    }

    /// <inheritdoc/>
    public Task<CreditLot?> GetPaidLotByExternalPaymentIdAsync(
        string tenantId,
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            var lotId = CommercialCreditPolicy.BuildPaidLotId(externalPaymentId);
            state.Lots.TryGetValue(lotId, out var lot);
            return Task.FromResult(lot);
        }
    }

    /// <inheritdoc/>
    public Task<CreditGrantAttemptOutcome> TryGrantPaidPurchaseAsync(
        string tenantId,
        string externalPaymentId,
        string externalEventId,
        int grantQuantity,
        DateTimeOffset grantedAtUtc,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            if (state.PaymentMarkers.ContainsKey(externalPaymentId)
                || state.EventMarkers.ContainsKey(externalEventId))
            {
                return Task.FromResult<CreditGrantAttemptOutcome>(new CreditGrantAttemptOutcome.AlreadyGranted());
            }

            var lotId = CommercialCreditPolicy.BuildPaidLotId(externalPaymentId);
            state.PaymentMarkers[externalPaymentId] = grantedAtUtc;
            state.EventMarkers[externalEventId] = grantedAtUtc;
            state.Lots[lotId] = new CreditLot(
                lotId,
                tenantId,
                LotType.Paid,
                grantQuantity,
                grantQuantity,
                grantedAtUtc,
                CommercialCreditPolicy.GetPaidLotExpiresAtUtc(grantedAtUtc),
                externalPaymentId);
            state.Transactions.Add(new CreditLedgerTransaction(
                Guid.NewGuid().ToString("N"),
                tenantId,
                grantedAtUtc,
                CreditEntryType.PaidPurchase,
                grantQuantity,
                lotId,
                LotType.Paid,
                ActorUserId: actorUserId));
            return Task.FromResult<CreditGrantAttemptOutcome>(new CreditGrantAttemptOutcome.Applied());
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExpirePaidLotAsync(
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
                CreditEntryType.PaidExpiry,
                quantity,
                lot.LotId,
                lot.LotType,
                ActorUserId: actorUserId));
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<PaidCreditAdjustmentAttemptOutcome> TryAdjustPaidCreditsAsync(
        string tenantId,
        string externalPaymentId,
        string externalAdjustmentId,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = GetTenantState(tenantId);
        lock (state.Sync)
        {
            if (state.AdjustmentMarkers.ContainsKey(externalAdjustmentId))
            {
                return Task.FromResult<PaidCreditAdjustmentAttemptOutcome>(
                    new PaidCreditAdjustmentAttemptOutcome.AlreadyAdjusted());
            }

            var lotId = CommercialCreditPolicy.BuildPaidLotId(externalPaymentId);
            if (!state.Lots.TryGetValue(lotId, out var lot))
            {
                return Task.FromResult<PaidCreditAdjustmentAttemptOutcome>(
                    new PaidCreditAdjustmentAttemptOutcome.Failure(PaidCreditPurchaseFailureCodes.PaidLotNotFound));
            }

            var quantityToRemove = Math.Max(0, lot.RemainingQuantity);
            state.AdjustmentMarkers[externalAdjustmentId] = occurredUtc;
            state.Lots[lotId] = lot with { RemainingQuantity = lot.RemainingQuantity - quantityToRemove };
            state.Transactions.Add(new CreditLedgerTransaction(
                Guid.NewGuid().ToString("N"),
                tenantId,
                occurredUtc,
                CreditEntryType.PaidAdjustment,
                quantityToRemove,
                lotId,
                LotType.Paid));
            return Task.FromResult<PaidCreditAdjustmentAttemptOutcome>(
                new PaidCreditAdjustmentAttemptOutcome.Applied());
        }
    }

    /// <summary>
    /// Seeds a lot for test scenarios (for example paid purchases before store grant APIs exist).
    /// </summary>
    public void SeedLot(CreditLot lot)
    {
        ArgumentNullException.ThrowIfNull(lot);
        var state = GetTenantState(lot.TenantId);
        lock (state.Sync)
        {
            state.Lots[lot.LotId] = lot;
        }
    }

    /// <summary>
    /// Seeds a ledger transaction for test scenarios (for example pre-window history or paid entries).
    /// </summary>
    public void SeedTransaction(CreditLedgerTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var state = GetTenantState(transaction.TenantId);
        lock (state.Sync)
        {
            state.Transactions.Add(transaction);
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

        public Dictionary<string, DateTimeOffset> PaymentMarkers { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, DateTimeOffset> EventMarkers { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, DateTimeOffset> AdjustmentMarkers { get; } = new(StringComparer.Ordinal);

        public List<CreditLedgerTransaction> Transactions { get; } = [];
    }
}
