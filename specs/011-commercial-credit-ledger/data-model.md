# Data Model: Commercial Credit Ledger

Credit persistence is commercial-only. It follows 008/009: models and store contracts in `ImportToPlanner.Commercial`; Azure Table adapters in Commercial; registered only when `Features:CommercialMode:Enabled` is true. Application does not own ledger entities. Self-host writes no credit rows.

Would-create and execution item shapes already exist on `ImportPlanPreview` / `ImportExecutionResult`. This feature adds tenant-scoped ledger entities plus structured quota/balance results.

## Ownership

| Entity | Owner | Notes |
|--------|--------|-------|
| `CreditLedgerTransaction` | Commercial | Append-only; source of remaining balance. |
| `CreditLot` | Commercial | Projection of an allocation; remaining derived from later usage/expiry. |
| `CreditMonthGrantMarker` | Commercial | Insert-only uniqueness for one free grant per tenant per UTC month. |
| `DerivedCreditBalance` | Commercial (computed) | Never persisted as an independently editable total. |
| `CommercialCreditBalanceSnapshot` | Commercial result | Structured remaining / grant applied; no UI text. |
| `ImportTaskCreationQuota` (port) | Application abstraction, Commercial implementation, no-op when commercial off | Create-loop Allow / Exhausted / Unavailable. |
| `CommercialAccount` | Commercial (unchanged) | Tenant identity already exists; ledger is keyed by `TenantId` only. |
| `SessionIdentityContext` | Application (unchanged) | Supplies `TenantId` for ledger scope. |

## 1. Commercial tenant (existing)

The organisation that owns the shared balance. Key: existing commercial `TenantId` from `SessionIdentityContext`. All users of the tenant share one ledger. Per-user wallets are out of scope.

Validation: ledger operations require a non-empty `TenantId`. Cross-tenant reads/writes are forbidden (FR-026).

## 2. Credit lot

A dated allocation of credits.

| Field | Type | Notes |
|-------|------|--------|
| `LotId` | string | Stable id (GUID). |
| `TenantId` | string | Partition. Required. |
| `LotType` | enum | `FreeMonthly` now. `Paid` reserved; unused this increment. |
| `GrantedQuantity` | int | 25 for V1 free grant; not prorated. |
| `RemainingQuantity` | int | Projection; 0 after full use or expiry. Must be ≥ 0. |
| `GrantedAtUtc` | `DateTimeOffset` | Actual grant instant (not backdated to month start). |
| `ExpiresAtUtc` | `DateTimeOffset` | Free: exclusive end of the UTC calendar month of `GrantedAtUtc` (first instant of next UTC month). Paid: reserved 12 months from purchase; no paid lots this increment. |
| `ETag` | storage concurrency token | Adapter-only; not a domain field on the public model if it would leak Azure types — keep on the table entity. |

Validation:

- `GrantedQuantity` > 0 at insert; `RemainingQuantity` between 0 and `GrantedQuantity`.
- Free lots expire at UTC month end of the grant month; no rollover.
- Consumption order: free lots with remaining > 0 first (any order among current-month free lots is acceptable while only one free lot exists), then paid lots oldest-first when they exist.

State:

- Open (`RemainingQuantity` > 0 and not yet expired).
- Consumed (`RemainingQuantity` = 0 via usage).
- Expired (`RemainingQuantity` reduced to 0 via expiry transaction).

## 3. Credit ledger transaction

Immutable balance-changing event.

| Field | Type | Notes |
|-------|------|--------|
| `TransactionId` | string | Stable id (GUID). |
| `TenantId` | string | Partition. Required. |
| `OccurredUtc` | `DateTimeOffset` | Required. |
| `EntryType` | enum | `FreeGrant`, `Usage`, `FreeExpiry`. Reserved: `PaidPurchase`, `PaidExpiry` (must not be written this increment). |
| `Quantity` | int | Absolute credits moved. Grant/purchase increase; usage/expiry decrease. |
| `LotId` | string | Lot this entry applies to. |
| `LotType` | enum | Denormalised for later free vs paid summary. |
| `ImportRunId` | string? | Required for `Usage`; optional otherwise. |
| `CreatedPlannerTaskId` | string? | For `Usage`; the Planner task that consumed the credit. |
| `ActorUserId` | string? | Session user for support; not a wallet key. |

Validation:

- Past rows are never updated or deleted by product flows (retention/purge of commercial accounts is a later concern; this increment does not add a credit purge job).
- Corrections are additional compensating transactions (none required in V1 happy path).
- `Usage` quantity is 1 per successfully created **task** (not bucket, not reused/skip/fail).
- `FreeGrant` quantity is 25; at most one successful `FreeGrant` per tenant per UTC month (enforced by month marker).
- `FreeExpiry` quantity equals leftover remaining on that free lot; leftover must not appear in the next month’s derived balance.

## 4. Credit month grant marker

Insert-only uniqueness row.

| Field | Type | Notes |
|-------|------|--------|
| `TenantId` | string | Partition. |
| `UtcYearMonth` | string | `yyyyMM` in UTC. |
| `GrantedAtUtc` | `DateTimeOffset` | Instant of the winning insert. |
| `LotId` | string | Lot created with the grant. |

Validation: second insert for the same tenant and month must fail; callers treat that as “already granted” and must not write a second `FreeGrant`.

Restored commercial access in the same calendar month as an earlier grant does not receive a second grant.

## 5. Derived credit balance

Computed from open lots (or equivalently from summing transactions). Fields on the structured snapshot:

| Field | Type | Notes |
|-------|------|--------|
| `RemainingCredits` | int | ≥ 0. Sum of open lot remaining. |
| `FreeRemaining` | int | For honest “free monthly” usage on the summary. |
| `PaidRemaining` | int | Always 0 this increment. |
| `WouldCreateCount` | int? | Set when comparing a preview. |
| `Shortfall` | int? | `max(0, N - M)` when comparing. |
| `InsufficientForConfirm` | bool | `WouldCreateCount > RemainingCredits`. |
| `GrantAppliedThisCall` | bool | Diagnostics; not UI copy. |
| `ExpiryAppliedThisCall` | bool | Diagnostics. |

Never persist this object as the source of truth. Never invent remaining = 0 when the ledger cannot be read.

## 6. Would-create count (existing preview)

Definition for this feature: number of `ImportPlanPreview.TaskActions` whose `Action` is `PlannedEntityAction.Create`.

Not: CSV row count, reused/skipped tasks, bucket creates, or plan reuse.

Preview always runs (when planning succeeds) and never writes `Usage`. Preview and confirm may write grant/expiry via `EnsureCurrentBalance` only. CSV parse/validation failure must not call `EnsureCurrentBalance`.

## 7. Import usage and credit-exhausted outcome

During execute, each successful `CreateTaskAsync` records one `Usage` against a free lot (paid later) **before** the next create starts.

| Execution outcome | Ledger | Planner | Summary |
|-------------------|--------|---------|---------|
| Task created, usage recorded | +1 usage | Task remains | Counts toward created and credits used |
| Credits remaining hit 0 before a create | No usage for that row | Not created | Visible credit-exhausted outcome, diagnostic `credits.exhausted` |
| Task created, usage record fails after retry | No successful usage (or partial retry then stop) | Task remains | Run stops; recording-failure diagnostic `credits.usage_record_failed`; remaining Create rows not started and visible |
| Reuse / skip / failed create | No usage | Unchanged | No credit line |
| Preview / validation | No usage | N/A | Balance unchanged except due grant/expiry on preview |

`ImportExecutionOutcomeSummary.CreatedCount` today includes buckets and tasks. Credits used MUST count **task** creates only. Summary presenter maps task-created count, credits used (free monthly this increment), and ledger remaining.

## 8. Azure Table mapping (adapter)

Single table (name from `Storage:CommercialCreditLedgerTable`, suggested default `CommercialCreditLedger`).

| Row kind | PartitionKey | RowKey | Notes |
|----------|--------------|--------|--------|
| Transaction | `TenantId` | `tx|{reverseTicks}|{transactionId}` | Append via `AddEntity`. |
| Lot projection | `TenantId` | `lot|{lotId}` | ETag; remaining updated only in the same EGT as the matching tx. |
| Month marker | `TenantId` | `grant|{yyyyMM}` | `AddEntity` uniqueness. |

All credit EGT batches stay in one tenant partition. Adapters catch 409/412 and retry or treat as already-granted. No `Azure.Data.Tables` types on Commercial models or Application.

## 9. State transitions (`EnsureCurrentBalance`)

Clock: `DateTimeOffset.UtcNow` (injectable clock in tests).

1. Load lots/transactions for `TenantId`.
2. For each free lot with `RemainingQuantity` > 0 and `ExpiresAtUtc` ≤ start of current UTC month: append `FreeExpiry` for leftover, set remaining 0.
3. If no month marker for current UTC `yyyyMM`: insert marker + lot + `FreeGrant` (25) dated at this instant. If marker insert conflicts, skip grant.
4. Return derived remaining. If any step fails, return structured failure; write no grant.

Called from: successful commercial sign-in (Allow / CreateAccount; failure does not block session), preview, confirm, execute. Not called from: commercial-off, deleted-account gate, CSV validation-only, Profile delete/restore except that restore in a month that already granted must not grant again (Ensure on next balance-needed action is enough).

## 10. Application quota port (not a ledger entity)

`IImportTaskCreationQuota`:

- Self-host / commercial off: always `Allow`; `RecordSuccessfulCreate` is no-op.
- Commercial: `BeforeCreate` returns `Allow` if remaining > 0 after ensure+read, `Exhausted` if remaining = 0, `Unavailable` if ledger cannot be used (fail closed — execution must not start or must stop without treating remaining as zero).
- `RecordSuccessfulCreate` writes `Usage` (retry once internally); failure surfaces as recording failure to the use case.

No Stripe customer ids, checkout session ids, or webhook payloads on any entity.
