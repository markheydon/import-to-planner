# Data Model: Profile Credits and Ledger Activity

Read-only commercial profile projections over the immutable ledger (011). No new persisted entities; optional new Commercial result types and Web view models. Self-host writes and reads no credit rows.

## Ownership

| Concept | Owner | Notes |
|---------|--------|-------|
| `CreditLedgerTransaction`, `CreditLot` | Commercial (011) | Unchanged; profile reads only. |
| `CommercialCreditBalanceResult` | Commercial (011/019) | Reused after ensure on profile. |
| `ProfileCreditSummaryResult` | Commercial | Balance + optional lot breakdown for UI mapping. |
| `ProfileCreditActivityEntry` | Commercial | User-facing row before UK English labels. |
| `ProfileCreditPresenter` | Web | Maps results to display strings; no ledger access. |
| `CommercialAccount` | Commercial (008) | Unchanged; credits section is additive on same page. |

## 1. Profile credit summary (computed)

Produced after `EnsureCurrentCreditBalance` succeeds for the signed-in tenant.

| Field | Type | Notes |
|-------|------|--------|
| `RemainingCredits` | int | From ensure result when not complimentary. |
| `ComplimentaryUnlimitedAccess` | bool | From ensure (019). |
| `FreeMonthlyLot` | `ProfileFreeMonthlyLot?` | Null when complimentary or no current-month free lot. |
| `PaidLots` | `IReadOnlyList<ProfilePaidLot>` | Empty until [#125](https://github.com/markheydon/import-to-planner/issues/125); hidden in UI when complimentary. |
| `EnsureExpiryApplied` | bool | Optional for activity correlation; not required on UI. |
| `EnsureGrantApplied` | bool | Optional for activity correlation. |

### ProfileFreeMonthlyLot

| Field | Type | Validation |
|-------|------|------------|
| `GrantedQuantity` | int | > 0; typically 25 (policy constant). |
| `RemainingQuantity` | int | 0..GrantedQuantity. |
| `ExpiresAtUtc` | `DateTimeOffset` | UTC month end for grant month. |
| `GrantedAtUtc` | `DateTimeOffset` | Actual grant instant. |

Selection rule: after ensure at `occurredUtc`, the lot is the `FreeMonthly` lot whose grant month equals `GetUtcYearMonth(occurredUtc)` with highest `GrantedAtUtc` if duplicates exist (should not happen with marker).

### ProfilePaidLot (reserved for #125)

| Field | Type | Notes |
|-------|------|--------|
| `LotId` | string | Stable. |
| `GrantedQuantity` | int | Purchase size. |
| `RemainingQuantity` | int | Open balance. |
| `GrantedAtUtc` | `DateTimeOffset` | Purchase instant. |
| `ExpiresAtUtc` | `DateTimeOffset` | Twelve months from purchase per product doc. |

## 2. Profile credit activity entry (computed)

| Field | Type | Notes |
|-------|------|--------|
| `OccurredUtc` | `DateTimeOffset` | Sort key; for grouped usage, latest usage in run. |
| `ActivityKind` | enum | `FreeMonthlyGrant`, `FreeCreditExpiry`, `ImportRunUsage`, `PaidPurchase` (future), `PaidLotExpiry` (future). |
| `Quantity` | int | Absolute credits for the row (sum for grouped usage). |
| `ImportRunId` | string? | Set for `ImportRunUsage`. |
| `IsCreditIncrease` | bool | true for grants/purchases; false for usage/expiry. |

### Grouping: import run usage

- Input: `Usage` transactions with same `ImportRunId` within the twelve-month window.
- Output: one `ImportRunUsage` entry; `Quantity` = sum of usage quantities; exclude complimentary-only pairs from duplicate counting (usage rows still count; grant rows never become profile rows).

### Window and ordering

- `ActivityWindowStartUtc` = `occurredUtc.AddMonths(-12)` (rolling).
- Sort: `OccurredUtc` descending.
- `HasOlderHistory` = any ledger transaction with `OccurredUtc < ActivityWindowStartUtc` (boolean for copy only).

## 3. Failure models

| Outcome | Code (examples) | UI rule |
|---------|-----------------|--------|
| Summary failed | `credits.ledger_unavailable`, `credits.grant_failed`, `credits.expiry_failed` | No remaining figure; friendly error. |
| Activity failed | `credits.ledger_unavailable` | Activity error; summary may still show if succeeded independently. |

## 4. State transitions (read path)

```text
Profile load (commercial on, authenticated)
  → Ensure balance (Profile reason)
       → fail → summary error state
       → success → map lots + remaining
  → List transactions (12m window) + aggregate
       → fail → activity error state
       → success → activity list + optional "older history omitted" flag
```

No writes except those already performed by ensure (grant/expiry).

## 5. Relationships

```text
CommercialAccount (1) ── tenantId ──> (1) Credit ledger partition
                                              ├── CreditLot (*)
                                              └── CreditLedgerTransaction (*)
ProfileCreditSummaryResult ──derived──> Ensure + lots
ProfileCreditActivityEntry (*) ──derived──> transactions (+ grouping)
```

Cross-tenant access is forbidden: `TenantId` always from `SessionIdentityContext`, never from client input (FR-002, constitution VIII).
