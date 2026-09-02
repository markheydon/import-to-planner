# Research: Commercial Credit Ledger

All Technical Context items are resolved from spec 011, `docs-internal/credits-billing-usage-model.md`, issue #126, constitution 2.2.0, engineering policies, and shipped 008/009 commercial storage. No remaining NEEDS CLARIFICATION.

## 1. Ledger as system of record (Azure Tables, 008/009 pattern)

**Decision**: Persist an append-only credit ledger in the existing commercial Azure Tables account (`tables` on `web` only). Add one configured table (`Storage:CommercialCreditLedgerTable`). Do not add SQL, Redis, Stripe, or a new Aspire storage resource.

**Rationale**: Spec FR-003 requires an immutable ledger with balances derived from transactions. 008/009 already isolate commercial persistence in `ImportToPlanner.Commercial` using keyed `TableClient`s from `AddAzureTableServiceClient("tables")`. Reusing that account keeps commercial mode off free of Tables and matches engineering-policy isolation.

**Alternatives considered**:
- Mutable remaining-credits column as source of truth — rejected (FR-003, product contract).
- SQL ledger — rejected; no SQL in topology; would force self-host or AppHost changes.
- Separate storage account — unnecessary for V1 free grants.

**Concurrency**: Azure entity-group transactions (same partition = tenant) update a lot projection and insert a usage/grant/expiry row together. Optimistic ETag retry on conflict. A insert-only month marker (`RowKey` unique per UTC `yyyyMM`) enforces one free grant per tenant per month. Derived remaining is recomputed from transactions when a projection conflicts; the ledger remains the source of record. Combined creates across concurrent imports must not drive remaining below zero.

## 2. Lots and types without paid purchases

**Decision**: Model lots and transaction types now (`FreeMonthly` lot; `Grant`, `Usage`, `Expiry` transactions). Reserve `Paid` lot type and `Purchase` / `PaidExpiry` transaction types in the model only. Do not implement Stripe, checkout, webhooks, invoices, paid SKUs, or a paid expiry job.

**Rationale**: FR-004/FR-005 require consumption order (free first, then oldest paid) so #125 can add purchases later. User constraint for this increment: no paid path and no fake purchase UI.

**Alternatives considered**:
- Free-only schema with a later rewrite — rejected (painful for #125).
- Shipping a stub checkout button — rejected (FR-020).
- Free-account SKU or “one free import” — rejected (out of scope; user constraint).

## 3. Application seam vs gateway decorator

**Decision**: Keep grant, expiry, live-balance, and confirm policy in Commercial. Add a thin Application port `IImportTaskCreationQuota` used only inside `ImportExecutionUseCase` around `CreateTaskAsync`. Web’s `ImportWorkflowCoordinator` gains the credit confirm re-check and preview balance snapshot; existing stale-preview, tenant-mismatch, and parse gates stay as they are.

**Rationale**: `ImportExecutionUseCase` currently treats any `CreateTaskAsync` exception as a per-row failure and continues. Credit exhaustion and usage-record failure must stop further creates (FR-022, FR-031). A Web/Graph decorator cannot express “stop the loop” without Application changes. A technology-neutral quota port (Allow / Exhausted / Unavailable) mirrors `ITenantOperationalMetadataStore`: Application owns the seam, Commercial implements it, self-host registers a no-op that always allows and never writes.

**Coordinator rule (only new gate)**: After a valid preview, compare **would-create** (count of `TaskActions` with `PlannedEntityAction.Create`, not file rows, not buckets, not reuse/skip) with **live** remaining. If N > M, disable/refuse Confirm import. Re-check at `ExecuteAsync`, not only at preview time. Do not reserve credits.

**Alternatives considered**:
- Decorating `IPlannerGateway` in Web only — rejected; use case continues after failures.
- Putting ledger types in Application — rejected (009 moved commercial types out; architecture tests forbid them).
- Changing `canValidate` or preview-staleness rules — rejected (user: preserve coordinator gating except the credit confirm rule).

## 4. Lazy UTC grant and expiry (no dormant jobs)

**Decision**: `EnsureCurrentBalance` (expire leftover free lots for prior UTC months, then grant 25 if no grant marker for the current UTC month) runs on successful commercial sign-in and on preview, confirm, and execute. File validation / CSV parse must not call it. No background grant or free-expiry job. Quantity 25 is a Commercial policy constant (single published V1 figure), not a SKU and not a per-import gift.

**Rationale**: FR-007, FR-012, FR-013, clarifications (session across month-end; dormant tenants stay silent). Sign-in may succeed if the ledger call fails; preview/confirm/execute fail closed (FR-030).

**Alternatives considered**:
- Timer job for all tenants — rejected (dormant noise; out of scope).
- Prorating or backdating to the 1st — rejected (FR-008, FR-009).
- Billing timezone per tenant — out of scope (assumption: UTC).

## 5. Fail-closed errors and usage-record retry

**Decision**: Ledger read/grant/expiry failure on preview, confirm, or execute returns a structured commercial failure (no UI text). Web presents UK English; remaining is not shown as zero; no grant row is written. After a successful Planner create, record one usage against free lots first; retry that record before the next create; if it still fails, stop the run, keep the Planner task, mark remaining not-started rows visible with a recording-failure diagnostic (not credit-exhausted, not full success).

**Rationale**: Clarifications and FR-030/FR-031. Credits never go negative (EGT decrement only when lot remaining > 0).

**Alternatives considered**:
- Treat ledger down as remaining zero — rejected (would false-block as insufficient credits).
- Delete the Planner task if usage cannot be recorded — rejected (FR-031).

## 6. Testing and delivery evidence

**Decision**: xUnit v3, NSubstitute, built-in Assert; in-memory ledger double for Commercial use-case tests; bUnit for warning, disabled confirm, summary figures, and commercial-off absence. Architecture scans: ledger types stay in Commercial; Application has no Azure Tables / Stripe; no AppHost tests; no Playwright unless a later explicit journey is requested (not required here). `dotnet format` at implement.

**Rationale**: `docs-internal/engineering-policies.md` and AGENTS.md. Inner policy must fail without production deploy (constitution VI).
