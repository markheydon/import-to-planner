# Feature Specification: CSV To Planner Import Workflow

**Feature Branch**: `[001-import-planner-csv]`  
**Created**: 2026-05-09  
**Status**: Draft  
**Input**: User description: "Single-use utility to import tasks from CSV into Microsoft Planner with explicit validation, dry-run preview, confirmation, and execution reporting."

## Clarifications

### Requirements Clarifications

- Q: For execution, how should the system handle a CSV row that matches an existing Planner task? → A: Skip matched existing tasks and report them as already exists.
- Q: How should the system determine that a CSV row matches an existing Planner task? → A: Match by task name only.
- Q: If execution encounters failures on some rows, what should happen to the rest of the import? → A: Continue processing remaining rows and report partial success/failure.
- Q: For transient Microsoft Graph errors during execution, what retry policy should apply per row? → A: Retry once, then mark row failed.
- Q: If planner state changes after preview but before confirmation, what should execution do? → A: Block execution and require a fresh preview.
- Q: What is the default behaviour when extra CSV columns are present? → A: In the web UI, the `Ignore extra columns` option MUST default to enabled (`ignoreExtraColumns = true`), so extra columns produce no validation errors unless the operator disables that option. The parser API signature MUST default `ignoreExtraColumns` to `false`; non-UI callers must set the flag explicitly.
- Q: Is there a file size limit for uploaded CSV files? → A: Yes. The UI MUST enforce a 10 MB maximum file size. Files exceeding this limit are rejected before parsing.
- Q: What happens when a CSV row specifies no bucket? → A: The system MUST resolve the task to the `General` bucket. This is a fixed default; it is not configurable per request.
- Q: How are textual priority values mapped? → A: The parser MUST accept both numeric values (0–10) and the following text tokens (case-insensitive): `Urgent` → 1, `Important` → 3, `Medium` → 5, `Low` → 9. Any other non-numeric, non-empty priority value is a validation error.
- Q: On what basis is stale-preview detection performed? → A: The orchestrator MUST compute two SHA-256-based fingerprints at preview time: a request fingerprint (derived from the import request content) and a planner-state fingerprint (derived from live bucket and task titles). Before execution, both fingerprints are recomputed and compared. Execution is blocked if either has changed.
- Q: What are the exact manual actions emitted during execution? → A: Two action types are emitted. `EnsureGoalExists` is emitted for every distinct goal across all tasks scheduled for creation or already-existing-matched tasks. `LinkTaskToGoal` is emitted for each (goal, task) pair for both newly created tasks and already-existing-matched tasks.
- Q: How does the system guard against duplicate execution of the same confirmation event? → A: Execution is blocked when either the request fingerprint or the planner-state fingerprint no longer matches the preview. Fingerprint mismatch is the sole protection (no separate one-time token).
- Q: How should mode-specific behaviour differences be indicated? → A: Mode differences are surfaced through behaviour and messaging, not a persistent runtime-mode label in the UI. In Graph mode, an unauthenticated session triggers an automatic sign-in redirect before the import UI is reachable. In in-memory mode, no authentication is required and the container/plan list is populated from in-memory stubs.

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITISED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Validate And Preview Import Plan (Priority: P1)

As an operator, I can upload a CSV file and receive a clear validation and preview of
what will happen before any tasks are created or changed.

**Why this priority**: Safe validation and preview are the core value of this utility and
must exist before execution capabilities.

**Independent Test**: Can be fully tested by uploading valid and invalid CSV files and
verifying that the system produces clear validation output and a non-destructive preview
plan.

**Acceptance Scenarios**:

1. **Given** a CSV file with valid rows, **When** the operator requests preview,
  **Then** the system returns a structured plan showing intended task actions without
  applying changes.
2. **Given** a CSV file with malformed rows or missing required fields, **When** the
  operator requests preview, **Then** the system returns row-level validation errors and
  blocks execution.

---

### User Story 2 - Confirm And Execute Import (Priority: P2)

As an operator, I can explicitly confirm the previewed plan and execute the import so
tasks are created in Planner only after a deliberate confirmation step.

**Why this priority**: Execution delivers the business outcome, but only after safe
preview and validation are in place.

**Independent Test**: Can be fully tested by executing a previously validated preview and
verifying that created tasks match the approved plan.

**Acceptance Scenarios**:

1. **Given** a validated preview plan, **When** the operator confirms execution,
  **Then** the system performs the planned task creation actions and prevents duplicate
  execution of the same confirmation event.
2. **Given** a preview that has unresolved validation errors, **When** the operator
  attempts execution, **Then** the system blocks execution and explains why.

---

### User Story 3 - Review Execution Reporting (Priority: P3)

As an operator, I can review an execution report that summarises completed actions,
failed actions, and any manual follow-up required.

**Why this priority**: Reporting is essential for operational trust and auditability but
depends on execution capability.

**Independent Test**: Can be fully tested by running a confirmed import and verifying that
the report includes totals, per-row outcomes, and manual actions.

**Acceptance Scenarios**:

1. **Given** an import execution has completed, **When** the operator views results,
  **Then** the system displays a clear summary of successful actions, failures, and next
  steps.
2. **Given** some items require follow-up outside system control, **When** the report is
  generated, **Then** those manual actions are explicitly listed and distinguished from
  automated actions.

---

### Edge Cases

- CSV contains duplicate task rows for the same destination context.
- CSV headers are present but one required column is empty for all rows.
- CSV file is empty or contains only headers.
- Preview was generated, but destination selection changes before execution confirmation.
- Execution is interrupted mid-run and must produce a truthful partial completion report.
- Some planner attributes cannot be set automatically and must be surfaced as manual
  actions.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept task import input in CSV format and parse rows into a
  structured import set.
- **FR-002**: System MUST validate required fields and data formats before execution is
  allowed.
- **FR-003**: System MUST return row-level validation errors with enough detail for a user
  to correct the source CSV.
- **FR-004**: System MUST generate a dry-run preview that clearly distinguishes
  non-destructive preview behaviour from execution behaviour.
- **FR-005**: System MUST require explicit user confirmation before performing any
  write-side import action.
- **FR-006**: System MUST block execution when validation errors are unresolved.
- **FR-007**: System MUST execute only the actions represented in the approved preview
  plan.
- **FR-007a**: System MUST skip rows matched to existing Planner tasks and report those
  rows with an `already exists` outcome instead of creating duplicates.
- **FR-007b**: System MUST determine an existing-task match using task name only.
- **FR-008**: System MUST produce an execution report containing totals, per-item outcome,
  and failure details.
- **FR-008a**: System MUST continue processing remaining rows when individual rows fail,
  and MUST report partial success/failure outcomes for the full execution.
- **FR-008b**: For transient Microsoft Graph row-level failures, system MUST retry once
  and then mark the row as failed if the retry also fails.
- **FR-008c**: System MUST detect stale preview state before execution and block execution
  when planner state has changed since preview generation, requiring a fresh preview.
- **FR-009**: System MUST provide explicit manual action guidance where items cannot be
  completed automatically.
- **FR-010**: System MUST preserve user-safe error handling and avoid exposing secrets or
  tenant-sensitive values in user-facing messages.
- **FR-011**: System MUST support operation in both available runtime modes and clearly
  indicate any mode-specific behaviour differences.
- **FR-012**: System MUST enforce a 10 MB maximum file size on CSV uploads and reject
  oversized files before any parsing occurs.
- **FR-013**: In the web UI workflow, `ignoreExtraColumns` MUST default to enabled,
  and CSV files with extra columns MUST pass validation unless the operator disables that
  option. For non-UI callers of the parser abstraction, the `ignoreExtraColumns` flag
  MUST be set explicitly to avoid ambiguity. When extra-column checking is enabled, any
  unrecognised column MUST produce a file-level validation error that blocks execution.
- **FR-014**: The system MUST resolve tasks with no specified bucket to the fixed default
  bucket name `General`.
- **FR-015**: The CSV parser MUST accept priority values as a numeric (0–10) or as one
  of the following case-insensitive text tokens: `Urgent` (→ 1), `Important` (→ 3),
  `Medium` (→ 5), `Low` (→ 9). Any other non-empty priority value MUST produce a
  row-level validation error.
- **FR-016**: Stale-preview detection MUST be based on two independent fingerprints: a
  request fingerprint derived from the import request content, and a planner-state
  fingerprint derived from live bucket and task titles at preview time. Execution MUST
  be blocked when either fingerprint no longer matches the recomputed live value.
- **FR-017**: The execution report MUST emit `EnsureGoalExists` manual actions for every
  distinct goal associated with tasks that are scheduled for creation or matched as
  already-existing. The report MUST also emit `LinkTaskToGoal` manual actions for each
  (goal, task) pair for both newly created tasks and already-existing-matched tasks.
- **FR-018**: In Graph mode, the system MUST redirect unauthenticated sessions to sign-in
  before the import UI is accessible. In in-memory mode, no authentication is required.
  Mode differences are communicated through behaviour and messaging, not a persistent
  mode label in the UI.

### Quality and Non-Functional Requirements *(mandatory)*

- **NFR-001 Code Quality**: Solution MUST preserve defined architectural boundaries and
  avoid introducing unnecessary coupling and preserve clear separation of import parsing,
  planning, execution, and reporting responsibilities.
- **NFR-002 Testing**: Behaviour changes MUST define required automated tests (unit,
  integration, regression as applicable) for validation, preview, confirmation, and
  reporting flows.
- **NFR-003 UX Consistency**: User-facing changes MUST define expected copy, interaction,
  and accessibility consistency with existing validation, preview, confirmation, and
  execution reporting flows.
- **NFR-004 Performance**: Feature MUST define measurable performance expectations for
  affected operations (latency, throughput, memory, or equivalent), including preview
  responsiveness for expected CSV sizes.
- **NFR-005 Runtime Modes**: Feature MUST specify compatibility expectations for
  in-memory and Graph runtime modes, or justify explicit single-mode scope.
- **NFR-006 Scope Boundary**: Feature MUST preserve current single-tenant scope unless
  approved multi-tenant scope expansion is explicitly documented.
- **NFR-007 AppHost and CI**: Feature MUST preserve solution-level and AppHost build/
  validation expectations in CI workflows.
- **NFR-008 Auth and Session Verification**: Tests and documentation MUST explicitly
  verify the mode-entry behaviour defined in FR-018 (Graph mode sign-in gate and
  in-memory mode no-auth access), including sign-in/sign-out UX consistency.

### Key Entities *(include if feature involves data)*

- **Import Request**: Represents a user-initiated import attempt, including source file,
  target planner context, and runtime mode.
- **CSV Task Row**: Represents one row from the CSV with normalized task attributes and
  validation state.
- **Import Plan Item**: Represents one planned action generated during preview, including
  proposed target, operation type, and dependencies.
- **Validation Error**: Represents a specific issue tied to a row/field, including message
  and severity.
- **Execution Result Item**: Represents per-plan-item execution outcome with success/fail
  state, message, and optional manual action note.
- **Manual Action**: Represents a required user follow-up that cannot be automated in the
  current system scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of valid CSV imports containing up to 500 rows can complete preview in
  under 10 seconds.
- **SC-002**: 100% of attempted executions without explicit confirmation are blocked.
- **SC-003**: 100% of executions started from a validated preview produce a completion
  report with per-item outcome statuses.
- **SC-004**: At least 90% of invalid CSV submissions are corrected successfully within one
  additional submission attempt.
- **SC-005**: All required unit, integration, and regression tests for impacted behaviour
  pass in CI.
- **SC-006**: No user-facing error output includes certificate values, secret material, or
  tenant-sensitive identifiers.
- **SC-007**: Mode-specific behaviour differences are documented for every feature change
  that affects runtime mode behaviour.

## Assumptions

- Operators have permission to access at least one valid planner destination context.
- CSV format conventions for task import are documented and available to operators.
- Single-tenant operation remains the active scope for this feature.
- In-memory mode remains available for local validation and Graph mode remains available
  for live tenant execution.
- Manual action support is acceptable for planner capabilities that are not fully
  automatable in the current scope.
- The `General` bucket default is fixed by the implementation; operators must be aware
  that rows without an explicit bucket will be assigned to `General`.
- Priority text tokens (`Urgent`, `Important`, `Medium`, `Low`) map to Planner numeric
  values; operators using a numeric priority column bypass the text lookup.
