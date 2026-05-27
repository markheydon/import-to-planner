# Tasks: Refine Import Guidance

**Input**: Design documents from `/specs/006-refine-import-guidance/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/import-guidance-ui-contract.md`  
**Tests**: Included and required because this feature changes user-facing workflow wording, visual step-state cues, a shared light-theme token, and the architecture guardrails around Home page guidance logic.  
**Agent delegation**: All coding, architecture, and test implementation tasks MUST be delegated to the C# Expert agent per `AGENTS.md`.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3]**: User story label from `spec.md`; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Capture the focused verification flow and evidence checkpoints before UI wording and state changes begin.

- [ ] T001 Record the focused workflow-copy, responsive-layout, and theme-token verification checkpoints in `specs/006-refine-import-guidance/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the shared Web-owned presentation seams and guardrails that every story depends on.

**⚠️ CRITICAL**: Complete all T002-T005 before any user story work begins.

- [ ] T002 [P] Create the shared workflow step state contract in `src/ImportToPlanner.Web/Components/Pages/HomeWorkflowStepState.cs`
- [ ] T003 [P] Create the shared workflow step presentation model in `src/ImportToPlanner.Web/Components/Pages/HomeWorkflowStepPresentation.cs`
- [ ] T004 [P] Create the page-level workflow state stylesheet scaffold in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`
- [ ] T005 [P] Extend architecture guardrails for Web-owned Home page guidance and state mapping in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`

**Checkpoint**: The Home page has explicit presentation contracts, a dedicated styling surface for step states, and architecture coverage that keeps wording and state logic on the Web side of the boundary.

---

## Phase 3: User Story 1 - Understand the workflow at a glance (Priority: P1) 🎯 MVP

**Goal**: Make the five-step import flow self-explanatory through clearer titles, plain-language actions, and visible current/completed/upcoming states.

**Independent Test**: Open the import page and verify that the five workflow cards use the new action-oriented headings, the active step is highlighted, completed steps show a completion cue, upcoming steps are subdued, and the preview/import buttons use sentence-case labels.

### Tests for User Story 1

- [ ] T006 [US1] Add rendered-heading and action-label regression coverage in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`
- [ ] T007 [US1] Add current/completed/upcoming workflow-state regression coverage in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 1

- [ ] T008 [US1] Populate the five-step presentation mapping and state derivation in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T009 [US1] Replace redundant `Step X` headings and all-caps workflow actions with concise Planner-focused wording in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T010 [US1] Implement current, completed, and upcoming card treatments with completion badge styling in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`

**Checkpoint**: User Story 1 is complete when the page still renders the same five workflow steps but the journey reads clearly without internal labels or ambiguous step state.

---

## Phase 4: User Story 2 - Know the CSV expectations before uploading (Priority: P2)

**Goal**: Add compact top-of-page CSV guidance that tells users the minimum required field and the full accepted field list without turning the page into long-form documentation.

**Independent Test**: Review the introduction area on the import page and confirm it identifies `Task Name` as required, names `Task Name`, `Description`, `Priority`, `Bucket`, and `Goal` as the accepted fields, and keeps the guidance brief.

### Tests for User Story 2

- [ ] T011 [US2] Add introduction guidance regression coverage for the required and accepted CSV field copy in `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`

### Implementation for User Story 2

- [ ] T012 [US2] Add the compact CSV overview, required-field cue, and accepted-fields summary in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T013 [US2] Refine the introduction layout so the added CSV guidance remains compact in `src/ImportToPlanner.Web/Components/Pages/Home.razor.css`

**Checkpoint**: User Story 2 is complete when first-time users can prepare a CSV from the Home page guidance alone without the introduction overwhelming the workflow.

---

## Phase 5: User Story 3 - Understand what still needs manual follow-up (Priority: P3)

**Goal**: Set a friendly expectation that some imported details may still need manual work in Planner after import, using one concise example rather than a limitations list.

**Independent Test**: Review the introduction and execution guidance on the import page and confirm it explains, in plain language, that some supported details still need manual follow-up in Planner after import and uses goals as the single example.

### Tests for User Story 3

- [ ] T014 [US3] Add manual-follow-up guidance and non-brittle Home page guard coverage in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`
- [ ] T015 [US3] Add manual-follow-up wording regression coverage in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 3

- [ ] T016 [US3] Add concise manual follow-up expectation copy with a goals example in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [ ] T017 [US3] Keep preview and execution guidance user-friendly without changing workflow semantics in `src/ImportToPlanner.Web/Components/Pages/Home.razor`

**Checkpoint**: User Story 3 is complete when the page clearly sets manual-follow-up expectations before import without adding dense explanatory text.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalise shared visual alignment and record verification evidence after the user stories are complete.

- [ ] T018 [P] Align the shared light-theme border and divider token to `#d1d1d1` in `src/ImportToPlanner.Web/Themes/ImportToPlannerTheme.cs`
- [ ] T019 [P] Record final focused test, responsive-review, and theme-review evidence in `specs/006-refine-import-guidance/quickstart.md`
- [ ] T020 Run the full feature verification checklist in `specs/006-refine-import-guidance/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the MVP by making the workflow clearer to follow
- **Phase 4 (US2)**: Depends on US1 because the introduction guidance should land against the final step titles and page structure
- **Phase 5 (US3)**: Depends on US2 because the manual-follow-up wording belongs in the same compact introduction and execution guidance surface
- **Phase 6 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is the MVP
- **US2 (P2)**: Depends on US1’s final workflow wording and remains independently testable through introduction-copy checks
- **US3 (P3)**: Depends on US2’s compact introduction structure and remains independently testable through manual-follow-up wording and architecture guard checks

### Within Each User Story

- Add or update tests before the corresponding implementation wherever practical
- Establish shared presentation/state seams before rewriting Home page markup
- Complete markup and wording changes before final style refinement in the same story
- Validate each story independently before moving to the next priority

### Parallel Opportunities

- T002-T005 can run in parallel after Setup is complete
- For US1, T006 and T007 can run in parallel, then T008-T010 proceed sequentially because they share the same Home page presentation surface
- For US2, T011 can run before T012-T013, and T012 can proceed in parallel with any non-overlapping polish preparation
- For US3, T014 and T015 can run in parallel, then T016-T017 proceed sequentially in the shared Home page file
- T018 and T019 can run in parallel after the user stories are complete

---

## Parallel Execution Examples

### User Story 1

```text
T006 (smoke coverage for headings and labels)  ║  T007 (workflow-state coverage)
then:
T008 (step presentation mapping)  →  T009 (workflow wording refresh)  →  T010 (state styling)
```

### User Story 3

```text
T014 (architecture guard coverage)  ║  T015 (manual-follow-up wording tests)
then:
T016 (manual-follow-up copy)  →  T017 (execution guidance refresh)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate the refreshed workflow headings, button labels, and state cues before expanding scope

### Incremental Delivery

1. Setup + Foundational establish the shared presentation and test guardrails
2. US1 delivers the clearer workflow journey and is the MVP
3. US2 adds compact CSV schema guidance
4. US3 adds concise manual-follow-up expectations
5. Polish aligns the light-theme border token and captures final evidence

### Parallel Team Strategy

1. One engineer completes Setup + Foundational to stabilise the Home page presentation seams
2. After Foundational, one engineer can update smoke coverage while another updates workflow-state coverage for US1
3. Once US1 is stable, introduction-copy work and architecture-guard updates can be split across US2 and US3 preparation
4. Theme-token alignment and quickstart evidence capture can be handled in parallel during Polish

---

## Summary

- **Total tasks**: 20
- **Setup + Foundational**: 5 (T001-T005)
- **US1**: 5 (T006-T010)
- **US2**: 3 (T011-T013)
- **US3**: 4 (T014-T017)
- **Polish**: 3 (T018-T020)
- **Parallel [P] tasks**: 6 of 20

Independent test criteria by story:

- **US1**: The five workflow cards use the refreshed titles and action labels, and the current/completed/upcoming states are obvious without extra legend text
- **US2**: The introduction clearly names the required CSV field and the full accepted field list while remaining compact
- **US3**: The page explains manual follow-up expectations in plain language with one concrete example and retains the Web-owned architecture guardrails

Suggested MVP scope: Complete Phase 1, Phase 2, and Phase 3 (US1) before adding the introduction refinements and cross-cutting theme polish.

Format validation: Every task uses the required checklist form with checkbox, task ID, optional `[P]`, required story label in user-story phases, and an exact file path.
