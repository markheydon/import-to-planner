---

description: "Task list for Complimentary Tenant Credits feature implementation"
---

# Tasks: Complimentary Tenant Credits

**Input**: Design documents from `/specs/019-complimentary-tenant-credits/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/complimentary-tenant-credits-contracts.md, quickstart.md. **Requires** shipped [#126](https://github.com/markheydon/import-to-planner/issues/126) credit ledger (`specs/011-commercial-credit-ledger`).

**Tests**: Required per spec FR/issue [#129](https://github.com/markheydon/import-to-planner/issues/129) acceptance criteria and quickstart.md (xUnit v3, NSubstitute, bUnit; no AppHost tests).

**Organization**: Tasks grouped by user story (US1–US4 from spec.md).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US4
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/ImportToPlanner.{AppHost,Commercial,Application,Web}/`
- **Tests**: `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`
- **Docs**: `docs/credits-billing-usage-model.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Deploy-time allowlist configuration wiring (empty default; no tenant GUIDs in source).

- [x] T001 Add AppHost parameter `complimentaryTenantIds` (default empty) and map to `Features__CommercialMode__ComplimentaryTenantIds` on the web resource in `src/ImportToPlanner.AppHost/AppHost.cs`
- [x] T002 Add `CommercialCreditModeOptions` with `ComplimentaryTenantIds` string property (default `""`) in `src/ImportToPlanner.Commercial/Configuration/CommercialCreditModeOptions.cs` and bind `Features:CommercialMode:ComplimentaryTenantIds` via `IOptionsMonitor` in `src/ImportToPlanner.Commercial/DependencyInjection.cs` (Commercial-owned options only; do not reference Web `CommercialModeOptions`)
- [x] T003 [P] Document empty-default operator configuration in `docs/credits-billing-usage-model.md` (parameter/key name only; no production GUIDs)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Policy, ledger extensions, ensure/quota/store behaviour shared by all stories. **No user story work until complete.**

**⚠️ CRITICAL**: Complimentary unlimited access depends on [#126](https://github.com/markheydon/import-to-planner/issues/126) ledger being present.

- [x] T004 [P] Add `LotType.Complimentary` and `CreditEntryType.ComplimentaryGrant` to `src/ImportToPlanner.Commercial/Models/CreditLedgerEnums.cs`
- [x] T005 [P] Add `ComplimentaryUnlimitedAccess` bool to `CommercialCreditBalanceResult` factory/helpers in `src/ImportToPlanner.Commercial/Models/CommercialCreditBalanceModels.cs`
- [x] T006 [P] Define `IComplimentaryTenantPolicy` with `IsComplimentary(string tenantId)` in `src/ImportToPlanner.Commercial/Abstractions/IComplimentaryTenantPolicy.cs`
- [x] T007 Implement `ComplimentaryTenantPolicy.cs` — read allowlist from `IOptionsMonitor<CommercialCreditModeOptions>`; parse comma-separated `ComplimentaryTenantIds` on each `IsComplimentary` call; trim; skip empties; `Guid.TryParse` per segment; log invalid tokens; match tenant via `Guid.TryParse` + `Guid.Equals` (no substring) in `src/ImportToPlanner.Commercial/Credits/ComplimentaryTenantPolicy.cs`
- [x] T008 Register `IComplimentaryTenantPolicy` as singleton when commercial mode is composed in `src/ImportToPlanner.Commercial/DependencyInjection.cs`
- [x] T009 Extend `ICreditLedgerStore` with complimentary paired recording (e.g. `RecordComplimentaryUsageAsync`) in `src/ImportToPlanner.Commercial/Abstractions/ICreditLedgerStore.cs`
- [x] T010 Implement paired `Usage` + `ComplimentaryGrant` (quantity 1 each, same import run/task refs) without consuming `FreeMonthly`/`Paid` lots in `src/ImportToPlanner.Commercial/Credits/Storage/TableCreditLedgerStore.cs`
- [x] T011 [P] Extend `tests/ImportToPlanner.Tests/TestInfrastructure/InMemoryCreditLedgerStore.cs` for complimentary paired usage
- [x] T012 Branch `EnsureCurrentCreditBalanceUseCase.cs` — when policy is complimentary: skip free expiry and `TryGrantFreeMonthlyAsync`; return success with `ComplimentaryUnlimitedAccess = true` and numeric remaining fields 0 in `src/ImportToPlanner.Commercial/Credits/EnsureCurrentCreditBalanceUseCase.cs`
- [x] T013 Branch `ImportTaskCreationCreditQuota.cs` — complimentary: `BeforeCreateAsync` always `Allow` (except ledger unavailable); `RecordSuccessfulCreateAsync` calls complimentary store method; never `Exhausted` for credit zero in `src/ImportToPlanner.Commercial/Credits/ImportTaskCreationCreditQuota.cs`

**Checkpoint**: Complimentary tenants can ensure balance, record usage, and execute without exhaustion in Commercial layer.

---

## Phase 3: User Story 1 - Operator-Designated Tenants Import Without Credit Walls (Priority: P1) 🎯 MVP

**Goal**: Allowlisted commercial tenants never blocked at confirm for credits; unlimited remaining in UI; no buy-credits CTA; usage still shown on summary.

**Independent Test**: Configure one allowlisted tenant GUID; preview would-create &gt; 25; confirm enabled; execute records credits used; remaining shows Unlimited.

### Tests for User Story 1

- [x] T014 [P] [US1] Add `ComplimentaryTenantPolicyTests.cs` — empty list, exact GUID match, non-match, whitespace, duplicate GUIDs in `tests/ImportToPlanner.Tests/Credits/ComplimentaryTenantPolicyTests.cs`
- [x] T015 [P] [US1] Add `ComplimentaryCreditEnsureTests.cs` — complimentary tenant skips `FreeGrant`/`FreeExpiry` and sets `ComplimentaryUnlimitedAccess` in `tests/ImportToPlanner.Tests/Credits/ComplimentaryCreditEnsureTests.cs`
- [x] T016 [P] [US1] Add `ComplimentaryCreditQuotaTests.cs` — never returns `Exhausted` for complimentary; large import completes in `tests/ImportToPlanner.Tests/Credits/ComplimentaryCreditQuotaTests.cs`
- [x] T017 [P] [US1] Add bUnit tests — no insufficient-credits alert, confirm not credit-disabled, unlimited copy when allowlisted in `tests/ImportToPlanner.Web.Tests/ComplimentaryCreditPreviewTests.cs`
- [x] T038 [P] [US1] Add bUnit tests — allowlisted tenant with ledger ensure/read failure: preview shows ledger-unavailable (not insufficient-credits alert); confirm/`canExecute` stays credit-fail-closed; execution path does not show numeric zero exhaustion framing — per FR-016 and quickstart item 10 in `tests/ImportToPlanner.Web.Tests/ComplimentaryCreditPreviewTests.cs` (and related Web credit tests if split)

### Implementation for User Story 1

- [x] T018 [US1] Add `ComplimentaryUnlimitedAccess` to `src/ImportToPlanner.Web/Features/Import/Workflows/WorkflowCreditBalanceSnapshot.cs`
- [x] T019 [US1] Map ensure outcome to snapshot; force `InsufficientCredits = false` when complimentary in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs`
- [x] T020 [US1] Skip credit-based `canExecute` disable when snapshot is complimentary in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.WorkflowActions.razor.cs`
- [x] T021 [US1] Hide insufficient-credits `MudAlert` and show Unlimited/complimentary remaining when complimentary in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T022 [P] [US1] Add unlimited remaining and complimentary credits-used labelling in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportCreditPreviewPresenter.cs` and `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- [x] T023 [US1] Render Unlimited remaining (not numeric 0 exhausted) on execution report in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor`
- [x] T024 [US1] Omit or guard buy-credits CTA when complimentary (including placeholders for #125) in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor` and related commercial credit UI files

**Checkpoint**: Allowlisted tenant full import journey works end-to-end with unlimited presentation.

---

## Phase 4: User Story 4 - Usage Remains Auditable on the Ledger (Priority: P1)

*Phase order: US4 follows US1 here because ledger audit tests depend on Phase 2 store/quota; US2 regression (Phase 5) can run in parallel once Phase 2 completes.*

**Goal**: Each created task writes usage; paired complimentary grants; derived balance never negative.

**Independent Test**: Two imports for allowlisted tenant; ledger shows usage totals; no negative derived balance.

### Tests for User Story 4

- [x] T025 [P] [US4] Add `ComplimentaryLedgerRecordingTests.cs` — N creates ⇒ N `Usage` and N `ComplimentaryGrant`; derived balance ≥ 0 in `tests/ImportToPlanner.Tests/Credits/ComplimentaryLedgerRecordingTests.cs`
- [x] T026 [P] [US4] Extend `CreditLedgerExecutionIntegrationTests.cs` with complimentary allowlist scenario (credits used = creates; no mid-run exhaustion stop) in `tests/ImportToPlanner.Tests/Credits/CreditLedgerExecutionIntegrationTests.cs`

### Implementation for User Story 4

- [x] T027 [US4] After US1 Web wiring (T018–T023), verify import execution summary fields: map complimentary `CreditsUsed` and unlimited remaining via existing neutral Application/Web presenter paths; touch `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs` only if a gap remains — do not reference Commercial types in Application

**Checkpoint**: Operator can audit complimentary usage from ledger transactions.

---

## Phase 5: User Story 2 - Normal Commercial Tenants Are Unchanged (Priority: P1)

**Goal**: Non-allowlisted tenants keep #126 preview gate, monthly grant, exhaustion, and buy-credit paths.

**Independent Test**: Empty allowlist or excluded tenant repeats 011 insufficient-credit and exhaustion scenarios.

### Tests for User Story 2

- [x] T028 [P] [US2] Add `ComplimentaryCreditRegressionTests.cs` — empty allowlist preserves insufficient preview/confirm and mid-run exhaustion in `tests/ImportToPlanner.Tests/Credits/ComplimentaryCreditRegressionTests.cs`
- [x] T029 [P] [US2] Add bUnit regression — non-allowlisted tenant still shows insufficient warning when N &gt; M in `tests/ImportToPlanner.Web.Tests/ComplimentaryCreditPreviewTests.cs`
- [x] T030 [US2] Run full existing credit test suite unchanged (`tests/ImportToPlanner.Tests/Credits/`, relevant `ImportToPlanner.Web.Tests`) and fix only if this feature caused regressions

**Checkpoint**: Default commercial metering unchanged when allowlist empty or tenant not listed.

---

## Phase 6: User Story 3 - Operator Configures the Allowlist Safely (Priority: P2)

**Goal**: Malformed tokens ignored with diagnostics; valid GUIDs in same string still apply; commercial off ignores allowlist.

**Independent Test**: Mix invalid token + valid GUID; only valid tenant gets complimentary; commercial off ignores config.

### Tests for User Story 3

- [x] T031 [P] [US3] Add policy tests — `"not-a-guid,<valid-guid>"` logs and still allowlists valid GUID only in `tests/ImportToPlanner.Tests/Credits/ComplimentaryTenantPolicyTests.cs`
- [x] T032 [P] [US3] Add test — `Features:CommercialMode:Enabled=false` with allowlist set does not register complimentary behaviour (no-op quota path) in `tests/ImportToPlanner.Web.Tests/ComplimentaryCreditCommercialOffTests.cs`
- [x] T033 [P] [US3] Add test — `IOptionsMonitor` configuration change removes complimentary on next preview without new sign-in in `tests/ImportToPlanner.Tests/Credits/ComplimentaryTenantPolicyTests.cs` or Web tests with options reload

### Implementation for User Story 3

- [x] T034 [US3] Verify `IOptionsMonitor<CommercialCreditModeOptions>` reload path — allowlist change applies on next credit action without new sign-in (extend T033 coverage; implementation covered by T002/T007) in `src/ImportToPlanner.Commercial/Credits/ComplimentaryTenantPolicy.cs`

**Checkpoint**: Operator-safe parsing and live config refresh validated.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Product contract, quality gates, traceability.

- [x] T035 [P] Complete complimentary tenants section in `docs/credits-billing-usage-model.md` (ledger pairing, UI, relation to free/paid credits, operator config)
- [x] T036 Run `dotnet test ImportToPlanner.slnx` and `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [x] T037 Validate scenarios in `specs/019-complimentary-tenant-credits/quickstart.md` manually or via automated coverage mapping

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** → **Phase 2** → **US1 (Phase 3)** is MVP spine
- **Phase 4 (US4)** depends on Phase 2 store/quota; tests can follow US1 Web wiring
- **Phase 5 (US2)** after Phase 2 (regression against 011 behaviour)
- **Phase 6 (US3)** policy details can start after T007; T034 completes live reload
- **Phase 7** after desired stories complete

### User Story Dependencies

| Story | Depends on | Notes |
|-------|------------|--------|
| US1 | Phase 2 | MVP — unlimited UI + gates |
| US4 | Phase 2 | Ledger audit; integrates with US1 execution |
| US2 | Phase 2 | Regression; no new product code if Phases 2–3 correct |
| US3 | T007 | Parser/logging; T034 for options monitor |

### Parallel Opportunities

- Phase 1: T003 parallel with T001–T002 after options property exists
- Phase 2: T004–T006, T011 parallel; T010 after T009
- US1 tests T014–T017 parallel before implementation T018–T024
- US2/US3 test tasks marked [P] parallel across files

---

## Parallel Example: User Story 1

```bash
# Tests first (fail before implementation):
tests/ImportToPlanner.Tests/Credits/ComplimentaryTenantPolicyTests.cs
tests/ImportToPlanner.Tests/Credits/ComplimentaryCreditQuotaTests.cs
tests/ImportToPlanner.Web.Tests/ComplimentaryCreditPreviewTests.cs

# Then Web presenters in parallel:
src/ImportToPlanner.Web/Features/Import/Presenters/ImportCreditPreviewPresenter.cs
src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1–2 (config + Commercial complimentary path)
2. Complete Phase 3 (US1) + minimal US4 tests (T025)
3. **STOP and VALIDATE** with quickstart manual spot-check
4. Add US2 regression, US3 polish, Phase 7 doc/format

### Incremental Delivery

1. Foundation → allowlisted execution works in tests without UI
2. US1 UI → demo unlimited journey
3. US4 audit tests → operator visibility
4. US2 regression → safe for production default tenants
5. US3 + docs → operator runbook complete

---

## Notes

- Traceability: GitHub [#129](https://github.com/markheydon/import-to-planner/issues/129), spec FR-001–FR-017 (38 tasks T001–T038)
- Do not commit production tenant GUIDs; use user secrets / local env for manual tests
- No AppHost integration tests per issue acceptance criteria
- After AppHost code changes during implement: `aspire resource web rebuild` when AppHost is running (`AGENTS.md`)
