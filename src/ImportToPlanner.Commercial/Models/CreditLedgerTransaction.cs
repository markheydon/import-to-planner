namespace ImportToPlanner.Commercial.Models;

/// <summary>
/// Immutable credit balance-changing ledger event.
/// </summary>
/// <param name="TransactionId">Stable transaction identifier.</param>
/// <param name="TenantId">Owning tenant partition key.</param>
/// <param name="OccurredUtc">When the event occurred.</param>
/// <param name="EntryType">Ledger entry type.</param>
/// <param name="Quantity">Absolute credits moved.</param>
/// <param name="LotId">Lot this entry applies to.</param>
/// <param name="LotType">Denormalised lot type.</param>
/// <param name="ImportRunId">Import run identifier for usage entries.</param>
/// <param name="CreatedPlannerTaskId">Planner task identifier for usage entries.</param>
/// <param name="ActorUserId">Session user for audit.</param>
public sealed record CreditLedgerTransaction(
    string TransactionId,
    string TenantId,
    DateTimeOffset OccurredUtc,
    CreditEntryType EntryType,
    int Quantity,
    string LotId,
    LotType LotType,
    string? ImportRunId = null,
    string? CreatedPlannerTaskId = null,
    string? ActorUserId = null);
