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

- [X] T001 Create feature test fixture CSV files in tests/ImportToPlanner.Tests/Fixtures/
- [X] T002 Add test helper for CSV fixture loading in tests/ImportToPlanner.Tests/TestData/CsvFixtureLoader.cs
- [X] T003 [P] Add planner import scenario constants for tests in tests/ImportToPlanner.Tests/TestData/ImportScenarioConstants.cs
- [X] T004 Define feature-level quality gates and traceability notes in specs/001-import-planner-csv/quickstart.md
- [X] T005 Define preview performance measurement protocol (500 rows, p95 <10s) in specs/001-import-planner-csv/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core prerequisites that block all user-story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Add preview freshness metadata model fields in src/ImportToPlanner.Application/Models/ImportPlanPreview.cs
- [X] T007 Update preview/task model metadata for staleness and report status in src/ImportToPlanner.Application/Models/ImportTaskPlanItem.cs
- [X] T008 [P] Add execution outcome helper model for report consistency in src/ImportToPlanner.Application/Models/ImportExecutionResult.cs
- [X] T009 Implement shared stale-preview verification utility in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [X] T010 [P] Add foundational model regression tests in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [X] T011 Add foundational UI state reset helpers for preview/execution transitions in src/ImportToPlanner.Web/Components/Pages/Home.razor

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Validate And Preview Import Plan (Priority: P1) 🎯 MVP

**Goal**: Provide CSV validation and non-destructive dry-run preview with explicit row-level issues

**Independent Test**: Upload valid and invalid CSV files and verify validation output plus dry-run preview plan without writes

### Tests for User Story 1 (MANDATORY when behaviour changes)

- [X] T012 [P] [US1] Add parser validation tests for required fields and format rules in tests/ImportToPlanner.Tests/CsvImportParserTests.cs
- [X] T013 [P] [US1] Add parser tests for ignore-extra-columns behaviour in tests/ImportToPlanner.Tests/CsvImportParserTests.cs
- [X] T014 [P] [US1] Add orchestrator preview tests for name-only match and skip-on-match in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [X] T015 [P] [US1] Add regression test for duplicate task-name handling in preview in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs

### Implementation for User Story 1

- [X] T016 [US1] Implement/adjust CSV parsing and row-level validation behaviours in src/ImportToPlanner.Application/Services/CsvImportParser.cs
- [X] T017 [US1] Implement preview plan generation with name-only match semantics in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [X] T018 [US1] Update preview action reason strings (`already exists`) in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [X] T019 [US1] Implement preview UI flow updates (validation states, dry-run messaging, CTA states) in src/ImportToPlanner.Web/Components/Pages/Home.razor

**Checkpoint**: User Story 1 should be fully functional and independently testable

---

## Phase 4: User Story 2 - Confirm And Execute Import (Priority: P2)

**Goal**: Execute only approved preview actions with confirmation, stale-preview blocking, retry-once transient handling, and partial-success semantics

**Independent Test**: Execute a validated preview and verify confirmation gating, stale-preview blocking, retry-once handling, and per-row partial outcomes

### Tests for User Story 2 (MANDATORY when behaviour changes)

- [X] T020 [P] [US2] Add execution confirmation and validation-block tests in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [X] T021 [P] [US2] Add stale-preview blocking tests in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [X] T022 [P] [US2] Add partial-success continuation tests for per-row failures in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [X] T023 [P] [US2] Add transient Graph row-failure retry-once tests in tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs
- [X] T024 [P] [US2] Add runtime-mode parity tests (in-memory vs graph gateway behaviour) in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs

### Implementation for User Story 2

- [X] T025 [US2] Implement execution gating and stale-preview enforcement in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [X] T026 [US2] Implement row-failure continuation with aggregate partial result reporting in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [X] T027 [US2] Implement transient Graph retry-once policy for row operations in src/ImportToPlanner.Infrastructure.Graph/GraphPlannerGateway.cs
- [X] T028 [US2] Align in-memory gateway behaviour with clarified execution semantics in src/ImportToPlanner.Infrastructure.Graph/InMemoryPlannerGateway.cs
- [X] T029 [US2] Update execute button-state and stale-preview handling UX in src/ImportToPlanner.Web/Components/Pages/Home.razor

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Review Execution Reporting (Priority: P3)

**Goal**: Provide clear execution reporting including created, reused/skipped, failed, and manual-action outcomes

**Independent Test**: Complete an import with mixed outcomes and verify report sections and per-item statuses are accurate and user-safe

### Tests for User Story 3 (MANDATORY when behaviour changes)

- [X] T030 [P] [US3] Add execution report aggregation tests for created/reused/errors/manual actions in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [X] T031 [P] [US3] Add regression test ensuring user-facing errors exclude secret/tenant-sensitive values in tests/ImportToPlanner.Tests/ImportPlannerOrchestratorTests.cs
- [ ] T032 [P] [US3] Add UI rendering tests for execution report sections in tests/ImportToPlanner.Tests/HomePageWorkflowTests.cs

### Implementation for User Story 3

- [X] T033 [US3] Finalize execution report composition and manual-action formatting in src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs
- [X] T034 [US3] Update execution report UI grids and status messaging in src/ImportToPlanner.Web/Components/Pages/Home.razor
- [X] T035 [US3] Normalize user-safe error message mapping in src/ImportToPlanner.Application/Exceptions/PlannerGraphExceptions.cs

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story hardening, documentation, and verification

- [X] T036 [P] Update implementation notes and operator checkpoints in specs/001-import-planner-csv/quickstart.md
- [X] T037 Run full test suite for feature validation in tests/ImportToPlanner.Tests/
- [X] T038 Run solution and AppHost CI parity commands and capture outcomes in specs/001-import-planner-csv/quickstart.md
- [X] T039 [P] Update README behaviour notes to reflect clarified matching/retry/staleness semantics in README.md
- [X] T040 [P] Capture accessibility and responsive validation outcomes for validation/preview/execute/report flows in specs/001-import-planner-csv/quickstart.md
- [X] T041 Verify single-tenant scope boundary remains unchanged and document confirmation in specs/001-import-planner-csv/quickstart.md
- [X] T042 Run and record preview performance measurement (500 rows, p95 under 10s) in specs/001-import-planner-csv/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story Phases (3-5)**: Depend on Foundational completion
- **Polish (Phase 6)**: Depends on completion of selected user stories

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2; no dependency on other stories
- **User Story 2 (P2)**: Starts after Phase 2; depends on US1 preview artefacts for execution path
- **User Story 3 (P3)**: Starts after Phase 2; can progress in parallel with late US2 implementation once report contracts are stable

### Within Each User Story

- Tests first, verify failing behaviour, then implementation
- Model updates before orchestration/service behaviour
- Service behaviour before UI binding and messaging
- Story complete before phase checkpoint

## Parallel Opportunities

- Phase 1: T003 can run parallel to T001/T002
- Phase 2: T008 and T010 can run in parallel after T006/T007 model updates are merged
- US1: T012-T015 can run in parallel; T019 follows T017
- US2: T020-T024 can run in parallel; T027 and T028 can run in parallel
- US3: T030-T032 can run in parallel; T034 can start after T033 contract shape is fixed
- Polish: T036, T039, and T040 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Parallel test tasks:
T012, T013, T014, T015

# UI follow-up task (after core orchestrator changes):
T019
```

## Parallel Example: User Story 2

```bash
# Parallel test tasks:
T020, T021, T022, T023, T024

# Parallel gateway implementation tasks:
T027, T028
```

## Parallel Example: User Story 3

```bash
# Parallel reporting tests:
T030, T031, T032
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 and Phase 2
2. Deliver User Story 1 (T012-T019)
3. Validate preview-only non-destructive flow end-to-end

### Incremental Delivery

1. Add User Story 2 for controlled execution semantics
2. Add User Story 3 for complete reporting and manual-action visibility
3. Polish with documentation and CI/AppHost parity checks

### Parallel Team Strategy

1. Developer A: Parser + preview path (US1)
2. Developer B: Execution semantics + gateway retry/mode parity (US2)
3. Developer C: Reporting + UI messaging (US3)

---

## Notes

- [P] tasks denote independent files/no direct unfinished dependencies
- Story labels map each task to one user story for traceability
- All behaviour-changing tasks include corresponding automated tests
- Stale preview protection and runtime-mode parity are mandatory acceptance concerns
