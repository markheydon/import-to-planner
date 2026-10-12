# Profile Credits and Ledger Activity Contracts

Boundary contracts for the `/profile` credits section (spec 020, issue [#138](https://github.com/markheydon/import-to-planner/issues/138)). Extends [commercial-credit-ledger-contracts.md](../../011-commercial-credit-ledger/contracts/commercial-credit-ledger-contracts.md) and [complimentary-tenant-credits-contracts.md](../../019-complimentary-tenant-credits/contracts/complimentary-tenant-credits-contracts.md). Where this document is silent, 011/019 rules still apply.

## 1. Composition and visibility

When `Features:CommercialMode:Enabled` is **true**:

1. Register profile credit read use cases and extended ledger list API in Commercial.
2. `/profile` MAY load credit summary and activity for the signed-in tenant.
3. Do not register Stripe metering or portal links for statement display (FR-009).

When **false**:

1. Do not register profile credit use cases.
2. `/profile` MUST NOT query the credit ledger or show credit copy (FR-001).
3. Home MUST NOT gain persistent credit wallet chrome.

## 2. Ensure current balance (profile amendment)

Add audit value `EnsureBalanceReason.Profile` (amend 011 `EnsureCurrentCreditBalanceRequest.Reason` enum and [commercial-credit-ledger-contracts.md](../../011-commercial-credit-ledger/contracts/commercial-credit-ledger-contracts.md) §2 when implementing T001).

| Caller | Behaviour |
|--------|-----------|
| `/profile` on load | Call ensure with `Profile` reason; for non-complimentary tenants, same expiry/grant/balance rules as preview (011). For complimentary tenants, apply [019 §3](../../019-complimentary-tenant-credits/contracts/complimentary-tenant-credits-contracts.md) (skip free grant/expiry; `ComplimentaryUnlimitedAccess`). |
| Import preview/confirm/execute | Unchanged. |

Complimentary tenants on profile: ensure follows 019 §3; UI shows unlimited remaining and usage-only activity with no free-monthly or paid lot breakdown (FR-005, FR-006, FR-010).

Failure on profile: structured failure code only from Commercial; Web shows UK English error; MUST NOT show remaining as `0` or insufficient-credits messaging (FR-004).

## 3. Profile credit summary use case

`IGetProfileCreditSummaryUseCase.GetAsync(ProfileCreditSummaryRequest, CancellationToken)`

Request:

| Field | Type | Required |
|-------|------|----------|
| `TenantId` | string | Yes (session) |
| `ActorUserId` | string | Yes (session) |
| `OccurredUtc` | `DateTimeOffset` | Yes (typically UTC now) |

Response (success): `ProfileCreditSummaryResult` per [data-model.md](../data-model.md).

| Complimentary tenant | Non-complimentary |
|----------------------|-------------------|
| `ComplimentaryUnlimitedAccess = true` | Numeric `RemainingCredits` |
| No `FreeMonthlyLot` or `PaidLots` in UI | `FreeMonthlyLot` populated when current month lot exists |
| Remaining display **Unlimited** | Free lot granted/remaining/expiry shown |

Response (failure): `CommercialCreditBalanceFailure` with stable code; no UI text in Commercial.

## 4. Profile credit activity use case

`IGetProfileCreditActivityUseCase.GetAsync(ProfileCreditActivityRequest, CancellationToken)`

Request: same identity fields as summary.

Complimentary filtering: resolve allowlist membership via `IComplimentaryTenantPolicy.IsComplimentary(TenantId)` on each call (019 §2); do not infer from ledger rows alone.

Response (success):

| Field | Meaning |
|-------|---------|
| `Entries` | Newest-first within rolling twelve months |
| `HasOlderHistory` | true when ledger has events before window (for omission copy) |

Entry types by tenant:

| Entry type | Non-complimentary | Complimentary |
|------------|-------------------|---------------|
| Free monthly grant | Yes | No |
| Free credit expiry | Yes | No |
| Import run usage (grouped) | Yes | Yes (only type) |
| Paid purchase | When [#125](https://github.com/markheydon/import-to-planner/issues/125) writes ledger | No |
| Paid credit lot expiry | When [#125](https://github.com/markheydon/import-to-planner/issues/125) writes `PaidExpiry` ledger rows | No |
| Complimentary grant | Never as its own row | Never |

Grouping: one row per `ImportRunId` for usage when the id is present; quantity = total credits consumed in that run (FR-007). Usage rows without `ImportRunId` appear as one grouped usage row each (legacy fallback; not one row per task).

Response (failure): `CommercialCreditBalanceFailure` with stable code (typically `credits.ledger_unavailable`); independent of summary success/failure; no UI text in Commercial.

## 5. Ledger store list contract (amendment)

`ICreditLedgerStore.ListTransactionsAsync(tenantId, occurredSinceUtc, cancellationToken)` → `IReadOnlyList<CreditLedgerTransaction>`

- Scope: single tenant partition.
- Filter: `OccurredUtc >= occurredSinceUtc`.
- Ordering: use case sorts newest-first.
- Read-only; no mutation.

`ICreditLedgerStore.HasTransactionsBeforeAsync(tenantId, beforeOccurredUtc, cancellationToken)` → `bool`

- Scope: same tenant partition; `tx|` row prefix.
- Returns true when any transaction has `OccurredUtc < beforeOccurredUtc`.
- Used by the activity use case to set `HasOlderHistory` without loading pre-window rows.
- Read-only; no mutation.

## 6. Web presentation (`/profile`)

| Region | Content |
|--------|---------|
| Remaining | Unlimited or integer; never `0` on read failure |
| Free monthly lot | Granted, remaining, expiry (non-complimentary only) |
| Paid lots | When [#125](https://github.com/markheydon/import-to-planner/issues/125) available; non-complimentary only |
| Activity table/list | Labels from `ProfileCreditPresenter` (UK English) |
| Older history | Visible copy when `HasOlderHistory` (FR-006) |
| Buy credits | Link/button only when [#125](https://github.com/markheydon/import-to-planner/issues/125) shipped and tenant not complimentary (FR-011) |

MUST NOT expose: Stripe customer ids, stack traces, internal tenant diagnostics (FR-008).

## 7. Security and tenancy

- All operations require authenticated commercial session with resolved `TenantId`.
- Activity and summary for tenant A MUST NOT return tenant B data (FR-002).
- Profile is end-user scope only; no operator ledger browser (FR-013).

## 8. Non-goals (contract)

- Changing `IImportTaskCreationQuota`, coordinator confirm gates, or execution metering (FR-012).
- Home header wallet.
- Self-hosted credit UI or ledger writes.
