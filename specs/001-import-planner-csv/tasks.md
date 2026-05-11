# Tasks: CSV To Planner Import Workflow

**Input**: Design documents from `/specs/001-import-planner-csv/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Test tasks are REQUIRED for behaviour changes and bug fixes. Include unit, integration, and regression tasks as applicable to the scope.

**Organisation**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and shared scaffolding for feature delivery

- [ ] T001 Create feature CSV fixtures in tests/ImportToPlanner.Tests/Fixtures/
- [ ] T002 Add CSV fixture loader helper in tests/ImportToPlanner.Tests/TestData/CsvFixtureLoader.cs
- [ ] T003 [P] Add import scenario constants in tests/ImportToPlanner.Tests/TestData/ImportScenarioConstants.cs
- [ ] T004 Define feature quality gates and traceability checklist in specs/001-import-planner-csv/quickstart.md
- [ ] T005 Define preview performance protocol (including IMPORT_TO_PLANNER_RUN_PERF_TESTS) in specs/001-import-planner-csv/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core prerequisites that block all user-story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T006 Add preview freshness fields (request/planner fingerprints, timestamps, validation marker) in src/ImportToPlanner.Application/Models/ImportPlanPreview.cs
- [ ] T007 Add preview report metadata fields and invariants in src/ImportToPlanner.Application/Models/ImportTaskPlanItem.cs
- [ ] T008 [P] Add execution outcome summary model fields and helpers in src/ImportToPlanner.Application/Models/ImportExecutionResult.cs
- [ ] T009 Implement shared stale-preview verification utility in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [ ] T010 [P] Implement startup auth/session mode wiring and certificate compatibility fallback in src/ImportToPlanner.Web/Program.cs
- [ ] T011 Add foundational regression tests for preview metadata and stale-preview guards in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T012 Add foundational UI state reset helpers for preview/execution transitions in src/ImportToPlanner.Web/Components/Pages/Home.razor

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Validate And Preview Import Plan (Priority: P1) 🎯 MVP

**Goal**: Provide CSV validation and non-destructive dry-run preview with explicit row-level issues

**Independent Test**: Upload valid and invalid CSV files and verify validation output plus dry-run preview plan without writes

### Tests for User Story 1 (MANDATORY when behaviour changes)

- [ ] T013 [P] [US1] Add parser validation tests for required fields and file-level header handling in tests/ImportToPlanner.Tests/CsvImportParserTests.cs
- [ ] T014 [P] [US1] Add parser tests for ignore-extra-columns UI-default behaviour in tests/ImportToPlanner.Tests/CsvImportParserTests.cs
- [ ] T015 [P] [US1] Add parser tests for priority token mapping (Urgent/Important/Medium/Low) in tests/ImportToPlanner.Tests/CsvImportParserTests.cs
- [ ] T016 [P] [US1] Add preview tests for name-only match, duplicate-in-CSV skip, and default bucket resolution in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs

### Implementation for User Story 1

- [ ] T017 [US1] Implement CSV parsing and validation behaviour (including priority mapping) in src/ImportToPlanner.Application/Services/CsvImportParser.cs
- [ ] T018 [US1] Implement preview planning rules (name-only matching, duplicate skip, General bucket fallback) in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [ ] T019 [US1] Implement upload file-size limit and parser option defaults in src/ImportToPlanner.Web/Components/Pages/Home.razor
- [ ] T020 [US1] Implement preview UI states and dry-run messaging updates in src/ImportToPlanner.Web/Components/Pages/Home.razor

**Checkpoint**: User Story 1 should be fully functional and independently testable

---

## Phase 4: User Story 2 - Confirm And Execute Import (Priority: P2)

**Goal**: Execute only approved preview actions with confirmation, stale-preview blocking, retry-once transient handling, and partial-success semantics

**Independent Test**: Execute a validated preview and verify confirmation gating, stale-preview blocking, retry-once handling, and per-row partial outcomes

### Tests for User Story 2 (MANDATORY when behaviour changes)

- [ ] T021 [P] [US2] Add execution confirmation and unresolved-validation block tests in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T022 [P] [US2] Add stale-preview fingerprint mismatch block tests in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T023 [P] [US2] Add partial-success continuation tests for per-row failures in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T024 [P] [US2] Add transient Graph row-failure retry-once tests in tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs
- [ ] T025 [P] [US2] Add runtime-mode parity tests (in-memory vs Graph) for execution outcomes in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs

### Implementation for User Story 2

- [ ] T026 [US2] Implement execution gating and stale-preview enforcement in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [ ] T027 [US2] Implement row-failure continuation and aggregate result reporting in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [ ] T028 [US2] Implement Graph transient retry-once row operation policy in src/ImportToPlanner.Infrastructure.Graph/GraphPlannerGateway.cs
- [ ] T029 [US2] Align in-memory gateway semantics with execution rules in src/ImportToPlanner.Infrastructure.Graph/InMemoryPlannerGateway.cs
- [ ] T030 [US2] Implement execute-state UX and stale-preview blocking messaging in src/ImportToPlanner.Web/Components/Pages/Home.razor

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Review Execution Reporting (Priority: P3)

**Goal**: Provide clear execution reporting including created, reused/skipped, failed, and manual-action outcomes

**Independent Test**: Complete an import with mixed outcomes and verify report sections and per-item statuses are accurate and user-safe

### Tests for User Story 3 (MANDATORY when behaviour changes)

- [ ] T031 [P] [US3] Add execution report aggregation tests for created/reused/errors/manual actions and outcome summary in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T032 [P] [US3] Add regression tests ensuring user-facing errors exclude secrets/tenant-sensitive values in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T033 [US3] Create dedicated Blazor UI test project in tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj and add it to ImportToPlanner.slnx
- [ ] T034 [P] [US3] Configure UI test dependencies (`bunit`, `xunit`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`) and reference src/ImportToPlanner.Web/ImportToPlanner.Web.csproj in tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj
- [ ] T035 [US3] Add shared Blazor test bootstrap utilities for Home page rendering in tests/ImportToPlanner.Web.Tests/TestInfrastructure/
- [ ] T036 [US3] Add Home page smoke render test in tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs
- [ ] T037 [US3] Add execution-report section rendering workflow tests in tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs

### Implementation for User Story 3

- [ ] T038 [US3] Implement execution report composition and manual-action formatting (EnsureGoalExists/LinkTaskToGoal) in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [ ] T039 [US3] Implement execution report UI grids and status messaging in src/ImportToPlanner.Web/Components/Pages/Home.razor
- [ ] T040 [US3] Implement user-safe Graph error normalization in src/ImportToPlanner.Application/Exceptions/PlannerGraphExceptions.cs

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story hardening, documentation, and verification

- [ ] T041 [P] Update operator implementation notes and checkpoints in specs/001-import-planner-csv/quickstart.md
- [ ] T042 [P] Update README runtime behaviour notes (matching/retry/staleness/mode semantics) in README.md
- [ ] T043 [P] Capture accessibility and responsive validation outcomes for validation/preview/execute/report flows in specs/001-import-planner-csv/quickstart.md
- [ ] T044 Verify single-tenant scope boundary and document confirmation in specs/001-import-planner-csv/quickstart.md
- [ ] T045 Run and record preview performance measurement (500 rows, p95 under 10s) in specs/001-import-planner-csv/quickstart.md
- [ ] T046 Run core test suite in tests/ImportToPlanner.Tests/
- [ ] T047 Run UI test suite in tests/ImportToPlanner.Web.Tests/
- [ ] T048 Run solution and AppHost parity commands and record outcomes in specs/001-import-planner-csv/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story Phases (3-5)**: Depend on Foundational completion
- **Polish (Phase 6)**: Depends on completion of user stories

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2; no dependency on other stories
- **User Story 2 (P2)**: Starts after Phase 2; depends on US1 preview artefacts and validation flow
- **User Story 3 (P3)**: Starts after Phase 2; depends on US2 execution/report contracts for stable UI report binding
- **US3 UI test prerequisites**: T037 depends on T033, T034, T035, and T036

### Within Each User Story

- Tests first, verify failing behaviour, then implementation
- Model/contract updates before orchestration/service behaviour
- Service behaviour before UI binding and messaging
- Story complete before phase checkpoint

## Parallel Opportunities

- Setup: T003 can run in parallel with T001 and T002
- Foundational: T008 and T010 can run in parallel after model shape is agreed
- US1: T013-T016 can run in parallel; T019 and T020 follow parser/orchestrator updates
- US2: T021-T025 can run in parallel; T028 and T029 can run in parallel
- US3: T031 and T032 can run in parallel; T033 and T034 can run in parallel; T038 and T039 can proceed once T031-T037 stabilise report contracts
- Polish: T041-T043 can run in parallel; T046 and T047 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Parallel test tasks:
T013, T014, T015, T016

# Follow-up implementation tasks:
T017, T018, T019, T020
```

## Parallel Example: User Story 2

```bash
# Parallel test tasks:
T021, T022, T023, T024, T025

# Parallel gateway tasks:
T028, T029
```

## Parallel Example: User Story 3

```bash
# Parallel report tests:
T031, T032

# Parallel UI test host foundation:
T033, T034

# Then complete prerequisites and workflow test:
T035 -> T036 -> T037
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 and Phase 2
2. Deliver User Story 1 (T013-T020)
3. Validate preview-only non-destructive flow end-to-end

### Incremental Delivery

1. Add User Story 2 for controlled execution semantics
2. Add User Story 3 for complete reporting and manual-action visibility
3. Complete new UI test host and execution-report workflow rendering tests
4. Polish with documentation, performance evidence, and CI/AppHost parity checks

### Parallel Team Strategy

1. Developer A: Parser and preview path (US1)
2. Developer B: Execution semantics, retry policy, runtime parity (US2)
3. Developer C: Reporting and UI plus UI-test-host/test workflows (US3)

---

## Notes

- [P] tasks denote independent files with no direct unfinished dependency
- [Story] labels map each task to one user story for traceability
- All behaviour-changing tasks include corresponding automated tests
- Runtime-mode parity and stale-preview protection are mandatory acceptance concerns
- UI rendering workflow coverage for execution reports is implemented as a first-class task (T037), not deferred
