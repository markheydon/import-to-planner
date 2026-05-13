# Tasks: UI/UX Redesign — Stepped Import Workflow (MudBlazor)

**Input**: Design documents from `/specs/002-ui-ux-redesign/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`  
**Tests**: Included and required (behaviour changes and regression coverage).  
**Agent delegation**: All coding, architecture, and test implementation tasks MUST be delegated to the C# Expert agent per `AGENTS.md`. The `mudblazor` skill is the primary UI reference.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3/US4]**: User story label from spec.md; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Confirm feature governance, execution references, and quality gates before code changes.

- [X] T001 Update feature-level quality gates for architecture, testing, UX consistency, performance, runtime-mode compatibility, and CI/AppHost parity in `specs/002-ui-ux-redesign/plan.md`
- [X] T002 Confirm C# Expert delegation and MudBlazor skill usage for implementation in `AGENTS.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Package swap and global registrations that every Razor file depends on. No user-story implementation can begin until this phase is complete.

**⚠️ CRITICAL**: Complete all T003–T009 before any Phase 3+ work starts.

- [X] T003 Update MudBlazor package versions and remove Fluent UI package versions in `Directory.Packages.props`
- [X] T004 [P] Replace Fluent UI package references with MudBlazor in `src/ImportToPlanner.Web/ImportToPlanner.Web.csproj`
- [X] T005 [P] Replace `AddFluentUIComponents()` with `AddMudServices()` in `src/ImportToPlanner.Web/Program.cs`
- [X] T006 [P] Replace Fluent UI imports with MudBlazor imports in `src/ImportToPlanner.Web/Components/_Imports.razor`
- [X] T007 [P] Add the MudBlazor stylesheet reference to `src/ImportToPlanner.Web/Components/App.razor`
- [X] T008 Rewrite the application shell using `MudLayout`, `MudAppBar`, `MudMainContent`, and required providers in `src/ImportToPlanner.Web/Components/Layout/MainLayout.razor`
- [X] T009 Remove superseded shell and import-grid CSS while retaining Blazor error overlay styles in `src/ImportToPlanner.Web/wwwroot/app.css`

**Checkpoint**: Solution builds, providers are registered, and the UI shell baseline is ready for feature work.

---

## Phase 3: User Story 1 — Guided Step-by-Step Import (Priority: P1) 🎯 MVP

**Goal**: The user can complete a full end-to-end import through five vertically stacked step cards that unlock in sequence.

**Independent Test**: Start the app in in-memory mode, complete the five-step flow using a representative CSV fixture, and verify that Step 5 shows the execution report inline without navigation.

### Tests for User Story 1 ⚠️

- [X] T010 [P] [US1] Update MudBlazor shell and smoke assertions in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`
- [X] T011 [US1] Replace workflow selectors and add step-sequence assertions for Step 1 through Step 5 in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T012 [US1] Add regression coverage for null placeholder selector state on initial render in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T013 [US1] Add regression coverage for explicit first-option selection unlocking the next step in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Rewrite the stepped page scaffold with five `MudPaper` cards and derived step-state helpers in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T015 [US1] Implement Step 1 container selection, refresh action, and empty-state alert in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T016 [US1] Implement Step 2 plan selection, refresh action, no-plan warning, and explicit state reset logic in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T017 [US1] Implement Step 3 CSV upload, file-name display, and ignore-extra-columns toggle in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T018 [US1] Implement Step 4 validate-and-preview actions, inline validation errors, preview tables, and busy-state rendering in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T019 [US1] Refactor Step 5 execution reporting to `MudTabs` with summary, manual actions, and errors in `src/ImportToPlanner.Web/Components/Pages/HomeExecutionReport.razor`
- [X] T020 [US1] Wire Step 5 confirm-and-execute flow and inline success reporting in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: The complete five-step import flow is functional and independently testable in in-memory mode.

---

## Phase 4: User Story 2 — Clear Visual Status and Progress (Priority: P2)

**Goal**: Users can distinguish completed, active, and locked steps at a glance without reading the full body content.

**Independent Test**: Complete Steps 1 and 2 only, then verify completed indicators, active-step emphasis, and locked-step styling remain visually distinct.

### Tests for User Story 2 ⚠️

- [X] T021 [US2] Add coverage for `MudPaper` elevation values across locked, active, and complete states in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T022 [US2] Add coverage for avatar colour, checkmark rendering, and step-number rendering in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 2

- [X] T023 [P] [US2] Add the active-step accent rule in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`
- [X] T024 [US2] Finalize active, locked, and complete visual state helpers and summary text rendering in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: Visual progress states are verifiable by automated tests and manual inspection.

---

## Phase 5: User Story 4 — Searchable Selectors for Large Tenant Datasets (Priority: P2)

**Goal**: Users can search container and plan lists and select results with keyboard or pointer input.

**Independent Test**: Load a large fixture list, type partial search text into both selectors, and verify filtering plus keyboard-driven selection unlock the next step.

### Tests for User Story 4 ⚠️

- [X] T025 [US4] Add coverage for container search filtering with a large fixture list in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T026 [US4] Add coverage for keyboard or `ValueChanged`-driven plan selection unlocking Step 3 in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 4

- [X] T027 [US4] Implement `SearchContainers` and `SearchPlans` for case-insensitive filtering in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: Search filtering and explicit selection behaviour are functional for large lists.

---

## Phase 6: User Story 3 — Stale Preview Warning (Priority: P3)

**Goal**: If the user changes selections after generating a preview, the execute step locks again and a contextual stale-preview warning appears inline in Step 4.

**Independent Test**: Generate a preview, change the selected plan or container, verify the stale-preview warning appears in Step 4 and the execute action disables, then re-validate to clear the warning.

### Tests for User Story 3 ⚠️

- [X] T028 [US3] Add coverage for stale-preview warning visibility and disabled execute action after selection changes in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T029 [US3] Add coverage for re-validation clearing the stale-preview warning and re-enabling execution in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 3

- [X] T030 [US3] Render the stale-preview warning inline in Step 4 and keep Step 5 execution guarded by preview freshness in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: Preview freshness safeguards are visible, contextual, and verified.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T031 [P] Verify `MudProgressLinear` appears for `isBusy` operations in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T032 [P] Audit UK English copy, alert text, button labels, and placeholders in `src/ImportToPlanner.Web/Components/Layout/MainLayout.razor`
- [X] T033 [P] Validate the end-to-end five-step workflow can be completed within 5 minutes using an in-memory run and a representative CSV fixture from `specs/002-ui-ux-redesign/quickstart.md`
- [X] T034 [P] Capture measurable UI responsiveness evidence for step unlock and preview-state rendering in `specs/002-ui-ux-redesign/quickstart.md`
- [X] T035 [P] Validate primary-flow mobile/touch usability and responsive behaviour in `specs/002-ui-ux-redesign/quickstart.md`
- [X] T036 [P] Run solution build and all tests, then record runtime-mode validation outcomes for both in-memory and Graph paths in `specs/002-ui-ux-redesign/quickstart.md`
- [X] T037 [P] Update Dependabot NuGet group definitions by replacing Fluent UI package grouping with MudBlazor package grouping in `.github/dependabot.yml`
- [X] T038 [P] Capture SC-005 architecture-boundary verification evidence for PR notes and mirror a concise evidence summary in `specs/002-ui-ux-redesign/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the MVP
- **Phase 4 (US2)**: Depends on T014 from US1 because visual-state helpers live in `Home.razor`
- **Phase 5 (US4)**: Depends on T015–T016 because searchable selectors extend the Step 1 and Step 2 controls in `Home.razor`
- **Phase 6 (US3)**: Depends on T018 and T020 because stale-preview protection builds on preview and execute flow state
- **Phase 7 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is independently valuable
- **US2 (P2)**: Builds on the US1 step scaffold but remains independently testable once visual-state work is applied
- **US4 (P2)**: Builds on the US1 selector controls but remains independently testable once filtering is applied
- **US3 (P3)**: Builds on the US1 preview and execute flow but remains independently testable once stale-preview protection is applied

### Within Each User Story

- Tests before implementation wherever practical
- Step scaffold before step-specific UI work
- Preview flow before stale-preview protections
- Story-specific verification before cross-cutting polish

### Parallel Opportunities

- T004, T005, T006, and T007 can run in parallel after T003
- T010 can run in parallel with T014 because they touch different files
- T023 can run in parallel with T021–T022 because CSS and test work touch different files
- T031–T038 can run in parallel once feature implementation is complete

---

## Parallel Execution Examples

### Phase 2 (after T003)

```text
T004 (Web csproj)  ║  T005 (Program.cs)  ║  T006 (_Imports.razor)  ║  T007 (App.razor)
then:
T008 (MainLayout.razor)  →  T009 (app.css)
```

### Phase 3 (after T014)

```text
T010 (smoke tests)  ║  T015 (Step 1)  →  T016 (Step 2)  →  T017 (Step 3)
then:
T018 (Step 4)  →  T019 (execution-report tabs)  →  T020 (Step 5 wiring)
```

### Phase 7 (after all story phases)

```text
T031 (busy-state verification)  ║  T032 (copy audit)  ║  T033 (5-minute walkthrough)
T034 (UI responsiveness evidence)  ║  T035 (mobile/touch validation)  ║  T036 (build, tests, runtime-mode checks)
T037 (Dependabot group update)
T038 (architecture-boundary evidence)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate the complete in-memory five-step workflow

### Incremental Delivery

1. Setup + Foundational establish the MudBlazor baseline
2. US1 delivers the working stepped workflow
3. US2 improves visual progress clarity
4. US4 improves large-list search and selection
5. US3 adds stale-preview protection
6. Polish captures validation evidence and final quality checks

### Parallel Team Strategy

1. One engineer completes Setup + Foundational
2. After US1 scaffold work lands, another engineer can take US2 visual-state work while a third takes test updates in separate files
3. Polish verification tasks can be split across team members once implementation stabilises

---

## Summary

- **Total tasks**: 38
- **Setup + Foundational**: 9 (T001–T009)
- **US1**: 11 (T010–T020)
- **US2**: 4 (T021–T024)
- **US4**: 3 (T025–T027)
- **US3**: 3 (T028–T030)
- **Polish**: 8 (T031–T038)
- **Parallel [P] tasks**: 14 of 38

All tasks follow required checklist format: checkbox ✓, Task ID ✓, [P] marker only where safe ✓, [US?] label for user-story-phase tasks ✓, file path in description ✓.
