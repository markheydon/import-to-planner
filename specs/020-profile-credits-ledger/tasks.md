---

description: "Task list for Profile Credits and Ledger Activity feature implementation"
---

# Tasks: Profile Credits and Ledger Activity

**Input**: Design documents from `/specs/020-profile-credits-ledger/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, [contracts/profile-credits-ledger-contracts.md](./contracts/profile-credits-ledger-contracts.md), quickstart.md. **Requires** shipped [#126](https://github.com/markheydon/import-to-planner/issues/126) (`specs/011-commercial-credit-ledger`) and [#129](https://github.com/markheydon/import-to-planner/issues/129) complimentary policy (`specs/019-complimentary-tenant-credits`). **Coordinates with** [#125](https://github.com/markheydon/import-to-planner/issues/125) for paid lots, purchases, and buy-credits UI hooks.

**Tests**: Required per spec success criteria (SC-001–SC-007), FR acceptance, and [quickstart.md](./quickstart.md) (xUnit v3, NSubstitute, bUnit; extend `InMemoryCreditLedgerStore`; no AppHost tests).

**Organization**: Tasks grouped by user story (US1–US5 from spec.md).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US5
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/ImportToPlanner.{Commercial,Web}/`
- **Tests**: `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`
- **Contracts**: `specs/020-profile-credits-ledger/contracts/profile-credits-ledger-contracts.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Audit reason and profile-specific Commercial types before store and use cases.

- [x] T001 Add `EnsureBalanceReason.Profile` to `src/ImportToPlanner.Commercial/Models/CreditLedgerEnums.cs` for profile-load ensure audit per contracts §2 and [011 commercial-credit-ledger-contracts.md](../011-commercial-credit-ledger/contracts/commercial-credit-ledger-contracts.md) §2 `Reason` enum (011 contract and data-model already document `Profile`; this task is the code enum value only)
- [x] T002 [P] Add `ProfileCreditActivityKind` enum (`FreeMonthlyGrant`, `FreeCreditExpiry`, `ImportRunUsage`, `PaidPurchase`, `PaidLotExpiry`) and profile request/result types (`ProfileCreditSummaryRequest`, `ProfileCreditSummaryResult`, `ProfileFreeMonthlyLot`, `ProfilePaidLot`, `ProfileCreditActivityRequest`, `ProfileCreditActivityResult`, `ProfileCreditActivityEntry`) with field constraints from [data-model.md](./data-model.md) in `src/ImportToPlanner.Commercial/Models/ProfileCreditModels.cs`
- [x] T003 [P] Add discriminated success/failure outcome types for profile summary and activity (`GetProfileCreditSummaryOutcome`, `GetProfileCreditActivityOutcome`; failures use `CommercialCreditBalanceFailure` + `CommercialCreditFailureCodes`; no UI strings in Commercial) in `src/ImportToPlanner.Commercial/Models/ProfileCreditModels.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Ledger list API and profile read use cases (FR-014), and Commercial unit tests. **No user story Web work until complete.**

**⚠️ CRITICAL**: Profile reads depend on [#126](https://github.com/markheydon/import-to-planner/issues/126) ledger and ensure; complimentary rules from [#129](https://github.com/markheydon/import-to-planner/issues/129).

- [x] T004 Define `IGetProfileCreditSummaryUseCase` with `GetAsync(ProfileCreditSummaryRequest, CancellationToken)` per contracts §3 in `src/ImportToPlanner.Commercial/Abstractions/IGetProfileCreditSummaryUseCase.cs`
- [x] T005 [P] Define `IGetProfileCreditActivityUseCase` with `GetAsync(ProfileCreditActivityRequest, CancellationToken)` per contracts §4 in `src/ImportToPlanner.Commercial/Abstractions/IGetProfileCreditActivityUseCase.cs`
- [x] T006 Extend `ICreditLedgerStore` with read-only `ListTransactionsAsync(string tenantId, DateTimeOffset occurredSinceUtc, CancellationToken cancellationToken)` returning `IReadOnlyList<CreditLedgerTransaction>` and `HasTransactionsBeforeAsync(string tenantId, DateTimeOffset beforeOccurredUtc, CancellationToken cancellationToken)` returning whether any `tx|` row exists with `OccurredUtc < beforeOccurredUtc` (for `HasOlderHistory` without loading pre-window rows); implement per [contracts/profile-credits-ledger-contracts.md](./contracts/profile-credits-ledger-contracts.md) §5 and [data-model.md](./data-model.md) §2 in `src/ImportToPlanner.Commercial/Abstractions/ICreditLedgerStore.cs`
- [x] T007 Implement `ListTransactionsAsync` and `HasTransactionsBeforeAsync` in `TableCreditLedgerStore` — single tenant partition, `tx|` row prefix; list filters `OccurredUtc >= occurredSinceUtc`; existence check stops at first older row (efficient partition scan) in `src/ImportToPlanner.Commercial/Credits/Storage/TableCreditLedgerStore.cs`
- [x] T008 [P] Implement `ListTransactionsAsync` and `HasTransactionsBeforeAsync` with same semantics in `tests/ImportToPlanner.Tests/TestInfrastructure/InMemoryCreditLedgerStore.cs`
- [x] T009 Implement `GetProfileCreditSummaryUseCase.cs` — call `IEnsureCurrentCreditBalanceUseCase` with `EnsureBalanceReason.Profile` (FR-003); on success map `RemainingCredits`, `ComplimentaryUnlimitedAccess`, optional current-month `FreeMonthly` lot (`GrantedQuantity` > 0, `RemainingQuantity` 0..`GrantedQuantity`, `ExpiresAtUtc` UTC month end); return empty `PaidLots` until #125; on failure return structured codes (`credits.ledger_unavailable`, `credits.grant_failed`, `credits.expiry_failed`) without numeric remaining in `src/ImportToPlanner.Commercial/Credits/GetProfileCreditSummaryUseCase.cs`
- [x] T010 Implement `GetProfileCreditActivityUseCase.cs` — inject `IComplimentaryTenantPolicy` (019) to decide usage-only vs full activity types; `ActivityWindowStartUtc = request.OccurredUtc.AddMonths(-12)`; `ListTransactionsAsync` since window start; map ledger `EntryType` `FreeGrant`/`FreeExpiry`/`Usage` (and future paid types) to `ProfileCreditActivityKind` rows; build entries newest-first; group `Usage` by `ImportRunId` when present (one `ImportRunUsage` row per run, `Quantity` = sum, `OccurredUtc` = latest in group); usage rows with missing `ImportRunId` become one grouped usage row each (spec assumption; never one activity row per task); non-complimentary include grant/expiry rows (never `ComplimentaryGrant` as its own row); complimentary include grouped usage only; set `HasOlderHistory` via `HasTransactionsBeforeAsync(tenantId, ActivityWindowStartUtc)`; on list or existence-check failure return `GetProfileCreditActivityOutcome.Failed` with `credits.ledger_unavailable` (independent of summary outcome) in `src/ImportToPlanner.Commercial/Credits/GetProfileCreditActivityUseCase.cs`
- [x] T011 Register `IGetProfileCreditSummaryUseCase` and `IGetProfileCreditActivityUseCase` only when commercial mode is composed in `src/ImportToPlanner.Commercial/DependencyInjection.cs`
- [x] T012 [P] Add `CreditLedgerStoreListTests.cs` — partition filter, `occurredSinceUtc` boundary, `HasTransactionsBeforeAsync` true/false with seeded pre-window row, ordering assumptions in `tests/ImportToPlanner.Tests/Credits/CreditLedgerStoreListTests.cs`
- [x] T013 [P] Add `GetProfileCreditSummaryUseCaseTests.cs` — SC-001 ensure + remaining match ledger; SC-003 free lot fields; `Profile` ensure reason; tenant id from request only in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditSummaryUseCaseTests.cs`
- [x] T014 [P] Add `GetProfileCreditActivityUseCaseTests.cs` — SC-004 two import runs ⇒ two grouped usage rows; grants/expiry for non-complimentary; twelve-month omission + `HasOlderHistory`; complimentary usage-only filter; usage without `ImportRunId` ⇒ one activity row per usage transaction (not per task); simulated `ListTransactionsAsync` or `HasTransactionsBeforeAsync` failure ⇒ `Failed` with `credits.ledger_unavailable` and no entries (quickstart item 8 / FR-004 activity path) in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditActivityUseCaseTests.cs`

**Checkpoint**: Commercial profile summary and activity use cases pass unit tests without Web changes.

---

## Phase 3: User Story 1 - Commercial User Checks Remaining Credits on Profile (Priority: P1) 🎯 MVP

**Goal**: Signed-in hosted commercial users see organisation remaining credits after ensure on `/profile`; fail closed on read/ensure errors; no credits UI when commercial off.

**Independent Test**: Known ledger state ⇒ profile remaining matches post-ensure balance; ledger failure ⇒ friendly UK English error and no `0` remaining; commercial off ⇒ no credits section (SC-001, SC-002, SC-005).

### Tests for User Story 1

- [x] T015 [P] [US1] Extend `ProfilePageTests.cs` — commercial on shows credits section with remaining matching stubbed summary in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`
- [x] T016 [P] [US1] Add bUnit fail-closed balance — summary failure shows UK English error; markup must not present remaining as `0` or insufficient-credits/buy-credits substitute (FR-004) in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`
- [x] T017 [P] [US1] Add bUnit commercial off / self-host composition — no credits copy on `Profile` (regression SC-005) in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs` and `tests/ImportToPlanner.Web.Tests/SelfHostWebCompositionTests.cs` if needed

### Implementation for User Story 1

- [x] T018 [P] [US1] Create `ProfileCreditPresenter.cs` — UK English balance labels and error messages; delegate unlimited/numeric remaining formatting via `ImportCreditPreviewPresenter.FormatRemainingCredits` and reuse `BuildLedgerUnavailableMessage()` (or equivalent) for ledger read failures where appropriate; no Stripe or diagnostic text (FR-008) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Presenters/ProfileCreditPresenter.cs`
- [x] T019 [US1] Inject optional `IGetProfileCreditSummaryUseCase` and load summary on profile init when commercial services registered; pass `TenantId` and `ActorUserId` from session identity only (never from query or form); map summary failure to balance error state independent of activity in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor.cs`
- [x] T020 [US1] Add `MudPaper` credits section (between account details and delete account) with remaining headline and balance-only `MudAlert` on failure in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor`
- [x] T021 [US1] Gate credits section on commercial mode + authenticated session + registered use cases; do not query ledger when commercial off (FR-001) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor` and `Profile.razor.cs`

**Checkpoint**: MVP profile shows correct remaining or fail-closed balance; commercial off unchanged.

---

## Phase 4: User Story 2 - User Understands the Current Free Monthly Lot (Priority: P1)

**Goal**: Non-complimentary tenants see current free monthly lot granted, remaining, and UTC month-end expiry after ensure on profile load.

**Independent Test**: Partially used monthly grant ⇒ profile shows granted, lot remaining, and expiry aligned with `CreditLot` (SC-003).

### Tests for User Story 2

- [x] T022 [P] [US2] Extend `GetProfileCreditSummaryUseCaseTests.cs` — month-boundary ensure updates lot; grant-before-display when month not yet granted in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditSummaryUseCaseTests.cs`
- [x] T023 [P] [US2] Extend `ProfilePageTests.cs` — free lot row visible with granted/remaining/expiry for non-complimentary tenant in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`

### Implementation for User Story 2

- [x] T024 [US2] Add presenter methods for free monthly lot copy (UK English, month-end expiry wording per product contract) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Presenters/ProfileCreditPresenter.cs`
- [x] T025 [US2] Render free monthly lot subsection when `FreeMonthlyLot` is non-null and tenant is not complimentary in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor`

**Checkpoint**: Free lot details visible for standard commercial tenants alongside remaining total.

---

## Phase 5: User Story 3 - User Reviews Organisation Credit Activity (Priority: P2)

**Goal**: Rolling twelve-month activity, newest first, grouped import usage per run, grants/expiry for non-complimentary; independent activity failure handling.

**Independent Test**: Seed grant, expiry, two import runs ⇒ correct order, types, totals; older-than-twelve-month events omitted with visible copy (SC-004).

### Tests for User Story 3

- [x] T026 [P] [US3] Extend `GetProfileCreditActivityUseCaseTests.cs` — never one row per task; quantity equals per-run total (FR-007) in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditActivityUseCaseTests.cs`
- [x] T027 [P] [US3] Extend `ProfilePageTests.cs` — activity list newest-first; `HasOlderHistory` shows UK English omission copy in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`
- [x] T028 [P] [US3] Add bUnit independent activity failure — summary succeeds, activity fails: balance/lot visible; activity region error only (quickstart item 8) in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`

### Implementation for User Story 3

- [x] T029 [US3] Load activity via `IGetProfileCreditActivityUseCase` in parallel or sequence after summary; separate activity error state in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor.cs`
- [x] T030 [US3] Add activity list/table with presenter labels per `ProfileCreditActivityKind`; if row count within the twelve-month window is large, use pagination or virtualisation with clear “more activity” copy (spec edge case) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor`
- [x] T031 [US3] Show older-history notice when `HasOlderHistory` is true (FR-006) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor` via `ProfileCreditPresenter.cs`

**Checkpoint**: Activity report readable and aligned with ledger; balance and activity errors independent.

---

## Phase 6: User Story 4 - Complimentary Tenants See Unlimited, Not Exhaustion (Priority: P1)

**Goal**: Allowlisted tenants see Unlimited remaining, no free/paid lot breakdown, activity shows grouped import usage only.

**Independent Test**: Allowlisted tenant with usage ⇒ Unlimited + usage-only activity rows; no buy-credits when #125 UI exists (SC-006).

### Tests for User Story 4

- [x] T032 [P] [US4] Extend `GetProfileCreditActivityUseCaseTests.cs` — complimentary tenant excludes grant/expiry/purchase/complimentary-grant lines in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditActivityUseCaseTests.cs`
- [x] T033 [P] [US4] Extend `ProfilePageTests.cs` — complimentary: Unlimited remaining, no free lot section, usage-only activity in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`

### Implementation for User Story 4

- [x] T034 [US4] When `ComplimentaryUnlimitedAccess`, show Unlimited/complimentary remaining only; omit free monthly and paid lot subsections (FR-005, FR-010) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor` and `ProfileCreditPresenter.cs`
- [x] T035 [US4] Guard any buy-credits control so complimentary tenants never see it (FR-011 coordination with #125) in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor`

**Checkpoint**: Complimentary profile matches import journey unlimited promise.

---

## Phase 7: User Story 5 - Paid Credit Lots and Purchases Extend the Same Profile Section (Priority: P3)

**Goal**: When [#125](https://github.com/markheydon/import-to-planner/issues/125) is available, extend the same section with paid lots, purchase activity, and buy-credits for non-allowlisted tenants only.

**Independent Test**: Non-allowlisted tenant with paid lots and purchase ledger rows ⇒ lot breakdown + purchase activity + buy entry; allowlisted ⇒ no buy (SC-007).

### Tests for User Story 5

- [x] T036 [P] [US5] Extend `GetProfileCreditSummaryUseCaseTests.cs` — map open `Paid` lots with twelve-month expiry when ledger contains paid lots (skip or `[Explicit]` until #125 fixtures exist) in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditSummaryUseCaseTests.cs`
- [x] T037 [P] [US5] Extend `GetProfileCreditActivityUseCaseTests.cs` — `PaidPurchase` and `PaidLotExpiry` (`CreditEntryType.PaidExpiry`) activity rows for non-complimentary when ledger entry types present (skip or `[Explicit]` until #125 fixtures exist) in `tests/ImportToPlanner.Tests/Credits/GetProfileCreditActivityUseCaseTests.cs`
- [x] T038 [P] [US5] Extend `ProfilePageTests.cs` — paid lot rows and buy-credits visible only for non-allowlisted when checkout feature registered (mock/stub #125 availability) in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`

### Implementation for User Story 5

- [x] T039 [US5] Map `PaidLots` from ledger open `LotType.Paid` lots in `GetProfileCreditSummaryUseCase.cs` when #125 data exists; keep empty list before #125 ships
- [x] T040 [US5] Include `PaidPurchase` / paid expiry activity kinds in `GetProfileCreditActivityUseCase.cs` for non-complimentary tenants when ledger writes exist
- [x] T041 [US5] Add paid lot subsection and buy-credits link/button guarded by #125/checkout availability and non-complimentary policy in `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor` and `ProfileCreditPresenter.cs`

**Checkpoint**: Profile section ready to absorb paid credits without a separate wallet page.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: FR-012/FR-013 verification, format gate, and quickstart evidence.

- [x] T042 [P] Add FR-002 tenant isolation tests — Commercial use cases honour request `TenantId` from caller; Web profile passes session tenant only (extend `ProfilePageTests.cs` to assert use case invoked with session tenant) in `tests/ImportToPlanner.Tests/Credits/ProfileCreditTenancyTests.cs` and `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`
- [x] T043 Confirm import preview/confirm/execute credit gates unchanged — no edits to `ImportWorkflowCoordinator` balance gates beyond existing ensure calls (FR-012); confirm no operator-only ledger browser or support UI added on `/profile` (FR-013); document in PR if touched
- [x] T044 [P] Confirm profile credits registration and UI expose no Stripe usage metering, customer portal links, or payment-provider statement chrome (FR-009, contracts §1) — review `DependencyInjection.cs`, `Profile.razor`, and presenters only; no new Stripe dependencies for profile reads
- [x] T045 Run `dotnet test ImportToPlanner.slnx` and fix regressions in `tests/ImportToPlanner.Tests/Credits/` and `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`
- [x] T046 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [x] T047 Validate [quickstart.md](./quickstart.md) manual Aspire spot-check scenarios (commercial on profile, complimentary tenant, commercial off)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **blocks all user stories**
- **User Stories (Phases 3–7)**: Depend on Phase 2 completion
  - US1 (Phase 3) is MVP; US2–US4 can proceed after US1 Web shell exists
  - US3 activity UI depends on activity use case (Phase 2) and benefits from US1 profile section
  - US5 can ship after US1–US4; may trail #125 implementation
- **Polish (Phase 8)**: After desired user stories complete

### User Story Dependencies

- **US1 (P1)**: After Phase 2 — no dependency on other stories
- **US2 (P1)**: After Phase 2; Web depends on US1 credits section shell
- **US3 (P2)**: After Phase 2; Web integrates with US1 section
- **US4 (P1)**: Commercial filtering in Phase 2; Web presentation after US1–US3 regions exist
- **US5 (P3)**: After US1 section; blocked on #125 ledger/checkout availability for full validation

### Parallel Opportunities

- Phase 1: T002 and T003 parallel with T001 after enum file open
- Phase 2: T005, T008, T012, T013, T014 in parallel after T006–T007
- Within US1: T015–T017 tests parallel; T018 parallel with tests once interfaces stable
- US2–US4 test tasks marked [P] can run in parallel per story
- Different user stories: US2 and US3 Web work can parallelise after US1 T020 lands (coordinate `Profile.razor` edits)

---

## Parallel Example: User Story 1

```bash
# Commercial unit tests (Phase 2) in parallel:
tests/ImportToPlanner.Tests/Credits/GetProfileCreditSummaryUseCaseTests.cs
tests/ImportToPlanner.Tests/Credits/GetProfileCreditActivityUseCaseTests.cs

# Web tests in parallel once presenter exists:
tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs  # remaining + fail-closed + commercial off
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: SC-001, SC-002, SC-005 via automated tests and quickstart spot-check
5. Demo `/profile` remaining credits for a commercial tenant

### Incremental Delivery

1. Setup + Foundational → Commercial read path ready
2. US1 → balance on profile (MVP)
3. US2 → free lot transparency
4. US3 → activity report
5. US4 → complimentary alignment with #129
6. US5 when #125 ships → paid lots and buy-credits
7. Polish → format gate and full suite green

### Parallel Team Strategy

1. Developer A: Phase 2 store + summary use case + T013
2. Developer B: Phase 2 activity use case + T014
3. After Phase 2: Developer A — US1 Web; Developer B — US3 activity UI; Developer C — US4 complimentary tests/UI

---

## Notes

- Ledger list and grouping stay in Commercial; Web uses presenters only (constitution I, III)
- Do not add Home header wallet chrome (FR-001)
- Profile is read-only except ensure side effects already defined in 011
- Paid purchase and buy-credits tasks are safe to stub until #125 lands; initial release targets grants, expiry, and grouped usage (spec assumptions)
- Commit after each task or logical group; optional hook: `/speckit-git-commit` after tasks generation
