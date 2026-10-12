# Research: Profile Credits and Ledger Activity

All Technical Context items are resolved from spec 020, `docs/credits-billing-usage-model.md`, issues [#138](https://github.com/markheydon/import-to-planner/issues/138) / [#126](https://github.com/markheydon/import-to-planner/issues/126) / [#129](https://github.com/markheydon/import-to-planner/issues/129) / [#125](https://github.com/markheydon/import-to-planner/issues/125), shipped 011/019 commercial credit module, existing `/profile` page, constitution 2.2.0, and engineering policies. No remaining NEEDS CLARIFICATION.

## 1. Profile read path vs import coordinator

**Decision**: Add dedicated Commercial read use cases for profile (`IGetProfileCreditSummaryUseCase`, `IGetProfileCreditActivityUseCase`) registered only when commercial mode is on. Profile page calls them after session identity is known; they call existing `IEnsureCurrentCreditBalanceUseCase` with a new audit reason `EnsureBalanceReason.Profile`, then load lots and ledger transactions. Do not route profile through `ImportWorkflowCoordinator` or Home state.

**Rationale**: FR-012 forbids changing preview/confirm/execute gates. Profile is a separate account surface (FR-013). Coordinator snapshots are import-scoped; reusing them would couple `/profile` to workflow state and complicate independent balance vs activity failure (spec edge case).

**Alternatives considered**:
- Shared “balance service” in Web only — rejected; ensure and ledger access belong in Commercial (011/019 pattern).
- Sign-in-only ensure without profile ensure — rejected; FR-003 requires same ensure on profile load including month boundary.

## 2. Ledger transaction listing (Azure Tables)

**Decision**: Extend `ICreditLedgerStore` with `ListTransactionsAsync(tenantId, occurredSinceUtc, cancellationToken)` and `HasTransactionsBeforeAsync(tenantId, beforeOccurredUtc, cancellationToken)` implemented in `TableCreditLedgerStore` by querying the tenant partition for row keys with prefix `tx|`, filtering `OccurredUtc >= occurredSinceUtc` for the list (newest-first in memory; partition query returns lexicographic row key order which is reverse-chronological via reverse ticks). Cap the window at rolling twelve months in the use case (`request.OccurredUtc.AddMonths(-12)`), not in storage. Set `HasOlderHistory` via the existence probe at `ActivityWindowStartUtc`, not from the windowed list alone.

**Rationale**: FR-006 requires twelve-month activity. Transactions are already stored with `BuildTransactionRowKey` for time ordering. No new Aspire resources or tables.

**Alternatives considered**:
- Separate activity projection table — rejected (duplicates ledger; violates single source of truth).
- Load all transactions always — rejected (unbounded growth; spec limits display to twelve months).

## 3. Activity grouping and complimentary filtering

**Decision**: Build `ProfileCreditActivityEntry` rows in Commercial from raw transactions:

| Tenant type | Include entry types | Usage presentation |
|-------------|---------------------|--------------------|
| Non-complimentary | `FreeGrant`, `FreeExpiry`, `Usage` (grouped), reserved `PaidPurchase` / `PaidExpiry` when [#125](https://github.com/markheydon/import-to-planner/issues/125) writes them | Group `Usage` by `ImportRunId` when present; sum `Quantity`; one row per run; use latest `OccurredUtc` in group for sort. Rows with missing `ImportRunId` (legacy): one grouped row per usage transaction (never one row per task in the activity list) |
| Complimentary allowlist | `Usage` grouped only | Exclude `ComplimentaryGrant`, `FreeGrant`, `FreeExpiry`, purchases from profile activity (clarifications session 2026-10-12) |

Pairing rule: never surface `ComplimentaryGrant` as its own row; it exists for audit only (019).

**Rationale**: FR-007, FR-006, User Story 4. Import runs are already keyed on usage rows (`ImportRunId` on `CreditLedgerTransaction`).

**Alternatives considered**:
- One row per task on profile — rejected (FR-007).
- Show complimentary grants as “free top-up” lines — rejected (clarification: grouped import usage only).

## 4. Free monthly lot presentation

**Decision**: After successful ensure, select the current UTC month’s `FreeMonthly` lot (grant marker month = `CommercialCreditPolicy.GetUtcYearMonth(occurredUtc)`): expose `GrantedQuantity`, `RemainingQuantity`, `ExpiresAtUtc` (month end). If no open lot but grant just applied, use refreshed lots from store. Paid lots: list open `Paid` lots when [#125](https://github.com/markheydon/import-to-planner/issues/125) exists; omit lot breakdown sections entirely when `ComplimentaryUnlimitedAccess` is true (FR-005, FR-010).

**Rationale**: Aligns with `CreditLot` model and product contract month-end expiry wording.

**Alternatives considered**:
- Recompute lot remaining from transactions on profile only — rejected; lot projection is already maintained on usage/expiry (011).

## 5. Fail-closed and independent errors

**Decision**: Summary and activity use cases return discriminated outcomes (`Succeeded` / `Failed` with `CommercialCreditFailureCodes`). Web shows separate `MudAlert` regions for balance vs activity. On summary failure: no numeric remaining, no “0 credits”, no buy-credits substitute (FR-004). On activity failure with successful summary: show balance/lots; activity section shows error only. Reuse diagnostic codes in logs/activity tags; UK English strings in `ProfileCreditPresenter` only.

**Rationale**: FR-004, spec edge case (balance succeeds while activity list fails), constitution VII. Matches import fail-closed pattern (`ImportCreditPreviewPresenter`).

**Alternatives considered**:
- Single combined error for whole section — rejected (spec requires independent handling).

## 6. UI placement and commercial gating

**Decision**: Add a `MudPaper` credits section on existing `Profile.razor` between account details and delete account, only when `CommercialModeOptions.Enabled`, user authenticated, and credit use cases registered. No Home header wallet (FR-001). Self-host: profile route behaviour unchanged (commercial off already redirects or omits commercial services); no credit queries.

**Rationale**: Spec FR-001, FR-013; existing profile structure in `CommercialAccounts/Pages`.

**Alternatives considered**:
- New `/profile/credits` route — rejected (spec fixes surface to `/profile`).

## 7. Remaining credits copy alignment

**Decision**: Reuse `ImportCreditPreviewPresenter.FormatRemainingCredits` and complimentary unlimited wording consistent with 019 (“Unlimited” UK English). Profile does not show insufficient-credits or buy-credits on ledger read failure.

**Rationale**: FR-010, coordination with [#129](https://github.com/markheydon/import-to-planner/issues/129).

## 8. Testing and delivery evidence

**Decision**: xUnit v3, NSubstitute, built-in Assert; Commercial unit tests for activity grouping, twelve-month filter, complimentary filtering; extend `InMemoryCreditLedgerStore` with list API; bUnit on `Profile` for commercial on/off, fail-closed, unlimited tenant, twelve-month notice copy. No AppHost tests. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` before merge.

**Rationale**: Constitution VI/IX and `docs/engineering-policies.md`.

**Alternatives considered**:
- Playwright profile journey — not required unless explicitly requested later.
