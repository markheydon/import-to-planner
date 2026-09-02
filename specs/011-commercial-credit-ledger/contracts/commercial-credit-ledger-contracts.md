# Commercial Credit Ledger Contracts

Boundary contracts for tenant credit accounting in commercial mode. Paid checkout, Stripe, webhooks, invoices, a free-account SKU, and a free-import count are **out of contract** for this increment.

Existing import gates (preview required, stale preview, tenant mismatch, validation errors) remain. This feature adds one confirm rule: live remaining must cover would-create.

## 1. Composition and storage

When `Features:CommercialMode:Enabled` is **true**:

1. Keep 009 composition: `AddAzureTableServiceClient("tables")`, keyed `TableClient`s, Commercial use cases.
2. Require `Storage:CommercialCreditLedgerTable` at startup (same pattern as accounts/audit tables). Default name `CommercialCreditLedger`.
3. Register credit ledger store, `EnsureCurrentBalance` / consume use cases, and `IImportTaskCreationQuota` (Commercial implementation).
4. Do not register Stripe clients, checkout endpoints, or webhook handlers.
5. Do not add AppHost resources; reuse `tables`.

When **false**:

1. Do not require the credit table setting.
2. Do not register credit stores or use cases.
3. Register the Application no-op `IImportTaskCreationQuota`.
4. Home and coordinator must not show credit copy or block confirm for credits.

Commercial still must not reference Graph, Kiota, MudBlazor, or Stripe packages. Application/Domain must not reference Azure Tables, Stripe, or Commercial ledger types.

## 2. Ensure current balance

Purpose: Lazy expiry then grant; return live remaining.

Request:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `TenantId` | string | Yes | From session; never from the client body as a spoofable wallet id. |
| `ActorUserId` | string | Yes | Audit only. |
| `OccurredUtc` | `DateTimeOffset` | Yes | Grant/expiry timestamp. |
| `Reason` | enum | Yes | `SignIn`, `Preview`, `Confirm`, `Execute`. |

Response (success):

| Field | Type | Meaning |
|-------|------|---------|
| `RemainingCredits` | int | Derived; ≥ 0. |
| `FreeRemaining` | int | V1 equals remaining. |
| `PaidRemaining` | int | 0 this increment. |
| `ExpiryApplied` | bool | Leftover free lots closed. |
| `GrantApplied` | bool | This call inserted the month’s free grant. |

Response (failure): structured code, no UI text. Suggested codes: `credits.ledger_unavailable`, `credits.grant_failed`, `credits.expiry_failed`.

Rules:

- `Reason = SignIn`: caller MAY ignore failure and still establish a session.
- `Preview` / `Confirm` / `Execute`: caller MUST fail closed (no import, no pretend remaining 0, no grant if the grant step failed).
- File validation / CSV parse MUST NOT call this contract.
- Deleted-account / blocked sign-in MUST NOT grant.
- Idempotent for the same UTC month.

## 3. Preview balance comparison (UI contract)

After a successful planning preview (commercial on):

| Field | Type | Notes |
|-------|------|-------|
| `WouldCreateCount` (N) | int | `TaskActions` with `Create` only. |
| `RemainingCredits` (M) | int | Live remaining after Ensure. |
| `Shortfall` | int | `N - M` when N > M; else 0. |
| `InsufficientCredits` | bool | N > M. |

Preview never consumes credits. Confirm import is disabled when `InsufficientCredits` or when Ensure/read failed (fail closed: error, not the insufficient-credits warning with M = 0).

UK English warning (Web presenter only), prominent on the preview step, must include N, M, and shortfall. Copy MAY say the organisation needs more credits. MUST NOT offer or imply a completed purchase, checkout, or SKU switch.

When N ≤ M, this feature MUST NOT disable confirm. Existing non-credit `canExecute` rules still apply (`!isBusy`, preview present, request present, `!isPreviewStale`, selection in sync).

Commercial off: no warning, no N/M/shortfall, confirm unchanged.

## 4. Confirm import (coordinator)

`ImportWorkflowCoordinator.ExecuteAsync` (and the preview-step `canExecute` binding) MUST re-read live remaining at confirm time.

| If | Then |
|----|------|
| Commercial off | Existing execute path; no credit call. |
| Ledger unavailable | Do not call execution use case; structured failure → UK English error. |
| Live M < N | Refuse execute; same insufficient-credits warning as preview. |
| Live M ≥ N | Proceed; do not reserve N credits. |
| Stale preview / other existing gates | Unchanged; still block. |

Two concurrent confirms that both saw M ≥ N MAY both start. Each create loop still stops at remaining 0. Combined usage MUST NOT make remaining negative.

## 5. Task-creation quota (Application port)

Registered always. Commercial implementation only meters when Commercial is composed.

`BeforeCreateAsync`:

| Kind | Execution use case |
|------|-------------------|
| `Allow` | Call `CreateTaskAsync`. |
| `Exhausted` | Do not start this or further creates; mark remaining Create rows with diagnostic `credits.exhausted`. |
| `Unavailable` | Stop; fail closed; do not treat as exhaustion with remaining 0. |

`RecordSuccessfulCreateAsync` (after Planner create succeeds):

- Persist `Usage` quantity 1 against free lots first (paid FIFO later).
- Retry once on transient persistence failure before returning failure.
- On persistent failure: execution use case must not start further creates, must not delete the Planner task, must mark remaining Create rows visible with `credits.usage_record_failed`.

No-op implementation: always `Allow`; record is no-op.

## 6. Import summary (UI contract)

After a commercial run (including partial stop):

| Figure | Source |
|--------|--------|
| Tasks created | Count of created items with target Task (not buckets). |
| Credits used | Usage quantity recorded for this run (free monthly in V1; no overage line). |
| Remaining credits | Ledger-derived remaining after the run (must match a subsequent Ensure/read). |

Credit-exhausted rows must appear with presenter UK English, not omitted. Commercial off: no credit figures on the summary; no Home billing copy.

No persistent header wallet (FR-028). No transaction-history page.

## 7. Security and tenancy

- Trust boundary: authenticated session → `TenantId` / `UserId` from server-side claims (`SessionIdentityContext`), never a client-supplied tenant override.
- A tenant MUST NOT read or mutate another tenant’s partition.
- Secrets: none new. No Stripe keys this increment.
- Diagnostics: outcome codes and quantities only; do not log full CSV or access tokens.

## 8. Architecture evidence

Automated checks MUST fail if:

- Application or Domain contain `CreditLedgerTransaction`, `CreditLot`, table adapters, or Stripe types.
- Commercial references Microsoft.Graph, Kiota, MudBlazor, Stripe.net, or `Infrastructure.Graph`.
- Credit table clients are registered when commercial mode is false.
- AppHost tests are added for this feature.

## 9. Traceability

| Contract | Spec |
|----------|------|
| Composition | FR-001, FR-020, user Stripe constraint |
| Ensure balance | FR-006–FR-013, FR-030, US1 |
| Preview comparison | FR-014–FR-019, US2 |
| Confirm re-check | FR-029, coordinator exception |
| Quota / consume | FR-021–FR-023, FR-031, US3 |
| Summary | FR-024–FR-025, US4 |
| Commercial off | FR-001, FR-028, US5 |
