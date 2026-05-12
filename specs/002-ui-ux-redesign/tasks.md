# Tasks: UI/UX Redesign — Stepped Import Workflow

**Input**: Design documents from `specs/002-ui-ux-redesign/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md  
**Tests**: Included and required (behaviour changes and regression coverage).  
**Agent delegation**: All coding, architecture, and test implementation tasks MUST be delegated to the C# Expert agent per `AGENTS.md`.

## Format: `[ID] [P?] [Story] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3/US4]**: User story label from spec.md

## Phase 1: Setup

**Purpose**: Confirm feature governance and implementation constraints before code changes.

- [ ] T001 Confirm C# Expert delegation and record any approved exceptions in `specs/002-ui-ux-redesign/plan.md`
- [ ] T002 Confirm constitution gates and Fluent-first/CSS-last-resort guardrails are reflected in `specs/002-ui-ux-redesign/plan.md`
- [ ] T003 Confirm Fluent UI MCP server is configured in `.vscode/mcp.json` and available for implementation guidance in this workspace

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared implementation foundations required by all user stories.

**Critical**: No user-story implementation starts before this phase is complete.

- [ ] T004 [P] Create scoped stylesheet `src/ImportToPlanner.Web/Components/Pages/Home.razor.css` with step shell classes (`.step-list`, `.step-connector`, `.step--active`, `.step--complete`, `.step--locked`) using Fluent design tokens
- [ ] T005 [P] Refactor `src/ImportToPlanner.Web/Components/Pages/HomeExecutionReport.razor` to `FluentTabs` layout while keeping the existing `ExecutionResult` parameter contract unchanged
- [ ] T006 Define and document selector strategy for v4 implementation in `specs/002-ui-ux-redesign/research.md` (unselected placeholders, explicit selection progression, searchable component choice)

**Checkpoint**: Shared step styling, tabbed report component, and selector strategy are in place.

---

## Phase 3: User Story 1 — Guided Step-by-Step Import (Priority: P1) 🎯 MVP

**Goal**: Deliver five vertical workflow steps with progressive unlock behaviour and no regression to existing import functionality.

**Independent Test**: Complete full import flow from Step 1 to Step 5 in in-memory mode and verify sequential unlock behaviour.

### Tests for User Story 1 (write first, verify failing)

- [ ] T007 [P] [US1] Add failing render test for five step regions with role/aria labels in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`
- [ ] T008 [P] [US1] Add failing workflow test for initial lock state (only Step 1 active) in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T009 [P] [US1] Add failing workflow test for step progression container→plan→file→preview→execute in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 1

- [ ] T010 [US1] Replace flat page structure with five vertical step cards in `src/ImportToPlanner.Web/Components/Pages/Home.razor` while preserving existing code-behind behaviour
- [ ] T011 [US1] Implement Step 1 (container selection + refresh + empty-state message) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T012 [US1] Implement Step 2 (plan selection + refresh + empty-state message) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T013 [US1] Implement Step 3 (CSV upload + options + selected filename) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T014 [US1] Implement Step 4 (validate/preview, parse errors, dry-run grids) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T015 [US1] Implement Step 5 (execute action + `HomeExecutionReport`) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: MVP flow is fully usable and independently testable.

---

## Phase 4: User Story 2 — Clear Visual Status And Progress (Priority: P2)

**Goal**: Make active, complete, and locked step status obvious at a glance.

**Independent Test**: Partial workflow completion shows complete/active/locked states accurately without reading step content.

### Tests for User Story 2 (write first, verify failing)

- [ ] T016 [P] [US2] Add failing test for `step--active` class and active status icon in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T017 [P] [US2] Add failing test for `step--complete` class and complete status icon in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T018 [P] [US2] Add failing test for `step--locked` class and disabled controls in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 2

- [ ] T019 [US2] Add computed per-step state helpers in `src/ImportToPlanner.Web/Components/Pages/Home.razor` derived from existing state variables only
- [ ] T020 [US2] Bind step CSS classes and badge appearance based on computed state in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T021 [US2] Add status icons (active/complete/locked) in step headers in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T022 [US2] Add compact summaries for completed steps in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T023 [US2] Finalise status visuals in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`

**Checkpoint**: Visual progression cues are correct and independently validated.

---

## Phase 5: User Story 4 — Searchable Selectors For Large Tenant Datasets (Priority: P2)

**Goal**: Enable quick, searchable selection for containers and plans in large Microsoft 365 datasets.

**Independent Test**: With large fixture lists, users can search and select target container/plan without full-list scrolling.

### Tests for User Story 4 (write first, verify failing)

- [ ] T024 [P] [US4] Add failing test for unselected placeholder initial state in Step 1 and Step 2 selectors in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T025 [P] [US4] Add failing regression test that explicit first-option selection unlocks next step in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T026 [P] [US4] Add failing test for searchable filtering behaviour in container and plan selectors in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T027 [P] [US4] Add failing keyboard-selection parity test for searchable selectors in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 4

- [ ] T028 [US4] Replace Step 1 selector with Fluent searchable selector pattern suitable for v4 (for example `FluentAutocomplete`/`FluentCombobox`) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T029 [US4] Replace Step 2 selector with Fluent searchable selector pattern suitable for v4 in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T030 [US4] Ensure both selectors initialise with explicit unselected placeholder state and no implicit first-option selection in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T031 [US4] Ensure progression logic advances only on explicit user selection events, including first-option selection, in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T032 [US4] Add lightweight UX copy hints for searchable selectors (e.g., search prompt text) in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: Selector usability scales for large datasets and first-option regression is eliminated.

---

## Phase 6: User Story 3 — Stale Preview Warning Inline With Execute Step (Priority: P3)

**Goal**: Keep stale-preview messaging contextual and execution safety enforced.

**Independent Test**: Generate preview, change upstream selection, verify stale warning appears in Step 4 and Step 5 relocks until re-validation.

### Tests for User Story 3 (write first, verify failing)

- [ ] T033 [P] [US3] Add failing test for stale warning placement within Step 4 content in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [ ] T034 [P] [US3] Add failing test for Step 5 lock/unlock transitions driven by stale preview state in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 3

- [ ] T035 [US3] Place stale-preview warning inline inside Step 4 content flow in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T036 [US3] Confirm Step 5 lock state remains derived from existing `preview` and `isPreviewStale` invariants in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: Stale-preview behaviour is contextual, safe, and independently testable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final quality, accessibility, runtime-mode checks, and regression pass.

- [ ] T037 [P] Add or verify `role="region"` and per-step `aria-label` attributes in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T038 [P] Review user-facing copy for UK English consistency in `src/ImportToPlanner.Web/Components/Pages/Home.razor` and `src/ImportToPlanner.Web/Components/Pages/HomeExecutionReport.razor`
- [ ] T039 [P] Validate 1024px viewport behaviour and prevent horizontal scrolling in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`
- [ ] T040 Run full automated test suite via `dotnet test` from repository root and resolve regressions
- [ ] T041 Verify both runtime modes (in-memory and Graph) per `specs/002-ui-ux-redesign/quickstart.md`
- [ ] T042 [P] Confirm NFR-004 performance is not regressed: run the bUnit test suite and verify no new slow-render warnings; record baseline render assertion evidence in `specs/002-ui-ux-redesign/quickstart.md`
- [ ] T043 [P] Confirm NFR-007 AppHost and CI parity: run `dotnet build` from repo root and from the AppHost project and confirm both succeed without errors or new warnings

---

## Dependencies & Execution Order

### Phase dependencies

- Phase 1 → Phase 2
- Phase 2 blocks all user-story phases
- User stories proceed in priority order for incremental delivery: US1 → US2 → US4 → US3
- Polish phase depends on completion of all selected user stories

### Story dependencies

- **US1** depends on foundational tasks only
- **US2** depends on US1 structure being present
- **US4** depends on US1 step shell and selector placement
- **US3** depends on US1 step shell and preview/execute placement

### Key task dependencies

- T004 → T010, T023
- T005 → T015
- T007-T009 before T010-T015
- T016-T018 before T019-T023
- T024-T027 before T028-T032
- T033-T034 before T035-T036
- T040 after all implementation and test tasks
- T041 after T040
- T042 after T040
- T043 after T040

---

## Parallel Opportunities

### Setup/Foundation

- T004 and T005 can run in parallel

### US1

- T007, T008, T009 can run in parallel

### US2

- T016, T017, T018 can run in parallel

### US4

- T024, T025, T026, T027 can run in parallel

### US3

- T033 and T034 can run in parallel

### Polish

- T037, T038, T039, T042, T043 can run in parallel (after T040)

---

## Parallel Example: User Story 4

```bash
# Parallel test authoring tasks
T024: Placeholder initial-state regression test
T025: First-option explicit-selection regression test
T026: Search filtering behaviour test
T027: Keyboard-selection parity test

# Parallel implementation tasks (after tests fail)
T028: Searchable Step 1 selector
T029: Searchable Step 2 selector
```

---

## Implementation Strategy

### MVP first (US1)

1. Complete Phase 1 and Phase 2.
2. Complete US1 (T007-T015).
3. Validate independently using quickstart flow.

### Incremental delivery

1. Add US2 visual progression.
2. Add US4 searchable-selector and first-option regression fixes.
3. Add US3 stale-preview contextual warning behaviour.
4. Complete polish and full regression validation.

### Suggested immediate scope for next implement run

- Foundation + US1 + US4 first (addresses both primary UX flow and known selector bug risk early).

---

## Summary

- Total tasks: 43
- Setup/Foundation tasks: 6
- US1 tasks: 9
- US2 tasks: 8
- US4 tasks: 9
- US3 tasks: 4
- Polish tasks: 7

All tasks follow required checklist format with IDs, labels, and concrete file paths.
