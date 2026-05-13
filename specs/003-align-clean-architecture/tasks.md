# Tasks: Clean Architecture Alignment

**Input**: Design documents from `/specs/003-align-clean-architecture/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/import-use-case-contracts.md`  
**Tests**: Included and required because this feature changes behaviour, boundaries, and runtime-mode-sensitive planner flows.  
**Agent delegation**: All coding, architecture, and test implementation tasks MUST be delegated to the C# Expert agent per `AGENTS.md`.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3]**: User story label from `spec.md`; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Establish package ownership, compliance evidence, and implementation guardrails before refactoring code.

- [X] T001 Move `CsvHelper` package ownership from Application to Infrastructure in `Directory.Packages.props`
- [X] T002 [P] Remove adapter-level CSV package usage from `src/ImportToPlanner.Application/ImportToPlanner.Application.csproj`
- [X] T003 [P] Add adapter-level CSV package usage to `src/ImportToPlanner.Infrastructure.Graph/ImportToPlanner.Infrastructure.Graph.csproj`
- [X] T004 Define architecture evidence commands and runtime-mode verification notes in `specs/003-align-clean-architecture/quickstart.md`
- [X] T005 Capture repository policy checkpoints and implementation delegation notes in `specs/003-align-clean-architecture/research.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the shared contracts, registrations, and verification scaffolding that every user story depends on.

**⚠️ CRITICAL**: Complete all T006-T010 before any user story work starts.

- [X] T006 Replace the combined orchestrator boundary with planning and execution abstractions in `src/ImportToPlanner.Application/Abstractions/IImportPlannerOrchestrator.cs`
- [X] T007 [P] Introduce neutral request, response, validation, and failure models in `src/ImportToPlanner.Application/Models/ImportRequest.cs`
- [X] T008 [P] Register the new use cases, presenters, and workflow collaborators in `src/ImportToPlanner.Web/Program.cs`
- [X] T009 [P] Add architecture compliance regression coverage for forbidden inner-layer references in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`
- [X] T010 [P] Parameterise web-test runtime-mode setup for planner parity checks in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/HomePageTestContext.cs`

**Checkpoint**: Shared seams, dependency wiring, and compliance checks exist so story work can proceed without reopening the architecture baseline.

---

## Phase 3: User Story 1 - Restore Inner-Layer Purity (Priority: P1) 🎯 MVP

**Goal**: Keep Domain and Application free of provider, file-format, and user-facing presentation concerns while preserving the current import workflow semantics.

**Independent Test**: Run focused parser, gateway, and architecture-compliance tests to verify that CSV implementation, failure translation, and domain plan models no longer leak outer-layer concerns while producing equivalent business outcomes.

### Tests for User Story 1

- [X] T011 [P] [US1] Move CSV parsing regression coverage to the infrastructure adapter in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T012 [P] [US1] Add neutral planner failure-mapping regression coverage in `tests/ImportToPlanner.Tests/GraphPlannerGatewayTests.cs`
- [X] T013 [US1] Add domain-plan purity coverage for removed provider metadata in `tests/ImportToPlanner.Tests/PlannerPlanTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Create the Infrastructure CSV parser adapter in `src/ImportToPlanner.Infrastructure.Graph/CsvImportParser.cs`
- [X] T015 [US1] Trim the core CSV parsing contract to repository-owned concepts in `src/ImportToPlanner.Application/Abstractions/ICsvImportParser.cs`
- [X] T016 [US1] Retire the Application-owned CSV implementation in `src/ImportToPlanner.Application/Services/CsvImportParser.cs`
- [X] T017 [US1] Replace Graph-branded exception and message mapping with neutral failure taxonomy in `src/ImportToPlanner.Application/Exceptions/PlannerGraphExceptions.cs`
- [X] T018 [P] [US1] Remove provider metadata residue from the domain plan model in `src/ImportToPlanner.Domain/PlannerPlan.cs`
- [X] T019 [P] [US1] Map Graph-specific planner metadata to neutral contracts at the adapter boundary in `src/ImportToPlanner.Infrastructure.Graph/GraphPlannerGateway.cs`
- [X] T020 [P] [US1] Keep in-memory planner behaviour aligned with the neutral contracts in `src/ImportToPlanner.Infrastructure.Graph/InMemoryPlannerGateway.cs`

**Checkpoint**: Domain and Application use only repository-owned concepts, and both planner runtime modes still translate adapter details at the edge.

---

## Phase 4: User Story 2 - Make Use-Case Boundaries Explicit (Priority: P2)

**Goal**: Split planning and execution into explicit use cases with clear request, response, and output-boundary seams that presenters can consume without changing application policy code.

**Independent Test**: Invoke planning and execution through their contracts, assert neutral structured outcomes in Application tests, and verify Web presenters own all user-facing wording.

### Tests for User Story 2

- [X] T021 [P] [US2] Replace orchestrator coverage with planning use-case tests in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T022 [P] [US2] Add execution use-case coverage for structured neutral outcomes in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T023 [US2] Add presenter-boundary regression coverage for UI wording ownership in `tests/ImportToPlanner.Web.Tests/ImportPresenterTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Retire the combined orchestrator implementation and migrate callers from `src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs`
- [X] T025 [P] [US2] Introduce planning request and preview result contracts in `src/ImportToPlanner.Application/Models/ImportPlanPreview.cs`
- [X] T026 [P] [US2] Introduce execution request and outcome contracts in `src/ImportToPlanner.Application/Models/ImportExecutionResult.cs`
- [X] T027 [US2] Implement the planning use case boundary in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T028 [US2] Implement the execution use case boundary in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs`
- [X] T029 [P] [US2] Add the planning presenter and preview view models in `src/ImportToPlanner.Web/Presenters/ImportPlanningPresenter.cs`
- [X] T030 [P] [US2] Add the execution presenter and report view models in `src/ImportToPlanner.Web/Presenters/ImportExecutionPresenter.cs`

**Checkpoint**: Planning and execution are explicit application seams, and presenters own wording without pulling UI text back into use-case implementations.

---

## Phase 5: User Story 3 - Reduce UI Workflow Coupling (Priority: P3)

**Goal**: Keep the main page focused on composition and binding by moving stepped workflow coordination into dedicated Web collaborators.

**Independent Test**: Exercise the stepped workflow in bUnit, verify validation, preview, confirmation, stale-preview invalidation, and execution still work, and confirm the page delegates coordination responsibilities outside the Razor component.

### Tests for User Story 3

- [X] T031 [P] [US3] Refactor stepped-workflow regression coverage around dedicated coordinator state in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T032 [US3] Add smoke coverage that `Home.razor` remains a composition surface after delegation in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`

### Implementation for User Story 3

- [X] T033 [P] [US3] Create workflow coordination state for validation, preview, confirmation, and execution in `src/ImportToPlanner.Web/Workflows/WorkflowCoordinationState.cs`
- [X] T034 [P] [US3] Add an import workflow coordinator that drives page actions in `src/ImportToPlanner.Web/Workflows/ImportWorkflowCoordinator.cs`
- [X] T035 [US3] Refactor page event handlers and bindings to delegate workflow coordination in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T036 [US3] Update execution report rendering to consume presenter-owned output models in `src/ImportToPlanner.Web/Components/Pages/HomeExecutionReport.razor`

**Checkpoint**: The stepped import workflow still behaves the same, but page-level coordination logic is owned by Web collaborators rather than the Razor page.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Capture verification evidence, close the audit loop, and record final architecture compliance outcomes.

- [X] T037 [P] Record forbidden-reference checks, runtime-mode parity steps, and manual workflow verification evidence in `specs/003-align-clean-architecture/quickstart.md`
- [X] T038 [P] Run focused Application and Web test suites plus solution verification and record outcomes in `specs/003-align-clean-architecture/quickstart.md`
- [X] T039 [P] Update remediation evidence and remaining follow-up notes in `docs-internal/clean-architecture-strict-adherence-audit.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the MVP architecture correction
- **Phase 4 (US2)**: Depends on US1 because the explicit use-case seams build on the neutral contracts and failure taxonomy introduced there
- **Phase 5 (US3)**: Depends on US2 because workflow delegation consumes the new use cases and presenter seams
- **Phase 6 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is independently valuable
- **US2 (P2)**: Depends on US1 neutral models and failure handling, then remains independently testable through the new use-case seams
- **US3 (P3)**: Depends on US2 presenter and use-case boundaries, then remains independently testable through page-workflow regression checks

### Within Each User Story

- Tests before implementation wherever practical
- Contract and model changes before service/use-case rewrites
- Presenter changes before Razor wiring that consumes them
- Story-specific regression verification before moving to the next priority

### Parallel Opportunities

- T002 and T003 can run in parallel after T001
- T007, T008, T009, and T010 can run in parallel after T006
- T011 and T012 can run in parallel before US1 implementation, while T018, T019, and T020 can run in parallel once T017 defines the neutral taxonomy
- T021 and T022 can run in parallel, and T025, T026, T029, and T030 can run in parallel once T024 is underway
- T031, T033, and T034 can run in parallel before the final `Home.razor` integration step
- T037, T038, and T039 can run in parallel once implementation is complete

---

## Parallel Execution Examples

### User Story 1

```text
T011 (CSV parser tests)  ║  T012 (gateway failure mapping tests)
then:
T014 (Infrastructure CSV parser)  →  T015 (core parser contract)  →  T016 (retire Application parser)
in parallel after T017:
T018 (domain plan cleanup)  ║  T019 (Graph adapter mapping)  ║  T020 (in-memory parity)
```

### User Story 2

```text
T021 (planning use-case tests)  ║  T022 (execution use-case tests)
in parallel after T024:
T025 (planning contracts)  ║  T026 (execution contracts)  ║  T029 (planning presenter)  ║  T030 (execution presenter)
then:
T027 (planning use case)  →  T028 (execution use case)
```

### User Story 3

```text
T031 (workflow regression tests)  ║  T033 (workflow state)  ║  T034 (workflow coordinator)
then:
T035 (Home.razor delegation)  →  T036 (execution report presenter output)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate architecture purity, parser relocation, and runtime-mode parity before expanding scope

### Incremental Delivery

1. Setup + Foundational establish the neutral seams and evidence scaffolding
2. US1 removes the most serious inner-layer leaks and becomes the MVP checkpoint
3. US2 formalises planning and execution boundaries plus presenter ownership
4. US3 reduces page coupling while preserving workflow behaviour
5. Polish records compliance and verification evidence

### Parallel Team Strategy

1. One engineer completes Setup + Foundational to stabilise the shared seams
2. After US1 starts, parser, domain, and gateway tasks can be split across separate engineers
3. After US2 contracts are in place, presenter work and use-case work can proceed in parallel
4. Web workflow delegation and final evidence capture can be split once the new seams are stable

---

## Summary

- **Total tasks**: 39
- **Setup + Foundational**: 10 (T001-T010)
- **US1**: 10 (T011-T020)
- **US2**: 10 (T021-T030)
- **US3**: 6 (T031-T036)
- **Polish**: 3 (T037-T039)
- **Parallel [P] tasks**: 23 of 39

Independent test criteria by story:

- **US1**: Focused parser, gateway, and architecture checks show no provider/file-format/UI leakage in inner layers while business outcomes stay equivalent
- **US2**: Planning and execution use cases emit neutral structured outcomes through explicit output boundaries, and presenters own the UI wording
- **US3**: The stepped workflow remains behaviourally consistent while `Home.razor` delegates coordination to dedicated Web collaborators

Suggested MVP scope: Complete Phase 1, Phase 2, and Phase 3 (US1) before starting explicit use-case or UI workflow refactors.

Format validation target: Every task uses the required checklist form with checkbox, task ID, optional `[P]`, required story label in user-story phases, and an exact file path.