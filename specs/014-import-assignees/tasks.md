# Tasks: Optional Assignees From CSV

**Input**: Design documents from `/specs/014-import-assignees/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Parser, planning, execution, Graph adapter, and architecture compliance unit tests are required for this increment (spec FR-018, plan.md, quickstart.md, and engineering policies). Extend existing xUnit test classes only; no Playwright or AppHost tests.

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
- **Web UI**: `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`, `src/ImportToPlanner.Web/Features/Import/Presenters/ImportPlanningPresenter.cs`, `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- **Unit tests**: `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`
- **Public docs**: `docs/csv-format.md`, `docs/troubleshooting.md`
- **Internal docs**: `docs-internal/entra-app-registration-setup.md`, `docs-internal/microsoft-graph-guidelines.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm feature context and contracts before implementation

- [X] T001 Review parser, gateway, preview, execution, and docs obligations in `specs/014-import-assignees/contracts/import-assignees-contracts.md` against current `ICsvImportParser` and `IPlannerGateway` signatures
- [X] T002 [P] Confirm quality gates and traceability checklist in `specs/014-import-assignees/quickstart.md` match spec FR-001–FR-019 and issue [#132](https://github.com/markheydon/import-to-planner/issues/132)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared Application models, gateway contract, fingerprint, and stub updates that all user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add `PlanMember` record (`Id`, `Mail`, `SignInName`) with XML documentation in `src/ImportToPlanner.Application/Models/PlanMember.cs`
- [X] T004 Add `CreatedPlannerTask` record (`Snapshot`, `AppliedAssigneeIds`) with XML documentation in `src/ImportToPlanner.Application/Models/CreatedPlannerTask.cs`
- [X] T005 Add `UnresolvedAssignee` record (`Address`, `ReasonCode`) with XML documentation in `src/ImportToPlanner.Application/Models/UnresolvedAssignee.cs`
- [X] T006 Add `AssigneeAddresses` (`IReadOnlyList<string>`, default empty) to `CsvTaskRow` with XML documentation in `src/ImportToPlanner.Application/Models/CsvTaskRow.cs`
- [X] T007 Extend `ImportTaskPlanItem` with `AssigneeAddresses`, `ResolvedAssigneeIds`, and `UnresolvedAssignees` with XML documentation in `src/ImportToPlanner.Application/Models/ImportTaskPlanItem.cs`
- [X] T008 Extend `ManualAction` with optional `PersonIdentifier` and document `AssignPersonToTask` action type in `src/ImportToPlanner.Application/Models/ManualAction.cs`
- [X] T009 Add `GetPlanMembersAsync(containerId, containerType, cancellationToken)` returning `IReadOnlyList<PlanMember>` with XML docs in `src/ImportToPlanner.Application/Abstractions/IPlannerGateway.cs`
- [X] T010 Change `CreateTaskAsync` to accept `IReadOnlyList<string> assigneeUserIds` (after `dueDate`, before `CancellationToken`) and return `CreatedPlannerTask` with XML docs in `src/ImportToPlanner.Application/Abstractions/IPlannerGateway.cs`
- [X] T011 [P] Update `GetPlanMembersAsync` and `CreateTaskAsync` in `tests/ImportToPlanner.Tests/TestDoubles/PlannerGatewayStub.cs` (seed at least one member with mail + UPN and one guest member for later tests)
- [X] T012 [P] Update `GetPlanMembersAsync` and `CreateTaskAsync` in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/PlannerGatewayStub.cs`
- [X] T013 Update all remaining `CreateTaskAsync` implementers and call sites (`GraphPlannerGateway.cs`, `ImportExecutionUseCase.cs`, `CreditLedgerExecutionIntegrationTests.cs`, `ImportExecutionUseCaseTests.cs` nested doubles) to accept assignee ids and return `CreatedPlannerTask`
- [X] T014 Include each row's normalised assignee addresses (joined in stored order) in `ImportFingerprintBuilder.BuildRequestFingerprint` in `src/ImportToPlanner.Application/Services/ImportFingerprintBuilder.cs`

**Checkpoint**: Foundation ready — inner models and gateway contract carry assignee data; fingerprint will detect assignee edits

---

## Phase 3: User Story 1 - Preview optional assignees from CSV (Priority: P1) 🎯 MVP

**Goal**: Operators can include an optional `Assigned To` column, upload the file, and see who will be assigned versus who needs manual follow-up before anything is created; lookup failure with addresses present blocks preview

**Independent Test**: Upload files with no Assigned To column, blank cells, matching members, unmatched addresses, and mixed cells; confirm preview lists intended assignees and follow-up people without blocking execution solely because some people cannot be assigned; confirm member lookup failure blocks preview

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T015 [P] [US1] Add no Assigned To column success test asserting empty `AssigneeAddresses` in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T016 [P] [US1] Add empty and whitespace-only Assigned To cell success tests in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T017 [P] [US1] Add comma and semicolon list split tests (`a@contoso.com; b@contoso.com`) in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T018 [P] [US1] Add quoted multi-address cell test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T019 [P] [US1] Add duplicate ignore-case collapse test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T020 [P] [US1] Add non-address text (`Not a person`) still succeeds as a row test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T021 [P] [US1] Add semicolon-delimited file Assigned To equivalence test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T022 [P] [US1] Update extra-column fixture test so canonical `Assigned To` is supported and `Owner` remains the unexpected column in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T023 [P] [US1] Add planning test asserting `GetPlanMembersAsync` is not called when no assignee addresses exist in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T024 [P] [US1] Add planning test asserting member lookup failure sets `HasValidationErrors` with `Field = "Assigned To"` in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T025 [P] [US1] Add planning tests for mail and UPN match resolving member ids on Create rows in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T026 [P] [US1] Add planning test asserting guest already in destination resolves in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T027 [P] [US1] Add planning test asserting alias-only values are unresolved with `not-a-member` in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T028 [P] [US1] Add planning test for mixed cell showing resolved assignees and unresolved follow-up without blocking preview in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`

### Implementation for User Story 1

- [X] T029 [US1] Add `Assigned To` to `SupportedHeaders` and `AssignedToHeader` constant in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T030 [US1] Implement private assignee list splitting (comma/semicolon, trim, drop empties, ordinal ignore-case de-dupe) in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T031 [US1] Parse Assigned To in the row loop and populate `CsvTaskRow.AssigneeAddresses` in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T032 [US1] Implement `GetPlanMembersAsync` in `src/ImportToPlanner.Infrastructure.Graph/Planner/GraphPlannerGateway.cs` (paginated group users with `id`, `mail`, `userPrincipalName`; `/me` for user containers; fail closed for roster when members cannot be listed)
- [X] T033 [US1] Call `GetPlanMembersAsync` only when any row has assignee addresses; map lookup failure to request-level `ImportValidationError` (`RowNumber = 0`, `Field = "Assigned To"`) in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T034 [US1] Match CSV values to `PlanMember.Mail` or `PlanMember.SignInName` (ordinal ignore-case); populate `ResolvedAssigneeIds` and `UnresolvedAssignees` on Create rows in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T035 [US1] Carry assignee fields from `CsvTaskRow` onto `ImportTaskPlanItem` (including skip rows for display) in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T036 [P] [US1] Add `AssignedToDisplay` (matched vs follow-up people) to `ImportTaskActionViewModel` and map from plan item assignee fields in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportPlanningPresenter.cs`
- [X] T037 [US1] Add **Assigned to** column to the Task actions `MudDataGrid` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [X] T038 [US1] Update `tests/ImportToPlanner.Tests/Fixtures/with-extra-columns.csv` so `Owner` is the extra column under test and `Assigned To` is a supported heading

**Checkpoint**: User Story 1 should be fully functional — preview shows assignees and follow-up; lookup failure blocks; files without the column still succeed

---

## Phase 4: User Story 2 - Create tasks with resolved assignees (Priority: P1)

**Goal**: Newly created Planner tasks receive every listed person who can be assigned at create time, including guests already in the destination

**Independent Test**: Confirm a preview with fully resolved assignees and inspect created tasks (via gateway stub or Graph adapter tests) to confirm those people are assigned

### Tests for User Story 2

- [X] T039 [P] [US2] Add execution test asserting `CreateTaskAsync` receives resolved assignee ids when row action is Create in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T040 [P] [US2] Add execution test asserting `CreateTaskAsync` receives empty assignee list when Assigned To is absent or blank in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T041 [P] [US2] Add execution test asserting guest member id is passed on create in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T042 [P] [US2] Add Graph gateway test asserting POST body includes `assignments` keys for given user ids in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`
- [X] T043 [P] [US2] Add Graph gateway test asserting `assignments` is omitted when assignee list is empty in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`
- [X] T044 [P] [US2] Add Graph gateway test for paginated group member lookup in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`

### Implementation for User Story 2

- [X] T045 [US2] Pass `ResolvedAssigneeIds` into `plannerGateway.CreateTaskAsync` from create path and use `CreatedPlannerTask.Snapshot` in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`
- [X] T046 [US2] Set `plannerTask.assignments` on create POST (`plannerAssignment` + `orderHint`) and return `CreatedPlannerTask` with applied ids in `src/ImportToPlanner.Infrastructure.Graph/Planner/GraphPlannerGateway.cs`
- [X] T047 [US2] Verify execution remains blocked when planning result still has request-level Assigned To lookup errors (`HasValidationErrors` path unchanged) in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`

**Checkpoint**: User Story 2 should be fully functional — create path assigns resolved members; empty assignees leave tasks unassigned

---

## Phase 5: User Story 3 - Keep going when someone cannot be assigned (Priority: P1)

**Goal**: Unknown or otherwise unassignable addresses produce clear manual follow-up items; tasks are still created; execute-time assignment failures do not block other rows

**Independent Test**: Import a file mixing destination members (including guests) with unknown addresses and execute-time refusals; confirm every new task is created and every unassigned person appears in the report

### Tests for User Story 3

- [X] T048 [P] [US3] Add execution test for mixed cell: task created, assignable people assigned, `AssignPersonToTask` for each unresolved address in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T049 [P] [US3] Add execution test for all-unassignable row: task created unassigned with follow-up for each person in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T050 [P] [US3] Add execution test for preview-resolved id missing from `AppliedAssigneeIds` at execute time still creating task and emitting follow-up in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T051 [P] [US3] Add Graph gateway test retrying create without assignments when Graph rejects assignments, returning empty `AppliedAssigneeIds` in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`
- [X] T052 [P] [US3] Add presenter test for `AssignPersonToTask` UK display and reason codes in `tests/ImportToPlanner.Web.Tests/ImportPresenterTests.cs`

### Implementation for User Story 3

- [X] T053 [US3] Emit `AssignPersonToTask` manual actions for unresolved CSV addresses and for intended ids not in `AppliedAssigneeIds` (with stable reason codes) in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`
- [X] T054 [US3] Implement create-without-assignments retry when assignment payload causes create failure in `src/ImportToPlanner.Infrastructure.Graph/Planner/GraphPlannerGateway.cs`
- [X] T055 [US3] Map `AssignPersonToTask` to UK English display using reason codes (`not-a-member`, `not-an-address`, `assignment-refused`, `destination-limit`) and show `PersonIdentifier` in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`

**Checkpoint**: User Story 3 should be fully functional — create-and-follow-up pattern matches Goal; import continues per row

---

## Phase 6: User Story 4 - Leave existing tasks unassigned from CSV (Priority: P2)

**Goal**: Name-matched existing tasks stay skipped; CSV Assigned To must not add, replace, or clear people on those cards

**Independent Test**: Preview and execute a file mixing new titles with titles that already exist, including Assigned To on both, and confirm existing matches are reported as already exists with no assignment change or assignee follow-up

### Tests for User Story 4

- [X] T056 [P] [US4] Add planning test asserting name-matched row action is Skip with `Reason` already exists while assignee addresses remain visible but `ResolvedAssigneeIds` is empty in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T057 [P] [US4] Add execution test asserting skip/already-exists rows do not call `CreateTaskAsync` and do not emit `AssignPersonToTask` when CSV includes Assigned To in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T058 [P] [US4] Add mixed new-and-existing file execution test asserting only newly created tasks receive CSV assignees in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`

### Implementation for User Story 4

- [X] T059 [US4] Confirm skip path in `ImportExecutionUseCase` never invokes create or assignee follow-up for already-exists rows in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`

**Checkpoint**: User Story 4 should be fully functional — skip-only semantics preserved; CSV assignees visible but not applied to existing tasks

---

## Phase 7: User Story 5 - Document Assigned To on the full sample only (Priority: P3)

**Goal**: Public CSV guidance and in-app accepted-field cues list optional Assigned To; full example includes Assigned To with single and multi-person cells; minimal example stays Task Name only; no people-picker

**Independent Test**: Review `docs/csv-format.md`, `docs/troubleshooting.md`, Home accepted-fields copy, and minimal vs full examples; confirm Assigned To appears only on the full example and in the accepted-columns list

### Implementation for User Story 5

- [X] T060 [P] [US5] List `Assigned To` as optional, document work email or sign-in name matching, several people per cell, follow-up vs row error, and lookup-block rule; add Assigned To to the full example only (single address and multi-person cell) in `docs/csv-format.md`
- [X] T061 [P] [US5] Add troubleshooting bullets for blocked preview when destination members cannot be loaded and unmatched people appearing as follow-up in `docs/troubleshooting.md`
- [X] T062 [US5] Extend compact accepted-fields line to include optional **Assigned To** in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [X] T063 [P] [US5] Note that existing `GroupMember.Read.All` scope resolves Assigned To (no new Graph permission) in `docs-internal/entra-app-registration-setup.md` and `docs-internal/microsoft-graph-guidelines.md`
- [X] T064 [P] [US5] Update `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs` (or equivalent accepted-fields assertion) if it asserts the accepted-fields copy

**Checkpoint**: User Story 5 should be complete — operators can learn optional Assigned To from docs and in-app guidance

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Quality gates, architecture compliance, and quickstart validation

- [X] T065 [P] Add fingerprint test asserting assignee address change alters request fingerprint in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T066 Run `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests|ImportPlanningUseCaseTests|ImportExecutionUseCaseTests|GraphPlannerGatewayTests|ArchitectureComplianceTests|ImportPresenterTests"` and fix failures
- [X] T067 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` and fix formatting
- [X] T068 Validate manual scenarios in `specs/014-import-assignees/quickstart.md` section 5 (optional first-run)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational — delivers MVP preview and member lookup
- **User Story 2 (Phase 4)**: Depends on Foundational and US1 planning output (`ResolvedAssigneeIds`); gateway create wiring can start after Phase 2 in parallel with late US1 tasks
- **User Story 3 (Phase 5)**: Depends on US2 create path and `CreatedPlannerTask.AppliedAssigneeIds`
- **User Story 4 (Phase 6)**: Depends on US1 preview assignee display and US2/US3 create path — independently testable via execution doubles
- **User Story 5 (Phase 7)**: Can start after US1 parser behaviour is stable; docs reference final format list from contracts
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational — no dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational — needs US1 `ResolvedAssigneeIds` for end-to-end create, but Graph assignment mapping can be built and tested with stubs first
- **User Story 3 (P1)**: Depends on US2 create returning `AppliedAssigneeIds` — independently testable via execution doubles
- **User Story 4 (P2)**: Depends on US1 preview carrying assignees on skip rows and US2/US3 execution paths
- **User Story 5 (P3)**: Largely independent once accepted format list is final — can run in parallel with US2–US4 after US1 parser formats are settled

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Parser models before planning preview
- Gateway member lookup before planning match tests pass
- Planning preview before execution create
- Gateway adapter after `IPlannerGateway` signature change
- Docs after behaviour is stable

### Parallel Opportunities

- T002 can run parallel to T001
- T011, T012 can run in parallel after T009–T010
- All US1 parser tests (T015–T022) can run in parallel
- All US1 planning tests (T023–T028) can run in parallel after stub members exist
- T036 can run parallel to T035 once `ImportTaskPlanItem` has assignee fields
- US2 Graph tests (T042–T044) can run in parallel
- US3 tests (T048–T052) can run in parallel
- US4 tests (T056–T058) can run in parallel
- US5 doc tasks (T060, T061, T063, T064) can run in parallel
- T065 can run parallel to final test sweep

---

## Parallel Example: User Story 1

```bash
# Launch all parser tests together:
Task: "Add no Assigned To column success test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task: "Add comma and semicolon list split tests in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task: "Add duplicate ignore-case collapse test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"

# After parser implementation, planning and UI in sequence:
Task: "Implement GetPlanMembersAsync in GraphPlannerGateway.cs"
Task: "Match CSV values in ImportPlanningUseCase.cs"
Task: "Add Assigned to column in Home.razor"
```

---

## Parallel Example: User Story 2

```bash
# Gateway tests in parallel:
Task: "Graph gateway test for assignments JSON in GraphPlannerGatewayTests.cs"
Task: "Graph gateway test omitting assignments when empty in GraphPlannerGatewayTests.cs"

# Then wire execution and adapter:
Task: "Pass ResolvedAssigneeIds in ImportExecutionUseCase.cs"
Task: "Set plannerTask.assignments in GraphPlannerGateway.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (parser + member lookup + preview)
4. **STOP and VALIDATE**: Upload CSV with/without Assigned To; confirm preview assignees, follow-up, and lookup block
5. Deploy/demo preview safety before create wiring

### Incremental Delivery

1. Complete Setup + Foundational → contract and models ready
2. Add User Story 1 → Test preview independently → Demo (MVP!)
3. Add User Story 2 → Test create with assignees → Demo
4. Add User Story 3 → Test follow-up and partial assignment → Demo
5. Add User Story 4 → Test skip unchanged → Demo
6. Add User Story 5 → Docs and in-app cues → Demo
7. Polish → format, full test sweep, quickstart validation

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (parser + planning + preview UI)
   - Developer B: User Story 2 (execution + Graph create assignments) after T010
   - Developer C: User Story 5 (docs) once format list is agreed
3. User Story 3 lands after US2 checkpoint; User Story 4 after US1/US3
4. Polish phase shared

---

## Notes

- Canonical heading is exactly `Assigned To` (trim + case-insensitive match). Do not accept `Assignee`, `Email`, or alias variants in this increment ([#127](https://github.com/markheydon/import-to-planner/issues/127))
- Match only work email and sign-in name on destination members; guests already in the destination are assignable
- Individual unknown addresses are follow-up, not row errors; member lookup failure with any Assigned To address is a request-level block
- Skip rows show CSV assignees in preview but must not write to Planner or emit `AssignPersonToTask`
- Do not add people to destination membership; no people-picker in this increment
- Graph scopes remain unchanged (`User.Read`, `Group.Read.All`, `GroupMember.Read.All`, `Tasks.ReadWrite`)
- After code changes, run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` before merge
- If AppHost is running after Web changes, rebuild the web resource per AGENTS.md
