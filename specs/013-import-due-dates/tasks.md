# Tasks: Import Due Dates From CSV

**Input**: Design documents from `/specs/013-import-due-dates/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Parser, planning, execution, and Graph adapter unit tests are required for this increment (spec FR-015, plan.md, quickstart.md, and engineering policies). Extend existing xUnit test classes only; no Playwright or AppHost tests.

**Organisation**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Parser adapter**: `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- **Application models**: `src/ImportToPlanner.Application/Models/`
- **Use cases**: `src/ImportToPlanner.Application/Services/`
- **Gateway**: `src/ImportToPlanner.Application/Abstractions/IPlannerGateway.cs`, `src/ImportToPlanner.Infrastructure.Graph/Planner/GraphPlannerGateway.cs`
- **Web UI**: `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`, `src/ImportToPlanner.Web/Features/Import/Presenters/ImportPlanningPresenter.cs`
- **Unit tests**: `tests/ImportToPlanner.Tests/`
- **Public docs**: `docs/csv-format.md`, `docs/troubleshooting.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm feature context and contracts before implementation

- [X] T001 Review parser, gateway, preview, and docs obligations in `specs/013-import-due-dates/contracts/import-due-dates-contracts.md` against current `ICsvImportParser` and `IPlannerGateway` signatures
- [X] T002 [P] Confirm quality gates and traceability checklist in `specs/013-import-due-dates/quickstart.md` match spec FR-001–FR-015 and issue [#131](https://github.com/markheydon/import-to-planner/issues/131)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared Application models, gateway contract, fingerprint, and stub updates that all user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add optional `DateOnly? DueDate` parameter to `CsvTaskRow` with XML documentation in `src/ImportToPlanner.Application/Models/CsvTaskRow.cs`
- [X] T004 Add optional `DateOnly? DueDate` parameter to `ImportTaskPlanItem` with XML documentation in `src/ImportToPlanner.Application/Models/ImportTaskPlanItem.cs`
- [X] T005 Extend `CreateTaskAsync` with `DateOnly? dueDate` (after `goal`, before `CancellationToken`) and XML docs in `src/ImportToPlanner.Application/Abstractions/IPlannerGateway.cs`
- [X] T006 [P] Update `CreateTaskAsync` signature and capture `dueDate` in `tests/ImportToPlanner.Tests/TestDoubles/PlannerGatewayStub.cs`
- [X] T007 [P] Update `CreateTaskAsync` signature in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/PlannerGatewayStub.cs`
- [X] T008 [P] Update all remaining `CreateTaskAsync` implementers and call sites (`GraphPlannerGateway.cs`, `ImportExecutionUseCase.cs`, `CreditLedgerExecutionIntegrationTests.cs`, `ImportExecutionUseCaseTests.cs` nested doubles) to accept the new parameter
- [X] T009 Include each row's due date as `yyyy-MM-dd` or empty in `ImportFingerprintBuilder.BuildRequestFingerprint` in `src/ImportToPlanner.Application/Services/ImportFingerprintBuilder.cs`

**Checkpoint**: Foundation ready — inner models and gateway contract carry `DateOnly?`; fingerprint will detect date edits

---

## Phase 3: User Story 1 - Preview optional due dates from CSV (Priority: P1) 🎯 MVP

**Goal**: Operators can include an optional `Due Date` column, upload the file, and see each row's interpreted due date (or row-level rejection) in preview before anything is created

**Independent Test**: Upload valid files with and without a Due Date column, plus a file with an invalid date on one row, and confirm preview shows recognised dates and blocks execution while that row error remains

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T010 [P] [US1] Add no Due Date column success test asserting `DueDate` is null in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T011 [P] [US1] Add empty Due Date cell success test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T012 [P] [US1] Add ISO `yyyy-MM-dd` and UK `dd/MM/yyyy` / `dd-MM-yyyy` parse success tests in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T013 [P] [US1] Add two-digit year (`31/05/26` → 2026-05-31) parse success test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T014 [P] [US1] Add invalid due date tests (`not-a-date`, `2026-05-31T17:00`) asserting `Field = "Due Date"` in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T015 [P] [US1] Add semicolon-delimited Due Date equivalence test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T016 [P] [US1] Update extra-column fixture test so canonical `Due Date` is supported and `Owner` remains the unexpected column in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T017 [P] [US1] Add planning preview tests asserting `ImportTaskPlanItem.DueDate` is populated and invalid parse errors block preview in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`

### Implementation for User Story 1

- [X] T018 [US1] Add `Due Date` to `SupportedHeaders` and `DueDateHeader` constant in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T019 [US1] Implement private `TryParseDueDate` with invariant `DateOnly.TryParseExact` format list and two-digit year window 2000–2099 in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T020 [US1] Parse Due Date in the row loop using Priority-style validate-then-`continue` and populate `CsvTaskRow.DueDate` in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T021 [US1] Carry `DueDate` from `CsvTaskRow` onto `ImportTaskPlanItem` (including skip rows) in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T022 [P] [US1] Add `DueDateDisplay` (UK `dd/MM/yyyy` or blank) to `ImportTaskActionViewModel` and map from `ImportTaskPlanItem.DueDate` in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportPlanningPresenter.cs`
- [X] T023 [US1] Add **Due date** column to the Task actions `MudDataGrid` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [X] T024 [US1] Update `tests/ImportToPlanner.Tests/Fixtures/with-extra-columns.csv` so `Owner` is the extra column under test and `Due Date` is a supported heading

**Checkpoint**: User Story 1 should be fully functional — preview shows dates, invalid rows block, files without the column still succeed

---

## Phase 4: User Story 2 - Create tasks with the CSV due date (Priority: P1)

**Goal**: Newly created Planner tasks carry the due date from the CSV row; empty or absent Due Date leaves tasks without a due date from this import

**Independent Test**: Confirm a preview that includes valid due dates and inspect created tasks (via gateway stub or Graph adapter tests) to confirm each new task receives the same calendar date as the CSV row

### Tests for User Story 2

- [X] T025 [P] [US2] Add execution test asserting `CreateTaskAsync` receives `DateOnly` when row action is Create in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T026 [P] [US2] Add execution test asserting `CreateTaskAsync` receives null due date when Due Date cell is empty in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T027 [P] [US2] Add Graph gateway test asserting POST body includes `dueDateTime` `2026-05-31T10:00:00Z` when due date is 31 May 2026 in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`
- [X] T028 [P] [US2] Add Graph gateway test asserting `dueDateTime` is omitted when `dueDate` is null in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`

### Implementation for User Story 2

- [X] T029 [US2] Pass `sourceRow.DueDate` into `plannerGateway.CreateTaskAsync` from create path in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`
- [X] T030 [US2] Map `dueDate` to Graph `DueDateTime` at 10:00 UTC on the calendar date in `src/ImportToPlanner.Infrastructure.Graph/Planner/GraphPlannerGateway.cs`
- [X] T031 [US2] Verify execution remains blocked when parse result still has `Due Date` row errors (coordinator `HasErrors` path unchanged) in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`

**Checkpoint**: User Story 2 should be fully functional — create path sets Planner due date; invalid preview still blocks execution

---

## Phase 5: User Story 3 - Leave existing tasks unchanged (Priority: P2)

**Goal**: Name-matched existing tasks stay skipped; CSV due dates are shown in preview but never written to existing cards

**Independent Test**: Preview and execute a file mixing new titles with titles that already exist in the destination, including due dates on both, and confirm existing matches are reported as already exists with no due-date create or update call

### Tests for User Story 3

- [X] T032 [P] [US3] Add planning test asserting name-matched row action is Skip with `Reason` already exists while `DueDate` is still populated for display in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T033 [P] [US3] Add execution test asserting skip/already-exists rows do not call `CreateTaskAsync` (and no update API) when CSV includes a due date in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T034 [P] [US3] Add mixed new-and-existing file execution test asserting only newly created tasks receive due dates in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`

### Implementation for User Story 3

- [X] T035 [US3] Confirm skip path in `ImportExecutionUseCase` never invokes create or PATCH for due date on already-exists rows in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`

**Checkpoint**: User Story 3 should be fully functional — skip-only semantics preserved; CSV dates visible but not applied to existing tasks

---

## Phase 6: User Story 4 - Document due dates on the full sample only (Priority: P3)

**Goal**: Public CSV guidance and in-app accepted-field cues list optional Due Date with allowed shapes; full example includes Due Date; minimal example stays Task Name only

**Independent Test**: Review `docs/csv-format.md`, `docs/troubleshooting.md`, Home accepted-fields copy, and minimal vs full examples; confirm Due Date appears only on the full example and in the accepted-columns list

### Implementation for User Story 4

- [X] T036 [P] [US4] List `Due Date` as optional, document ISO and UK day-first formats, two-digit years (2000–2099), invalid-value row failure, and add Due Date to the full example only in `docs/csv-format.md`
- [X] T037 [P] [US4] Add troubleshooting bullet for invalid Due Date as a row-level CSV validation error in `docs/troubleshooting.md`
- [X] T038 [US4] Extend compact accepted-fields line to include optional **Due Date** in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [X] T039 [P] [US4] Update `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs` (or equivalent accepted-fields assertion) if it asserts the accepted-fields copy

**Checkpoint**: User Story 4 should be complete — operators can learn optional Due Date from docs and in-app guidance

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Quality gates, architecture compliance, and quickstart validation

- [X] T040 [P] Add fingerprint test asserting due date change alters request fingerprint in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T041 Run `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests|ImportPlanningUseCaseTests|ImportExecutionUseCaseTests|GraphPlannerGatewayTests|ArchitectureComplianceTests"` and fix failures
- [X] T042 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` and fix formatting
- [X] T043 Validate manual scenarios in `specs/013-import-due-dates/quickstart.md` section 5 (optional first-run)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational — delivers MVP preview
- **User Story 2 (Phase 4)**: Depends on Foundational; logically follows US1 parser output but gateway wiring can proceed in parallel after Phase 2
- **User Story 3 (Phase 5)**: Depends on US1 planning preview and US2 create path being in place
- **User Story 4 (Phase 6)**: Can start after US1 parser behaviour is stable; docs may reference final format list from contracts
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational — no dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational — needs `CsvTaskRow.DueDate` from parser (US1) for end-to-end create, but gateway mapping can be built and tested with stubs first
- **User Story 3 (P2)**: Depends on US1 preview carrying `DueDate` on skip rows and US2 create path — independently testable via execution doubles
- **User Story 4 (P3)**: Largely independent once accepted format list is final — can run in parallel with US2/US3 after US1 parser formats are settled

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Parser models before planning preview
- Planning preview before execution create
- Gateway adapter after `IPlannerGateway` signature change
- Docs after behaviour is stable

### Parallel Opportunities

- T002 can run parallel to T001
- T006, T007, T008 can run in parallel after T005
- All US1 parser tests (T010–T016) can run in parallel
- T022 can run parallel to T021 once `ImportTaskPlanItem` has `DueDate`
- US2 Graph tests (T027, T028) can run in parallel
- US3 tests (T032–T034) can run in parallel
- US4 doc tasks (T036, T037, T039) can run in parallel
- T040 can run parallel to final test sweep

---

## Parallel Example: User Story 1

```bash
# Launch all parser tests together:
Task: "Add no Due Date column success test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task: "Add ISO and UK parse success tests in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task: "Add invalid due date tests in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"

# After parser implementation, UI binding in parallel:
Task: "Add DueDateDisplay to ImportPlanningPresenter.cs"
Task: "Update with-extra-columns.csv fixture"
```

---

## Parallel Example: User Story 2

```bash
# Gateway tests in parallel:
Task: "Graph gateway test for dueDateTime at 10:00Z in GraphPlannerGatewayTests.cs"
Task: "Graph gateway test omitting dueDateTime when null in GraphPlannerGatewayTests.cs"

# Then wire execution and adapter:
Task: "Pass sourceRow.DueDate in ImportExecutionUseCase.cs"
Task: "Map dueDateTime in GraphPlannerGateway.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (parser + preview)
4. **STOP and VALIDATE**: Upload CSV with/without Due Date; confirm preview and row-level errors
5. Deploy/demo preview safety before create wiring

### Incremental Delivery

1. Complete Setup + Foundational → contract and models ready
2. Add User Story 1 → Test preview independently → Demo (MVP!)
3. Add User Story 2 → Test create with due date → Demo
4. Add User Story 3 → Test skip unchanged → Demo
5. Add User Story 4 → Docs and in-app cues → Demo
6. Polish → format, full test sweep, quickstart validation

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (parser + preview UI)
   - Developer B: User Story 2 (execution + Graph adapter) after T005
   - Developer C: User Story 4 (docs) once format list is agreed
3. User Story 3 tests land after US1/US2 checkpoints
4. Polish phase shared

---

## Notes

- Canonical heading is exactly `Due Date` (trim + case-insensitive match). Do not accept `DueDate`, `Deadline`, or alias variants in this increment
- Graph `dueDateTime` uses **10:00 UTC** on the calendar date, not midnight UTC
- Skip rows show CSV due date in preview but must not write to Planner
- Reuse Priority's validate-then-`continue` pattern for Due Date parsing
- After code changes, run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` before merge
- If AppHost is running after Web changes, rebuild the web resource per AGENTS.md
