---
description: "Task list for feature 002-ui-ux-redesign"
---

# Tasks: UI/UX Redesign — Stepped Import Workflow

**Input**: Design documents from `specs/002-ui-ux-redesign/`  
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ quickstart.md ✅  
**Tests**: Included — this feature changes UI behaviour and rendering.

**Agent delegation**: All implementation tasks MUST be delegated to the **C# Expert** agent per `AGENTS.md`.

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3]**: User story label from spec.md
- File paths are relative to the repository root

---

## Phase 1: Setup

**Purpose**: Confirm delegation, quality gates, and feature-level groundwork before any implementation begins.

- [ ] T001 Confirm C# Expert agent delegation applies to all implementation tasks in this feature per `AGENTS.md`; record any approved exceptions in `specs/002-ui-ux-redesign/plan.md`
- [ ] T002 Review constitution gates in `specs/002-ui-ux-redesign/plan.md` and confirm all are ✅ green before proceeding

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the new scoped CSS file and refactor the execution report component. Both are blocking prerequisites — US1 (step layout) depends on the CSS file, and US1 Step 5 depends on the refactored `HomeExecutionReport`.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T003 [P] Create `src/ImportToPlanner.Web/Components/Pages/Home.razor.css` with the full scoped CSS skeleton: `.step-list`, `.step-connector` connector line (using `--neutral-stroke-rest` design token), `.step--active` accent border (using `--accent-fill-rest`), and `.step--locked` opacity rule as specified in `specs/002-ui-ux-redesign/plan.md`
- [ ] T004 [P] Refactor `src/ImportToPlanner.Web/Components/Pages/HomeExecutionReport.razor` to use `FluentTabs` with three tabs: **Summary** (created + reused/skipped grids), **Manual Actions** (FluentDataGrid with count FluentBadge in header when non-zero), **Errors** (FluentDataGrid or success message with count FluentBadge in header when non-zero); preserve existing `[Parameter] ImportExecutionResult? ExecutionResult` interface unchanged

**Checkpoint**: Scoped CSS file exists; `HomeExecutionReport.razor` renders tabs — user story implementation can now begin.

---

## Phase 3: User Story 1 — Guided Step-by-Step Import (Priority: P1) 🎯 MVP

**Goal**: The page presents five clearly numbered vertical step cards. Each step becomes interactive only when its prerequisite inputs are satisfied. Completing all five steps produces a successful import.

**Independent Test**: Complete a full import from container selection through execution using the in-memory gateway and verify each step card activates in sequence per `specs/002-ui-ux-redesign/quickstart.md`.

### Tests for User Story 1 ⚠️

> **Write these tests FIRST — verify they FAIL before implementing T010–T015**

- [ ] T005 [P] [US1] Write failing bUnit test: page renders exactly five step containers with accessible `role="region"` and `aria-label` attributes matching "Step 1" through "Step 5" in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`
- [ ] T006 [P] [US1] Write failing bUnit test: on initial load, step 2 controls are disabled (no container selected) in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T007 [P] [US1] Write failing bUnit test: selecting a container activates step 2 and step 3 remains locked in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T008 [P] [US1] Write failing bUnit test: selecting a container and plan activates step 3 and step 4 remains locked in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T009 [US1] Write failing bUnit test: loading CSV content activates step 4 (validate button enabled) and step 5 remains locked in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 1

- [ ] T010 [US1] Replace the flat `FluentStack` layout in `src/ImportToPlanner.Web/Components/Pages/Home.razor` with a `.step-list` container holding five `.step-connector` wrapper divs, each containing a `FluentCard` with the two-zone header/content layout specified in `specs/002-ui-ux-redesign/plan.md`; preserve the entire `@code { }` block without any functional changes
- [ ] T011 [US1] Implement Step 1 (Select Container) card content in `src/ImportToPlanner.Web/Components/Pages/Home.razor`: `FluentSelect` for containers, Refresh Containers button, and `FluentMessageBar` for the no-containers-found edge case; step is always active on page load
- [ ] T012 [US1] Implement Step 2 (Select Plan) card content in `src/ImportToPlanner.Web/Components/Pages/Home.razor`: `FluentSelect` for plans (disabled when `selectedContainer is null`), Refresh Plans button, and `FluentMessageBar` for the no-plans-for-container edge case
- [ ] T013 [US1] Implement Step 3 (Upload CSV & Options) card content in `src/ImportToPlanner.Web/Components/Pages/Home.razor`: `FluentInputFile`, `FluentSwitch` for ignore-extra-columns, file name label; controls disabled when `selectedPlan is null`
- [ ] T014 [US1] Implement Step 4 (Validate & Preview) card content in `src/ImportToPlanner.Web/Components/Pages/Home.razor`: Validate And Preview button (using existing `canValidate` guard), parse-error `FluentDataGrid`, dry-run preview section with bucket-actions and task-actions grids; step 4 visible but locked when CSV not yet loaded
- [ ] T015 [US1] Implement Step 5 (Confirm & Import) card content in `src/ImportToPlanner.Web/Components/Pages/Home.razor`: Confirm And Execute button (using existing `canExecute` guard) and `<HomeExecutionReport ExecutionResult="@executionResult" />`; step 5 locked when `preview is null || isPreviewStale`

**Checkpoint**: Full import flow works end-to-end in the in-memory gateway. All US1 tests pass.

---

## Phase 4: User Story 2 — Clear Visual Status and Progress (Priority: P2)

**Goal**: A tester reviewing the page at any point in the flow can immediately identify which steps are complete (checkmark), active (accent border + chevron), or locked (muted + lock icon) without reading step content.

**Independent Test**: Partially complete the flow (select container only), take a screenshot or inspect DOM, and verify step 1 shows complete state, step 2 shows active state, steps 3–5 show locked state.

### Tests for User Story 2 ⚠️

> **Write these tests FIRST — verify they FAIL before implementing T019–T023**

- [ ] T016 [P] [US2] Write failing bUnit test: the currently active step card has CSS class `step--active` in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T017 [P] [US2] Write failing bUnit test: a completed step (container selected) shows `step--complete` class and renders a `CheckmarkCircle20Filled` icon in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T018 [P] [US2] Write failing bUnit test: a locked step has CSS class `step--locked` and its primary control has `disabled` attribute in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 2

- [ ] T019 [US2] Add private computed C# members to the `@code { }` block in `src/ImportToPlanner.Web/Components/Pages/Home.razor` for per-step state: `StepState GetStepState(int stepIndex)` returning an inline enum or string value derived from existing state variables (`selectedContainer`, `selectedPlan`, `csvContent`, `preview`, `isPreviewStale`) — no new state fields introduced
- [ ] T020 [US2] Apply conditional CSS class bindings (`step--active`, `step--complete`, `step--locked`) to each step card wrapper `<div>` in `src/ImportToPlanner.Web/Components/Pages/Home.razor`, and apply `Appearance.Accent` vs `Appearance.Neutral` to the step-number `FluentBadge` based on computed step state
- [ ] T021 [US2] Add the step status icon (`FluentIcon`) to each step header row in `src/ImportToPlanner.Web/Components/Pages/Home.razor`: `CheckmarkCircle20Filled` for complete, `ChevronRight20Filled` for active, `LockClosed20Regular` for locked
- [ ] T022 [US2] Render compact selection summary text in completed step headers in `src/ImportToPlanner.Web/Components/Pages/Home.razor` (e.g. step 1 complete: container display name and type; step 2 complete: plan title; step 3 complete: selected file name)
- [ ] T023 [US2] Implement CSS rules in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css` for `step--active` (accent left border using `--accent-fill-rest`), `step--complete` (default, no special opacity), and `step--locked` (opacity 0.6); confirm the connector line pseudo-element aligns with the badge centre

**Checkpoint**: Visual states are correct for all five steps at every point in the workflow. All US2 tests pass.

---

## Phase 5: User Story 3 — Stale Preview Warning Inline with the Execute Step (Priority: P3)

**Goal**: When selections change after a preview has been generated, a contextual warning appears within Step 4 (not as a floating message), and Step 5 locks until the user re-validates.

**Independent Test**: Generate a preview, change the container or plan, and verify (a) a stale warning appears inside the Step 4 card and (b) the Confirm And Execute button in Step 5 is disabled.

### Tests for User Story 3 ⚠️

> **Write these tests FIRST — verify they FAIL before implementing T026–T027**

- [ ] T024 [P] [US3] Write failing bUnit test: after generating a valid preview, changing the selected container causes a stale-preview `FluentMessageBar` to appear inside the Step 4 card body in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T025 [P] [US3] Write failing bUnit test: step 5 acquires `step--locked` class when `isPreviewStale` is true, and re-activates after re-validating in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 3

- [ ] T026 [US3] Move the stale-preview `FluentMessageBar` (currently a sibling of the action buttons) into the Step 4 card content body in `src/ImportToPlanner.Web/Components/Pages/Home.razor`; ensure it is only rendered when `isPreviewStale` is true and is contextually placed above the preview grids
- [ ] T027 [US3] Verify Step 5 locked-state derivation in `src/ImportToPlanner.Web/Components/Pages/Home.razor` correctly uses the existing `isPreviewStale` and `canExecute` booleans with no new state logic; add a guard-clause comment confirming the invariant

**Checkpoint**: Stale-preview warning is contextual and Step 5 locks/unlocks correctly. All US3 tests pass.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility, UX copy consistency, runtime mode verification, and final regression check.

- [ ] T028 [P] Add `role="region"` and `aria-label="Step N: [Step Title]"` attributes to each step card wrapper `<div>` in `src/ImportToPlanner.Web/Components/Pages/Home.razor` (required for US1 tests T005 and per UX consistency gate)
- [ ] T029 [P] Review all user-facing copy in `src/ImportToPlanner.Web/Components/Pages/Home.razor` and `src/ImportToPlanner.Web/Components/Pages/HomeExecutionReport.razor` for UK English consistency (e.g. "colour", "behaviour", "organisation") and alignment with existing app tone
- [ ] T030 [P] Verify the layout renders without horizontal scrolling at a 1024 px viewport using the browser devtools responsive view per `specs/002-ui-ux-redesign/quickstart.md`; adjust `.step-list` max-width and padding if needed in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`
- [ ] T031 Run the full test suite (`dotnet test`) and confirm all tests pass including the existing `HomePageSmokeTests.cs` and `HomePageWorkflowTests.cs` in `tests/ImportToPlanner.Web.Tests/`
- [ ] T032 Manually verify both runtime modes per `specs/002-ui-ux-redesign/quickstart.md`: in-memory gateway (no auth) and Graph gateway (with auth); confirm authentication redirect in `OnInitializedAsync` continues to function correctly and the stepped layout renders identically in both modes

---

## Dependencies

```
T001 → T002
T003 → T010, T023
T004 → T015
T005–T009 (write before T010–T015, verify failure first)
T010 → T011 → T012 → T013 → T014 → T015
T015 depends on T004 (HomeExecutionReport tabs)
T016–T018 (write before T019–T023, verify failure first)
T019 → T020 → T021 → T022
T023 depends on T003
T024–T025 (write before T026–T027, verify failure first)
T026 → T027
T028, T029, T030 can run in parallel after T015
T031 after all implementation and tests complete
T032 after T031
```

## Parallel Execution Opportunities

### Within Phase 2 (Foundational)
- T003 and T004 can run in parallel (different files)

### Within Phase 3 tests
- T005, T006, T007, T008 can be written in parallel (same file, different test methods)

### Within Phase 4 tests
- T016, T017, T018 can be written in parallel

### Within Phase 5 tests
- T024, T025 can be written in parallel

### Final Phase
- T028, T029, T030 can run in parallel after T015

## Implementation Strategy (MVP-first)

**Suggested MVP Scope**: Phases 1–3 (T001–T015)

After completing Phase 1–3, you have a fully functional stepped import flow with all five steps activating in sequence. This satisfies the primary user journey (US1) and is independently deployable and demonstrable.

Phases 4 and 5 add visual polish and contextual error handling on top of a working MVP.

## Summary

| Phase | Tasks | User Story | Parallelisable |
| ----- | ----- | ---------- | -------------- |
| Setup | T001–T002 | — | No |
| Foundational | T003–T004 | — | Yes (T003 ∥ T004) |
| US1 (MVP) | T005–T015 | US1 | T005–T009 partial |
| US2 | T016–T023 | US2 | T016–T018 partial |
| US3 | T024–T027 | US3 | T024–T025 partial |
| Polish | T028–T032 | — | T028–T030 parallel |
| **Total** | **32 tasks** | | |
