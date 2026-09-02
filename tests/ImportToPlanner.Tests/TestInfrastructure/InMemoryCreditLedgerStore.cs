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
        var state = GetTenantState(request.TenantId);
        lock (state.Sync)
        {
            var lot = state.Lots.Values
                .Where(candidate => candidate.RemainingQuantity > 0)
                .OrderBy(candidate => candidate.GrantedAtUtc)
                .FirstOrDefault(candidate => candidate.LotType == LotType.FreeMonthly)
                ?? state.Lots.Values
                    .Where(candidate => candidate.RemainingQuantity > 0)
                    .OrderBy(candidate => candidate.GrantedAtUtc)
                    .FirstOrDefault();

            if (lot is null || lot.RemainingQuantity <= 0)
            {
                return Task.FromResult<RecordCreditUsageOutcome>(
                    new RecordCreditUsageOutcome.Failure(CommercialCreditFailureCodes.Exhausted));
            }

            state.Lots[lot.LotId] = lot with { RemainingQuantity = lot.RemainingQuantity - 1 };
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

            var remaining = state.Lots.Values.Sum(candidate => candidate.RemainingQuantity);
            return Task.FromResult<RecordCreditUsageOutcome>(new RecordCreditUsageOutcome.Success(remaining));
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

    private TenantLedgerState GetTenantState(string tenantId)
        => tenants.GetOrAdd(tenantId, _ => new TenantLedgerState());

    private sealed class TenantLedgerState
    {
        public object Sync { get; } = new();

        public Dictionary<string, CreditLot> Lots { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, CreditMonthGrantMarker> Markers { get; } = new(StringComparer.Ordinal);

        public List<CreditLedgerTransaction> Transactions { get; } = [];
    }
}
