---

description: "Task list for Commercial Credit Ledger feature implementation"
---

# Tasks: Commercial Credit Ledger

**Input**: Design documents from `/specs/011-commercial-credit-ledger/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/commercial-credit-ledger-contracts.md, quickstart.md

**Tests**: Automated checks are required per quickstart.md and engineering policies (xUnit v3, NSubstitute, bUnit). Test tasks are included below.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/ImportToPlanner.{Commercial,Application,Web}/`
- **Tests**: `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuration keys, folder structure, and policy constants for the credit ledger module.

- [x] T001 Add `Storage:CommercialCreditLedgerTable` (default `CommercialCreditLedger`) to `src/ImportToPlanner.Web/appsettings.json` and `src/ImportToPlanner.Web/appsettings.Development.json`
- [x] T002 [P] Create `src/ImportToPlanner.Commercial/Credits/` and `src/ImportToPlanner.Commercial/Credits/Storage/` folder structure per plan.md
- [x] T003 [P] Add `CommercialCreditPolicy.cs` with V1 free allowance constant (25 credits, no prorate) in `src/ImportToPlanner.Commercial/Credits/CommercialCreditPolicy.cs`
- [x] T004 [P] Add injectable UTC clock abstraction for Commercial credit month-boundary tests (use existing repo pattern or add `ITimeProvider` in Commercial)
- [x] T005 Confirm `src/ImportToPlanner.Commercial/ImportToPlanner.Commercial.csproj` has no Graph, Kiota, MudBlazor, or Stripe package references

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Ledger models, store contracts, Application quota port, no-op registration, and startup validation. **No user story work can begin until this phase is complete.**

**⚠️ CRITICAL**: US1–US4 depend on these contracts; US5 depends on the no-op quota path.

- [x] T006 [P] Add `CreditLot`, `CreditLedgerTransaction`, and `CreditMonthGrantMarker` models in `src/ImportToPlanner.Commercial/Models/`
- [x] T007 [P] Add `LotType`, `CreditEntryType`, and `EnsureBalanceReason` enums in `src/ImportToPlanner.Commercial/Models/`
- [x] T008 [P] Add `CommercialCreditBalanceResult`, failure codes (`credits.ledger_unavailable`, `credits.grant_failed`, `credits.expiry_failed`), and ensure request/response records in `src/ImportToPlanner.Commercial/Models/`
- [x] T009 [P] Define `ICreditLedgerStore` contract in `src/ImportToPlanner.Commercial/Abstractions/ICreditLedgerStore.cs`
- [x] T010 [P] Define `IEnsureCurrentCreditBalanceUseCase` in `src/ImportToPlanner.Commercial/Abstractions/ICommercialCreditUseCase.cs`
- [x] T011 [P] Define `IImportTaskCreationQuota` and `TaskCreationQuotaResult` (Allow / Exhausted / Unavailable) in `src/ImportToPlanner.Application/Abstractions/IImportTaskCreationQuota.cs`
- [x] T012 [P] Implement `NoOpImportTaskCreationQuota.cs` in `src/ImportToPlanner.Application/Services/NoOpImportTaskCreationQuota.cs`
- [x] T013 Register default no-op `IImportTaskCreationQuota` in `src/ImportToPlanner.Application/DependencyInjection.cs` (Commercial overrides when composed)
- [x] T014 Extend `src/ImportToPlanner.Web/Infrastructure/StartupConfigurationValidator.cs` to require `Storage:CommercialCreditLedgerTable` when `Features:CommercialMode:Enabled` is true
- [x] T015 Add keyed `CommercialCreditLedgerTableClient` `TableClient` registration constant and wiring in `src/ImportToPlanner.Commercial/DependencyInjection.cs`
- [x] T016 Implement `TableCreditLedgerStore.cs` table entity mapping and partition layout (`tx|`, `lot|`, `grant|`) in `src/ImportToPlanner.Commercial/Credits/Storage/TableCreditLedgerStore.cs`
- [x] T017 [P] Add in-memory `ICreditLedgerStore` test double in `tests/ImportToPlanner.Tests/TestInfrastructure/InMemoryCreditLedgerStore.cs`
- [x] T018 [P] Extend `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` to forbid `CreditLedgerTransaction`, `CreditLot`, and Azure Tables types in Application and Domain

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Tenant Receives a Fair Monthly Free Allowance (Priority: P1) 🎯 MVP

**Goal**: Lazy 25 free credits per UTC calendar month per tenant; explicit free-credit expiry; idempotent grant via month marker; no dormant-tenant jobs.

**Independent Test**: First balance-needed action in a UTC month grants 25 at that instant; repeat actions same month do not grant again; month-end crossover expires leftovers then grants fresh 25; dormant tenants get no rows.

### Implementation for User Story 1

- [x] T019 [US1] Implement `EnsureCurrentCreditBalanceUseCase.cs` (lazy expiry, month marker grant, derived remaining) in `src/ImportToPlanner.Commercial/Credits/EnsureCurrentCreditBalanceUseCase.cs`
- [x] T020 [US1] Register `ICreditLedgerStore`, `IEnsureCurrentCreditBalanceUseCase`, and commercial `IImportTaskCreationQuota` in `src/ImportToPlanner.Commercial/DependencyInjection.cs` when commercial mode is on
- [x] T021 [US1] Call Ensure on Allow/CreateAccount commercial sign-in in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.CommercialAccess.razor.cs` (ledger failure does not block session per FR-030)

### Tests for User Story 1

- [x] T022 [P] [US1] Add `EnsureCurrentCreditBalanceUseCaseTests.cs` — first grant 25 at `OccurredUtc`, mid-month still 25 (no prorate) in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs`
- [x] T023 [P] [US1] Add test — second Ensure same UTC month does not grant again in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs`
- [x] T024 [P] [US1] Add test — month-end leftover 10 expires then new month grants 25 (remaining 25, not 35) in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs`
- [x] T025 [P] [US1] Add test — dormant tenant (no Ensure call) produces no grant or expiry rows in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs`
- [x] T026 [P] [US1] Add test — concurrent Ensure same month yields exactly one `FreeGrant` in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs`
- [x] T027 [US1] Add test — CSV parse/validation failure path does not invoke Ensure in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs`

**Checkpoint**: Ledger grants and expiry work; tenant balance is auditable and idempotent per UTC month.

---

## Phase 4: User Story 2 - Preview Warns and Blocks When the Import Would Exceed Credits (Priority: P1)

**Goal**: Preview never consumes credits; prominent UK English warning with N/M/shortfall when would-create exceeds live remaining; Confirm import disabled; live re-check at confirm; fail-closed on ledger errors.

**Independent Test**: With known remaining balance, preview N > M shows warning and disabled confirm; N ≤ M leaves confirm enabled; live re-check blocks stale sufficient preview; ledger unavailable shows error not remaining zero.

**Depends on**: Phase 3 (Ensure and balance snapshot).

### Implementation for User Story 2

- [x] T028 [US2] Add credit balance snapshot fields (`WouldCreateCount`, `RemainingCredits`, `Shortfall`, `InsufficientCredits`, ledger error state) to `src/ImportToPlanner.Web/Features/Import/Workflows/WorkflowCoordinationState.cs`
- [x] T029 [US2] Invoke Ensure and compute would-create (count of `TaskActions` with `PlannedEntityAction.Create`) after successful preview in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs`
- [x] T030 [US2] Create `ImportCreditPreviewPresenter.cs` for UK English N/M/shortfall warning copy (no purchase CTA) in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportCreditPreviewPresenter.cs`
- [x] T031 [US2] Wire `MudAlert` credit warning and `canExecute` insufficient-credits clause in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor` and `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.WorkflowActions.razor.cs`
- [x] T032 [US2] Add live remaining re-check at confirm in `ImportWorkflowCoordinator.ExecuteAsync` in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs`
- [x] T033 [US2] Map ledger grant/expiry/read failures to UK English errors without showing remaining as zero in coordinator and Home status handling

### Tests for User Story 2

- [x] T034 [P] [US2] Add bUnit test — N > M shows warning and disables Confirm import in `tests/ImportToPlanner.Web.Tests/HomePageCreditPreviewTests.cs`
- [x] T035 [P] [US2] Add bUnit test — N ≤ M does not disable confirm for credit reasons in `tests/ImportToPlanner.Web.Tests/HomePageCreditPreviewTests.cs`
- [x] T036 [P] [US2] Add bUnit test — live re-check blocks confirm when another consumer reduced remaining below would-create in `tests/ImportToPlanner.Web.Tests/HomePageCreditPreviewTests.cs`
- [x] T037 [P] [US2] Add bUnit test — ledger unavailable on preview/confirm fails closed (error, no grant, remaining not zero) in `tests/ImportToPlanner.Web.Tests/HomePageCreditPreviewTests.cs`

**Checkpoint**: Preview and confirm gates enforce live credit balance fairly without consuming credits.

---

## Phase 5: User Story 3 - Execution Charges Only Created Tasks and Never Goes Negative (Priority: P1)

**Goal**: One credit per successfully created Planner task; stop starting new creates at zero; usage recorded before next create; retry usage record; credit-exhausted and usage-record-failure outcomes visible.

**Independent Test**: Execute import charging only created tasks; mid-run exhaustion stops further creates with no negative balance; usage-record failure keeps Planner task and stops run.

**Depends on**: Phase 3 (ledger consume); Phase 4 (Ensure at execute).

### Implementation for User Story 3

- [x] T038 [US3] Implement `ImportTaskCreationCreditQuota.cs` (`BeforeCreateAsync`, `RecordSuccessfulCreateAsync` with one retry) in `src/ImportToPlanner.Commercial/Credits/ImportTaskCreationCreditQuota.cs`
- [x] T039 [US3] Integrate quota `BeforeCreate` / `RecordSuccessfulCreate` around task `CreateTaskAsync` only (not buckets) in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`
- [x] T040 [US3] Emit `credits.exhausted` diagnostic outcomes for remaining not-started Create rows in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- [x] T041 [US3] Handle `credits.usage_record_failed` — keep Planner task, retry once, stop further creates, surface UK English error in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`
- [x] T042 [US3] Call Ensure at execute start in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs` before invoking execution use case

### Tests for User Story 3

- [x] T043 [P] [US3] Add `ImportTaskCreationCreditQuotaTests.cs` — one usage per successfully created task in `tests/ImportToPlanner.Tests/Credits/ImportTaskCreationCreditQuotaTests.cs`
- [x] T044 [P] [US3] Add test — execution stops at zero remaining without negative balance in `tests/ImportToPlanner.Tests/Credits/ImportTaskCreationCreditQuotaTests.cs`
- [x] T045 [P] [US3] Add test — bucket/reuse/skip/failed rows do not consume credits in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [x] T046 [P] [US3] Add test — created task kept when usage record fails after retry; no further creates start in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`

**Checkpoint**: Execution metering is bounded, auditable, and never drives balance below zero.

---

## Phase 6: User Story 4 - Import Summary Makes Usage Obvious (Priority: P2)

**Goal**: Import summary shows tasks created, credits used (free monthly), and remaining credits matching ledger-derived balance; credit-exhausted rows explained in UK English.

**Independent Test**: Complete commercial import and confirm summary figures match ledger; partial exhaustion run shows exhaustion copy.

**Depends on**: Phase 5 (usage recording during execute).

### Implementation for User Story 4

- [x] T047 [US4] Extend execution view model with `CreditsUsed`, `RemainingCredits`, and task-created count (tasks only, not buckets) in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- [x] T048 [US4] Render tasks created, credits used (free monthly), and remaining credits in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor`
- [x] T049 [US4] Ensure credit-exhausted and usage-record-failure rows show UK English explanations (not omitted) in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`

### Tests for User Story 4

- [x] T050 [P] [US4] Add bUnit test — summary shows created count, credits used, remaining matching ledger in `tests/ImportToPlanner.Web.Tests/HomePageCreditSummaryTests.cs`
- [x] T051 [P] [US4] Add bUnit test — partial run stopped for credit exhaustion shows exhaustion copy on summary in `tests/ImportToPlanner.Web.Tests/HomePageCreditSummaryTests.cs`

**Checkpoint**: Users see honest post-run accounting without a separate wallet page.

---

## Phase 7: User Story 5 - Self-Hosted and Commercial-Off Stay Unchanged (Priority: P1)

**Goal**: Commercial mode off requires no credit table setting; no credit UI, warnings, confirm blocks, or ledger writes; full import journey unchanged.

**Independent Test**: Run full import with commercial off — no credit copy, no ledger rows, confirm not credit-blocked.

**Depends on**: Phase 2 no-op quota (most wiring); verification after other stories.

### Implementation for User Story 5

- [x] T052 [US5] Ensure `AddCommercial` and credit registrations are skipped when `Features:CommercialMode:Enabled` is false in `src/ImportToPlanner.Commercial/DependencyInjection.cs` and `src/ImportToPlanner.Web/DependencyInjection.cs`
- [x] T053 [US5] Verify Home and import workflow show no billing or credit copy when commercial services are not registered in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`

### Tests for User Story 5

- [x] T054 [P] [US5] Extend `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs` — no credit `TableClient` when commercial off
- [x] T055 [P] [US5] Add bUnit commercial-off journey — no warning, confirm not credit-blocked, summary has no credit figures in `tests/ImportToPlanner.Web.Tests/HomePageCommercialOffCreditTests.cs`
- [x] T056 [US5] Update `tests/ImportToPlanner.Web.Tests/TestInfrastructure/HomePageTestContext.cs` with commercial on/off configuration helpers for credit scenarios

**Checkpoint**: Self-hosted and commercial-off deployments remain credit-free.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Architecture evidence, format gate, and manual validation per quickstart.md.

- [x] T057 [P] Extend `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` — Commercial must not reference Graph, Kiota, MudBlazor, or Stripe.net
- [x] T058 [P] Extend `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs` — credit table client registered only when commercial on
- [x] T059 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [x] T060 Run `dotnet test ImportToPlanner.slnx` and fix any failures
- [x] T061 Validate quickstart.md manual Aspire spot-check scenarios (commercial on: grant, warning, execute summary; commercial off: no credit copy)
- [x] T062 If AppHost is running after implementation, run `aspire resource web rebuild` (no new Aspire resources)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Foundational — **BLOCKS US2, US3, US4**
- **US2 (Phase 4)**: Depends on US1 (Ensure and balance)
- **US3 (Phase 5)**: Depends on US1 (consume); integrates with US2 (Ensure at execute)
- **US4 (Phase 6)**: Depends on US3 (usage figures during execute)
- **US5 (Phase 7)**: No-op path from Phase 2; verification after US2–US4 to confirm isolation
- **Polish (Phase 8)**: Depends on desired user stories being complete

### User Story Dependencies

| Story | Priority | Depends on | Independent test |
|-------|----------|------------|------------------|
| US1 | P1 | Foundational | Grant/expiry/idempotency via in-memory ledger tests |
| US2 | P1 | US1 | bUnit warning, disabled confirm, fail-closed |
| US3 | P1 | US1 (+ US2 execute Ensure) | Quota and execution use-case tests |
| US4 | P2 | US3 | bUnit summary figures |
| US5 | P1 | Foundational (no-op) | Registration and commercial-off bUnit |

### Within Each User Story

- Models and contracts before use cases
- Use cases before Web/coordinator wiring
- Implementation before story-specific tests (tests may be written in parallel where marked [P])
- Story checkpoint before moving to next priority

### Parallel Opportunities

- Phase 1: T002, T003, T004 in parallel
- Phase 2: T006–T012, T017, T018 in parallel after T002
- US1 tests: T022–T026 in parallel after T019
- US2 tests: T034–T037 in parallel after T031
- US3 tests: T043–T046 in parallel after T039
- US4 tests: T050–T051 in parallel after T048
- US5 tests: T054–T055 in parallel
- Polish: T057–T058 in parallel

---

## Parallel Example: User Story 1

```bash
# After T019 completes, launch US1 tests together:
Task T022: "EnsureCurrentCreditBalanceUseCaseTests.cs — first grant 25"
Task T023: "EnsureCurrentCreditBalanceUseCaseTests.cs — no second grant"
Task T024: "EnsureCurrentCreditBalanceUseCaseTests.cs — month boundary"
Task T025: "EnsureCurrentCreditBalanceUseCaseTests.cs — dormant tenant"
Task T026: "EnsureCurrentCreditBalanceUseCaseTests.cs — concurrent grant"
```

---

## Parallel Example: User Story 2

```bash
# After T031 completes, launch bUnit tests together:
Task T034: "HomePageCreditPreviewTests.cs — insufficient credits warning"
Task T035: "HomePageCreditPreviewTests.cs — confirm enabled when N ≤ M"
Task T036: "HomePageCreditPreviewTests.cs — live re-check at confirm"
Task T037: "HomePageCreditPreviewTests.cs — ledger unavailable fail closed"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Run US1 tests (T022–T027); confirm grant/expiry/idempotency
5. Demo ledger behaviour before UI gates

### Incremental Delivery

1. Setup + Foundational → credit contracts and no-op path ready
2. US1 → auditable monthly allowance (foundation for all metering)
3. US2 → preview warning and confirm block (main fairness control)
4. US3 → execution metering and exhaustion handling
5. US5 → verify commercial-off unchanged (can run in parallel with US2–US4 verification)
6. US4 → summary visibility (P2 polish on top of US3)
7. Polish → architecture scans, format gate, quickstart validation

### Parallel Team Strategy

With multiple developers after Foundational:

- Developer A: US1 (ledger core)
- Once US1 lands:
  - Developer A: US3 (execution quota)
  - Developer B: US2 (preview/confirm UI)
- Developer C: US5 verification + architecture tests (from Phase 2 onward)
- After US3: Developer B or A: US4 summary

---

## Notes

- Would-create = count of preview `TaskActions` with `PlannedEntityAction.Create` (not buckets, not file rows)
- `ImportWorkflowCoordinator` gates unchanged except live credit confirm compare and fail-closed ledger errors
- No Stripe, checkout, webhooks, free-account SKU, or purchase CTA in this increment
- Insufficient-credits presenter must not imply a completed purchase
- Sign-in may succeed when ledger fails; preview, confirm, and execute fail closed
- All tasks use UK English for user-facing copy
- Commit after each task or logical group

---

## Phase 9: Convergence

- [x] T063 Add `ImportExecutionUseCaseTests` case — preview with bucket Create, task Reuse, and task Skip rows does not call `BeforeCreate`/`RecordSuccessfulCreate` for non-task rows per T045/SC-008/quickstart #8 (missing)
- [x] T064 Add test — after grant applied, preview Ensure leaves remaining unchanged and writes no `Usage` transactions per SC-005/US2/AC1 in `tests/ImportToPlanner.Tests/Credits/EnsureCurrentCreditBalanceUseCaseTests.cs` or `tests/ImportToPlanner.Web.Tests/HomePageCreditWorkflowTests.cs` (partial)
- [x] T065 Add bUnit test — ledger unavailable at `ExecuteAsync` fails closed with no execution report per SC-015/T037 in `tests/ImportToPlanner.Web.Tests/HomePageCreditWorkflowTests.cs` (partial)
- [x] T066 Add `TableCreditLedgerStore` unit tests with `FakeTableClient` (grant marker idempotency, usage EGT) mirroring `CommercialAccountTableStoreTests.cs` per plan:008/009 adapter pattern (partial)

---

## Phase 10: Convergence

- [x] T067 Fix `HandleAsync_WithMetering_DoesNotCallQuotaForBucketReuseOrTaskSkip` in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs` — include planning-request CSV rows for all preview `Create` task actions (e.g. row 4 for "Brand New") so task creation succeeds and `RecordSuccessfulCreate` is exercised once per SC-008/quickstart #8/T063 (partial)
- [x] T068 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` and fix import ordering in `tests/ImportToPlanner.Tests/Credits/CreditLedgerTableStoreTests.cs` per T059/plan polish (partial)
- [x] T069 Run `dotnet test ImportToPlanner.slnx` and resolve any remaining failures so the quickstart automated suite passes per T060/Constitution VI (partial)

---

## Phase 11: Convergence

- [x] T070 Make free-month grant atomic or recoverable when lot/transaction batch fails after marker insert in `src/ImportToPlanner.Commercial/Credits/Storage/TableCreditLedgerStore.cs`; add store test for marker-without-lot partial failure per FR-030/FR-010/US1/AC1 (contradicts)
- [x] T071 Add bounded optimistic ETag-conflict retry to `RecordUsageAsync` (and `ExpireFreeLotAsync` if needed) in `src/ImportToPlanner.Commercial/Credits/Storage/TableCreditLedgerStore.cs` with concurrency tests per plan:008/009/research.md concurrency and FR-022 (partial)
- [x] T072 Gate `HomeExecutionReport.razor` success banner on `Errors.Count == 0` (or use warning/error severity for partial credit runs) and extend `tests/ImportToPlanner.Web.Tests/HomePageCreditSummaryTests.cs` per FR-023/FR-031/US3/AC4 (partial)
- [x] T073 Always render Confirm import on preview step with `Disabled` when credits block instead of hiding via `canExecute` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`; update `tests/ImportToPlanner.Web.Tests/HomePageCreditPreviewTests.cs` per FR-017/US2/AC2 (partial)

---

## Phase 12: Convergence

- [x] T074 Add `ImportExecutionUseCaseTests` case — multi-row preview where quota allows first create(s) then returns `Exhausted` on next `BeforeCreate`; assert partial `CreatedItems`, `CreditsUsed` matches created count, `credits.exhausted` failures for not-started rows, and `RemainingCredits` ≥ 0 per SC-009/US3/AC3–AC4/quickstart #8 (partial)
- [x] T075 Add `ImportTaskCreationCreditQuotaTests` case — first `RecordUsageAsync` fails then second succeeds; assert `Succeeded` true and store called twice per SC-016/FR-031/US3/AC5/quickstart #9 (partial)
- [x] T076 Add integration-style test — execute with in-memory ledger via real `ImportTaskCreationCreditQuota` + `EnsureCurrentCreditBalanceUseCase`, then fresh `EnsureAsync`; assert summary `CreditsUsed` and `RemainingCredits` match ledger-derived balance per SC-010/FR-025/US4/AC3 (partial)
- [x] T077 Add `EnsureCurrentCreditBalanceUseCaseTests` cases — store double fails `ExpireFreeLotAsync` or `TryGrantFreeMonthlyAsync`; assert `Failed` with `ExpiryFailed`/`GrantFailed` and no grant row on grant failure per SC-015/FR-030 (partial)
- [x] T078 Add `ImportExecutionUseCaseTests` case — first `BeforeCreate` returns `Unavailable`; assert execution stops with `credits.ledger_unavailable`, no further creates per SC-015/FR-030/contract §5 (partial)
- [x] T079 Add `ImportExecutionUseCaseTests` case — `CreateTaskAsync` throws on one Create row; assert `RecordSuccessfulCreate` not called and `CreditsUsed` unchanged per SC-008/FR-021/US3/AC1 (partial)
- [x] T080 Add bUnit or unit test — commercial Allow/CreateAccount sign-in invokes `EnsureAsync` with `EnsureBalanceReason.SignIn` per SC-001/US1/AC1/plan:T021 (partial)
- [x] T081 Extend `HomePageCommercialOffCreditTests` — journey through confirm + execute; assert no credit figures on summary and credit Ensure stub call count remains 0 per SC-011/US5/AC2 (partial)
- [x] T082 Add `CreditLedgerTableStoreTests` case — `ExpireFreeLotAsync` retries after 412 ETag conflict then succeeds, mirroring `RecordUsageAsync_WhenFirstTransactionConflictFails_RetriesAndSucceeds` per plan:T071/Constitution VII (partial)

---

## Phase 13: Convergence

- [x] T083 Re-read live remaining credits when binding preview-step Confirm import `Disabled` state (not only preview-time `creditBalanceSnapshot`) in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.WorkflowActions.razor.cs` and/or `ImportWorkflowCoordinator.cs`; add bUnit coverage that confirm enablement reflects live balance after snapshot changes per FR-018/FR-029/US2/AC6/SC-007/SC-013/contract §4 (partial)
- [x] T084 Add bUnit test — commercial sign-in still renders import workflow when `IEnsureCurrentCreditBalanceUseCase` returns `Failed` (not only exceptions) in `tests/ImportToPlanner.Web.Tests/HomePageCommercialAccessTests.cs` per FR-030/plan sign-in/Constitution VI (missing)
- [x] T085 Add integration test — `ImportExecutionUseCase` + real `ImportTaskCreationCreditQuota` when `RecordUsageAsync` fails twice; assert Planner task kept, no further creates, `credits.usage_record_failed` on summary per FR-031/SC-016/US3/AC5/Constitution VI (partial)
- [x] T086 Add `CreditLedgerTableStoreTests` case — mixed free and paid lots; `RecordUsageAsync` debits free lot first then oldest paid per FR-005/contract §5/Constitution VI (missing)
- [x] T087 Extend `CreditLedgerExecutionIntegrationTests` — multi-row preview with limited ledger balance; assert partial creates, `credits.exhausted` on not-started rows, and non-negative derived balance per SC-009/US3/AC3–AC4/Constitution VI (partial)
