# Feature Specification: UI/UX Redesign — Stepped Import Workflow

**Feature Branch**: `002-ui-ux-redesign`  
**Created**: 2026-05-11  
**Status**: Draft  
**Input**: User description: "While the app is currently feature complete for the basic idea of importing tasks from a CSV, the UI isn't the best user experience right now. I want to stick with the Fluent UI that being used but it just doesn't flow very well or even look that great right now. It needs to be much more modern looking and flow almost in step form bit like a wizard type thing but down the page."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Guided step-by-step import (Priority: P1)

A user opens the app and is immediately presented with a clear, numbered step layout guiding them through the import process. Each step appears distinct on the page, with later steps becoming active only when the earlier step's required inputs are satisfied. The user selects a container, then a plan, then uploads a CSV file, then validates, and finally executes — all flowing naturally down the page without needing to scroll back up or hunt for the next action.

**Why this priority**: This is the core user journey. The entire purpose of the redesign is to make this flow obvious and intuitive.

**Independent Test**: Can be fully tested by completing a full import from start to finish and verifying each step activates in sequence, delivering a successful import to Planner.

**Acceptance Scenarios**:

1. **Given** the page has loaded, **When** the user has not yet selected a container, **Then** only Step 1 (Target Selection) is in an active/available state; all subsequent steps are visually inactive or locked.
2. **Given** a container has been selected, **When** the user has not yet selected a plan, **Then** Step 2 (Plan Selection) becomes active; Step 3 onwards remain inactive.
3. **Given** a container and plan have been selected, **When** the user has not yet uploaded a CSV, **Then** Step 3 (Upload & Options) becomes active; Step 4 onwards remain inactive.
4. **Given** all required inputs are complete, **When** the user activates validation, **Then** Step 4 (Validate & Preview) shows results inline within that step.
5. **Given** a successful preview exists, **When** the user confirms execution, **Then** Step 5 (Results) displays the execution report within that step without navigating away.
6. **Given** Step 1 or Step 2 is newly activated, **When** the user has not made an explicit selection in that step, **Then** the selector shows an unselected placeholder state and the next step remains locked.
7. **Given** the first real option in Step 1 or Step 2 is the option the user wants, **When** the user selects that first option explicitly, **Then** the next step unlocks without requiring the user to select a different option first.

---

### User Story 2 — Clear visual status and progress (Priority: P2)

The user can tell at a glance which steps are complete, which is currently active, and which are still pending. Visual indicators (such as step numbers, completion marks, or distinct card styling) communicate progress without requiring the user to read the full page content.

**Why this priority**: Visual progress feedback reduces cognitive load and builds user confidence, particularly for first-time users.

**Independent Test**: Can be tested by partially completing the flow and checking that completed steps display a visual "done" state, the current step is highlighted, and pending steps appear subdued.

**Acceptance Scenarios**:

1. **Given** a step has been completed, **When** the user views the page, **Then** that step's header or indicator shows a completed state (e.g., a tick icon or muted styling).
2. **Given** the user is on an active step, **When** viewing the page, **Then** that step is visually distinct from locked and completed steps (e.g., elevated card, accent border, or different background).
3. **Given** a step is locked pending earlier inputs, **When** viewing the page, **Then** its controls are disabled or visually subdued to communicate it is not yet actionable.

---

### User Story 3 — Stale preview warning inline with the execute step (Priority: P3)

If the user changes the container, plan, or CSV file after a preview has been generated, the execute action becomes unavailable and a contextual warning appears within the relevant step — not as a separate floating message — prompting the user to re-validate before proceeding.

**Why this priority**: Prevents accidental stale-data imports and keeps error messaging contextual rather than scattered across the page.

**Independent Test**: Can be tested by generating a preview, changing the selected container or plan, and verifying the execute step shows a stale-preview warning and disables the confirm button until a new preview is generated.

**Acceptance Scenarios**:

1. **Given** a valid preview has been generated, **When** the user changes the container or plan selection, **Then** the execute step becomes disabled and a stale-preview warning appears within Step 4 (Validate & Preview).
2. **Given** a stale preview warning is showing, **When** the user re-validates with the updated inputs, **Then** the warning clears and the execute step becomes available again.

---

### User Story 4 — Searchable selectors for large tenant datasets (Priority: P2)

When a tenant has many groups or plans, the user can quickly locate a target container and plan using search-enabled selection controls instead of manually scrolling long lists.

**Why this priority**: Large Microsoft 365 tenants can have substantial group and plan counts; searchable selection prevents friction and reduces input errors.

**Independent Test**: Can be tested by loading fixture data with large container and plan lists and verifying that typing in each selector filters options and allows selecting the first matching result.

**Acceptance Scenarios**:

1. **Given** Step 1 has many containers, **When** the user types into the selector search input, **Then** matching containers are filtered in the option list.
2. **Given** Step 2 has many plans, **When** the user types into the selector search input, **Then** matching plans are filtered in the option list.
3. **Given** a user navigates selector options with keyboard input, **When** they confirm a highlighted option, **Then** that option becomes selected and step progression behaves the same as pointer selection.

---

### Edge Cases

- What happens when no containers are available on load? Step 1 must show an informational prompt directing the user to create a Microsoft 365 group or check membership, without blocking the page layout.
- What happens when a plan has no buckets or tasks after execution? The results step must still render clearly, with an appropriate "nothing to report" message per section.
- What happens when the user selects a container that has no plans? Step 2 must show an inline warning within that step prompting the user to create a plan in Planner first.
- How does the layout behave on narrow viewports? The stepped layout must remain usable at narrower screen widths without horizontal scrolling, even if the visual complexity is reduced.
- What happens when the first real option in a selector is the intended value? The workflow must treat explicit selection of that first option as a valid progression event.
- What happens when container or plan lists are very large? Selectors must provide search/filter behaviour so the user is not required to scroll entire lists.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The import page MUST present the workflow as a sequence of clearly numbered, visually distinct steps rendered vertically down the page.
- **FR-002**: Each step MUST become interactive only when its prerequisite inputs from the previous step(s) are satisfied; steps not yet reachable MUST be visually locked.
- **FR-003**: A completed step MUST display a visual completed indicator and a compact inline summary of the user's selection. Interactive expand/collapse of completed steps is deferred to a follow-up iteration (see Research Decision 7).
- **FR-004**: Step 1 MUST allow the user to select a container and refresh the container list; it MUST display an informational message when no containers are found.
- **FR-005**: Step 2 MUST allow the user to select a plan from the chosen container and refresh the plan list; it MUST display an inline warning when no plans exist for the selected container.
- **FR-006**: Step 3 MUST allow the user to upload a CSV file and toggle the "Ignore extra columns" option; the selected file name MUST be displayed once chosen.
- **FR-007**: Step 4 MUST allow the user to trigger validation and preview; validation errors MUST be shown inline within this step; the dry-run preview (bucket actions and task actions) MUST be shown within this step on success.
- **FR-008**: Step 5 MUST allow the user to confirm and execute the import; the execution report (created, reused/skipped, errors, manual actions, and a link to the plan) MUST appear within this step on completion.
- **FR-009**: A stale-preview warning MUST appear within Step 4 when the user's current selections differ from those used to generate the most recent preview, and execution MUST be disabled until a fresh preview is generated.
- **FR-010**: The overall visual design MUST align with modern MudBlazor component patterns: consistent use of card-like containers, appropriate use of accent colours for active steps, neutral or muted styling for locked steps, and clear typographic hierarchy.
- **FR-011**: All existing functional behaviour (container/plan loading, CSV parsing, preview generation, execution, error display, authentication redirect) MUST be preserved without regression.
- **FR-012**: The layout header MUST retain user identity display and sign-in/sign-out actions, consistent with the current design.
- **FR-013**: Container and plan selectors MUST initialise in an unselected placeholder state and MUST NOT default-select the first real option.
- **FR-014**: Step progression MUST require an explicit user selection event for Step 1 and Step 2; selecting the first real option explicitly MUST unlock the next step.
- **FR-015**: Container and plan selectors MUST support searchable filtering suitable for large datasets.

### Quality and Non-Functional Requirements *(mandatory)*

- **NFR-001 Code Quality**: Solution MUST preserve defined architectural boundaries and avoid introducing unnecessary coupling.
- **NFR-002 Testing**: Behaviour changes MUST define required automated tests (unit, integration, regression as applicable).
- **NFR-003 UX Consistency**: User-facing changes MUST define expected copy, interaction, and accessibility consistency with existing flows.
- **NFR-004 Performance**: The stepped layout MUST define measurable UI responsiveness expectations for front-end interactions controlled by this feature (for example, step unlock and preview-state rendering) and MUST not introduce perceptible additional render delay compared to the current flat layout. Third-party component internals and back-end/API latency are out of scope for this feature.
- **NFR-005 Runtime Modes**: The redesigned UI MUST function correctly in both in-memory and Graph runtime modes, including correct authentication redirect and container/plan loading behaviour for each mode.
- **NFR-006 Scope Boundary**: Feature MUST preserve current single-tenant scope; no multi-tenant or multi-plan-simultaneous-import capability is introduced.
- **NFR-007 AppHost and CI**: Feature MUST preserve solution-level and AppHost build/validation expectations in CI workflows.
- **NFR-008 Agent Delegation**: Implementation MUST be delegated to the C# Expert agent registered in `AGENTS.md` for all Blazor component and code changes.
- **NFR-009 MudBlazor-First Inputs**: Selector behaviour MUST be implemented with MudBlazor component capabilities first; custom HTML/CSS input workarounds are allowed only as documented last resort.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time user can complete the full import workflow (container → plan → CSV → validate → execute) without referring to any documentation or receiving support, within 5 minutes.
- **SC-002**: All five steps are visually distinguishable from one another at a glance; a tester reviewing a screenshot can correctly identify which step is active, which are completed, and which are locked.
- **SC-003**: No existing functional behaviour is regressed; the full automated test suite (unit and integration tests) passes without modification to test logic.
- **SC-004**: The stale-preview warning appears correctly in 100% of cases where the user changes selections after generating a preview, verified by automated scenario tests.
- **SC-005**: No architecture boundary violations are introduced; the presentation layer remains the sole location of UI state and rendering changes.
- **SC-007**: In Step 1 and Step 2, the first real option can be selected as the initial selection and unlocks the next step in 100% of tested runs.
- **SC-008**: With large fixture lists, users can locate and select a container/plan via search input without full-list manual scrolling.

## Assumptions

- MudBlazor (`MudBlazor`) is the sole UI component library for this redesign; no additional third-party UI libraries will be introduced.
- The redesign is scoped to the single import page (`Home.razor`) and the shared layout (`MainLayout.razor`); no new pages or routes are required.
- Steps are rendered as a vertical sequence on a single page (not a traditional multi-page wizard with navigation between pages); the user can see earlier steps by scrolling up.
- The design system follows MudBlazor patterns and theme tokens configured for this repository; custom CSS additions must be minimal and consistent with existing patterns.
- Mobile and touch usage remain in scope for primary workflow usability, while desktop browser at or above 1024 px remains the primary target viewport.
- The existing `HomeExecutionReport` component may be refactored or incorporated inline as part of Step 5, at the implementer's discretion, provided no functional behaviour is lost.
- No changes are required to any back-end services, domain models, or infrastructure layers; all changes are confined to the Blazor web front-end project.
