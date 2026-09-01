---

description: "Task list for feature implementation"
---

# Tasks: Import Wizard Stepper Layout

**Input**: Design documents from `/specs/010-wizard-stepper-layout/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: bUnit updates are required by plan.md, quickstart.md, and contracts §Test observables. Tests are integrated into each user-story phase (not TDD-first).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/ImportToPlanner.Web/Features/Import/Pages/Home/`
- **Tests**: `tests/ImportToPlanner.Web.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm scope boundaries and baseline before editing Home presentation.

- [x] T001 Review `specs/010-wizard-stepper-layout/contracts/wizard-layout-ui-contract.md` and MudBlazor skill stepper guidance before editing `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T002 [P] Baseline `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --verbosity minimal` and confirm `ProfilePageTests` passes unchanged
- [x] T003 [P] Confirm `ImportWorkflowCoordinator`, `WorkflowCoordinationState`, commercial use cases, and `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor` remain out of scope per `specs/010-wizard-stepper-layout/plan.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add Home-owned `viewedStep` UI state and MudStepper mapping helpers. **No user story work should begin until this phase is complete.**

**⚠️ CRITICAL**: `viewedStep` is presentation-only; do not add it to `WorkflowCoordinationState` or `ImportWorkflowCoordinator`.

- [x] T004 Add `viewedStep` field (integer 1–5) and `@bind-ActiveIndex` mapping (`viewedStep - 1`) in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StepPresentation.razor.cs`
- [x] T005 Add `GetMudStepCompleted(int step)` and `GetMudStepDisabled(int step)` helpers mapping from existing `IsStepComplete` / `IsStepLocked` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StepPresentation.razor.cs`
- [x] T006 Implement `viewedStep` initialisation to `ActiveStep ?? 5` and auto-advance when the user completes the viewed step in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StepPresentation.razor.cs` per `specs/010-wizard-stepper-layout/research.md` Decision 2
- [x] T007 Add `OnPreviewInteraction` handler to cancel navigation to locked steps in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StepPresentation.razor.cs`

**Checkpoint**: `viewedStep` state and stepper parameter mapping compile; coordinator and presenters unchanged.

---

## Phase 3: User Story 1 - Focus on one import step at a time (Priority: P1) 🎯 MVP

**Goal**: Replace five always-expanded step cards with a vertical `MudStepper` as navigation and a single active working pane. Only one step's detailed form is visible at a time. Completed steps show ticks and compact summaries; locked steps stay unselectable.

**Independent Test**: Complete a full import with commercial hosting off and on. Confirm only one step's content is on screen, step list shows five titles without `Step X` prefixes, preview/execute gating is unchanged, and the execution report still appears on the report step.

### Implementation for User Story 1

- [x] T008 [US1] Replace the five stacked `MudPaper` step cards with a `MudGrid` hosting vertical `MudStepper` (left) and a sibling working-pane `MudItem` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T009 [US1] Bind five `MudStep` instances with `Title`, `SecondaryText` from `GetStepSummary`, `Completed`, and `Disabled` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T010 [US1] Render only the `viewedStep` form (location autocomplete, plan autocomplete, CSV upload, preview grids, confirm + report) in the centre pane in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T011 [US1] Set `NonLinear="true"`, `ShowResetButton="false"`, and suppress built-in Next/Previous footer actions on `MudStepper` in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T012 [US1] Keep **Preview import** and **Confirm import** as the only primary workflow actions on step 4 in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.WorkflowActions.razor.cs` and `Home.razor`; step 5 shows the execution report only
- [x] T013 [US1] Remove obsolete `GetStepCardClass` / `GetStepElevation` card styling helpers from `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StepPresentation.razor.cs`
- [x] T014 [US1] Delete `.step-card`, `.step-card--current`, `.step-card--completed`, `.step-card--upcoming`, and `.step-card__summary` rules from `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor.css`; retain `.step-intro` guidance styles
- [x] T015 [P] [US1] Update `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs` to assert five step titles via `MudStepper`/`MudStep` instead of five `.step-card` nodes
- [x] T016 [P] [US1] Update `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs` to assert current/complete/upcoming step state via stepper parameters or copy, not `.step-card--*` CSS classes
- [x] T017 [US1] Update `tests/ImportToPlanner.Web.Tests/TestInfrastructure/HomePageTestContext.cs` if shared selectors or render helpers change for the stepper layout

**Checkpoint**: Signed-in user sees stepper + single pane; full import path works; smoke and workflow tests pass without `.step-card` selectors.

---

## Phase 4: User Story 2 - Keep identity, theme, and commercial gates outside the wizard (Priority: P1)

**Goal**: Persistent header (theme, email, tenant, Sign out, commercial Profile) stays above the wizard. Commercial login and deleted-account gates replace the wizard entirely. Self-host shows no Profile or commercial gates.

**Independent Test**: Review Home signed-in and signed-out with commercial on and off, including deleted-account retention. Confirm chrome and gates match today's rules; stepper never appears on commercial blocking screens.

### Implementation for User Story 2

- [x] T018 [US2] Keep theme, email, tenant name, Sign in/out, and commercial Profile in the full-width header `MudPaper` above the wizard in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor` and `Home.ThemeHeader.razor.cs`
- [x] T019 [US2] Render `MudStepper` and working pane only inside the import-allowed branch (not `showCommercialLoginGate`, not `showCommercialDeletedAccountGate`) in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T020 [US2] Keep global workflow alerts (first-sign-in success, status, tenant mismatch, admin consent, unsupported account) above the stepper in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StatusDiagnostics.razor.cs` and `Home.razor`
- [x] T021 [US2] Ensure first-sign-in account-created success `MudAlert` appears once globally above the wizard, not duplicated inside a step pane in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.CommercialAccess.razor.cs`
- [x] T022 [P] [US2] Add assertions in `tests/ImportToPlanner.Web.Tests/HomePageCommercialAccessTests.cs` that `MudStepper` and "Select Planner location" are absent on the commercial login gate
- [x] T023 [P] [US2] Add assertions in `tests/ImportToPlanner.Web.Tests/HomePageCommercialRetentionTests.cs` that stepper, working pane, and summary rail are absent during deleted-account retention
- [x] T024 [P] [US2] Verify `tests/ImportToPlanner.Web.Tests/HomePageIdentityContextTests.cs` still asserts email, tenant name, and Profile `href="/profile"` when commercial and signed in
- [x] T025 [US2] Verify self-host (`commercialModeEnabled: false`) in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs` shows wizard after sign-in with no Profile and no commercial gates

**Checkpoint**: All identity and commercial gate behaviours preserved; gates hide the entire wizard; `ProfilePageTests` still pass unchanged.

---

## Phase 5: User Story 3 - Collapse completed setup and keep context in a summary rail (Priority: P2)

**Goal**: Steps 1–3 collapse by default when complete and the user is on preview or confirm. A sticky right summary rail shows location, plan, CSV file, preview/execution status, and Open in Planner when a plan id exists.

**Independent Test**: Progress through setup, confirm setup panels collapse on steps 4–5, summary rail shows current context, expand a setup step to change a value, and confirm stale-preview / execute blocking still matches today.

### Implementation for User Story 3

- [x] T026 [US3] Wrap steps 1–3 form content in `MudExpansionPanels` / `MudExpansionPanel` in the working pane in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T027 [US3] Implement `SetupPanelExpansion` logic (`IsExpanded` true when `viewedStep == step`; collapsed when complete and `viewedStep` is 4 or 5) in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StepPresentation.razor.cs`
- [x] T028 [US3] Add right-rail `MudItem` with sticky `MudPaper` + `MudStack` + `MudChip` summary (location, plan, CSV file, preview/execution status) in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T029 [US3] Map `ImportContextSummary` fields from existing selection and preview/execution state in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.SelectionState.razor.cs`
- [x] T030 [US3] Add Open in Planner link in the summary rail using existing plan URL logic when `preview.Preview.PlanId` or `executionResult.PlanId` is present in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T031 [US3] Show empty or "not yet chosen" placeholders in the summary rail when location, plan, or file is unset in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T032 [P] [US3] Add sticky-rail positioning via MudBlazor utility classes in `Home.razor`; add minimal isolated CSS in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor.css` only if utilities cannot pin the rail
- [x] T033 [P] [US3] Add bUnit tests for summary rail labels and collapsed setup panels (no full location/plan/CSV forms when `viewedStep` is 4 or 5 unless expanded) in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

**Checkpoint**: Wide-layout import shows sticky context rail; setup steps collapse; changing setup still invalidates preview/execute as today.

---

## Phase 6: User Story 4 - Scan import results without a long report stack (Priority: P3)

**Goal**: Execution report shows created, skipped, manual, and error counts before detailed tables. Manual follow-up items are visually distinct by action type. Preview step may show compact counts before full tables.

**Independent Test**: Run preview and import with mixed outcomes. Confirm count chips appear before tables; report meaning and data are unchanged.

### Implementation for User Story 4

- [x] T034 [US4] Add created / skipped / manual / error count chips or compact stat cards at the top of the Summary tab before existing tables in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor`
- [x] T035 [P] [US4] Add optional compact planned-action counts above preview `MudDataGrid` tables in step 4 pane in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T036 [US4] Style manual follow-up action types with coloured `MudChip` labels in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor` without changing view-model fields
- [x] T037 [P] [US4] Add bUnit assertions that execution report count chips render before detail tables in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

**Checkpoint**: Post-import and preview screens are scannable; underlying report data and classification unchanged.

---

## Phase 7: User Story 5 - Use the wizard on a narrow screen (Priority: P4, optional)

**Goal**: On small viewports, the wizard remains usable via stacked layout or drawer. Theme, Profile (commercial), and Sign out stay reachable with sensible focus order.

**Independent Test**: Emulate a narrow viewport, progress through steps, and confirm header actions remain reachable without horizontal page scrolling.

### Implementation for User Story 5

- [x] T038 [US5] Add `MudHidden` / responsive `MudGrid` stacking (stepper full width, then pane, then summary) below `md` breakpoint in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T039 [US5] Optional: add `@bind-Open` `MudDrawer` for the step list on narrow viewports with header chrome kept outside the drawer in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor`
- [x] T040 [US5] Verify keyboard focus order for step controls, theme menu, Profile, and Sign out on narrow layout (manual pass per `specs/010-wizard-stepper-layout/quickstart.md` §Manual visual pass)

**Checkpoint**: Narrow viewport wizard is operable; header chrome is not trapped behind the step list.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, deduplication, and format gate.

- [x] T041 [P] Audit and remove duplicate global vs step-level `MudAlert` instances for the same condition in `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.StatusDiagnostics.razor.cs` and `Home.razor`
- [x] T042 Run `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --verbosity minimal`
- [x] T043 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [x] T044 Execute manual visual pass from `specs/010-wizard-stepper-layout/quickstart.md` with commercial mode on and off
- [x] T045 [P] If AppHost is running, run `aspire resource web rebuild` after Web changes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational — core MVP stepper layout
- **User Story 2 (Phase 4)**: Depends on Phase 3 markup existing — gate/chrome verification; can overlap final US1 tasks
- **User Story 3 (Phase 5)**: Depends on Phase 3 — requires stepper skeleton and single pane
- **User Story 4 (Phase 6)**: Depends on Phase 3 — report polish in existing confirm/preview panes
- **User Story 5 (Phase 7)**: Depends on Phases 3–5 — optional; skip if stacked layout from US1/US3 is already usable
- **Polish (Phase 8)**: Depends on desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational — no dependencies on other stories
- **User Story 2 (P1)**: Depends on US1 wizard markup — independently testable via gate/chrome tests
- **User Story 3 (P2)**: Depends on US1 — independently testable once stepper exists
- **User Story 4 (P3)**: Depends on US1 — independently testable via preview/import journeys
- **User Story 5 (P4)**: Depends on US1; benefits from US3 rail — **optional**

### Within Each User Story

- Stepper structure before test updates
- Working pane before expansion panels (US3)
- Summary rail after pane layout (US3)
- Count chips before table assertion tests (US4)

### Parallel Opportunities

- T002 and T003 (Setup) can run in parallel
- T015 and T016 (US1 tests) can run in parallel after T008–T014
- T022, T023, and T024 (US2 tests) can run in parallel
- T032 and T033 (US3 CSS/tests) can run in parallel after T028–T031
- T035 and T037 (US4 preview counts and tests) can run in parallel after T034
- T041 and T045 (Polish) can run in parallel with T042–T044

---

## Parallel Example: User Story 1

```bash
# After T008–T014 implementation, update tests in parallel:
Task T015: "Update HomePageSmokeTests.cs to assert MudStepper instead of .step-card"
Task T016: "Update HomePageWorkflowTests.cs for stepper-based step state assertions"
```

---

## Parallel Example: User Story 3

```bash
# After summary rail markup (T028–T031), in parallel:
Task T032: "Sticky rail CSS in Home.razor.css if needed"
Task T033: "bUnit tests for summary rail and collapsed setup panels in HomePageWorkflowTests.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (stepper + single pane)
4. Complete Phase 4: User Story 2 (gates and chrome verification)
5. **STOP and VALIDATE**: Full import in self-host and commercial modes; gates hide wizard
6. Run format gate and Web.Tests

### Incremental Delivery

1. Setup + Foundational → viewed-step infrastructure ready
2. User Story 1 + 2 → MVP stepper layout with preserved gates (**deploy/demo**)
3. User Story 3 → collapsible setup + summary rail
4. User Story 4 → execution report and preview count polish
5. User Story 5 → optional narrow-viewport drawer/stack (skip if already usable)

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Developer A: User Story 1 implementation (T008–T014)
3. Developer B: User Story 1 tests (T015–T017) once markup lands; then User Story 2 tests (T022–T025)
4. After US1+US2 checkpoint: Developer A takes US3 rail; Developer B takes US4 report polish
5. User Story 5 only if narrow layout needs dedicated work

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Do not edit `ImportWorkflowCoordinator`, Application, Domain, or `/profile` markup
- MudBlazor decision order: component → layout primitives → utilities → theme → isolated CSS last
- UK English for all user-visible strings
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
