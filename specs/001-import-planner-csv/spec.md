# Feature Specification: CSV To Planner Import Workflow

**Feature Branch**: `[001-import-planner-csv]`  
**Created**: 2026-05-09  
**Status**: Draft  
**Input**: User description: "Single-use utility to import tasks from CSV into Microsoft Planner with explicit validation, dry-run preview, confirmation, and execution reporting."

## Clarifications

### Session 2026-05-09

- Q: For execution, how should the system handle a CSV row that matches an existing Planner task? -> A: Skip matched existing tasks and report them as already exists.
- Q: How should the system determine that a CSV row matches an existing Planner task? -> A: Match by task name only.
- Q: If execution encounters failures on some rows, what should happen to the rest of the import? -> A: Continue processing remaining rows and report partial success/failure.
- Q: For transient Microsoft Graph errors during execution, what retry policy should apply per row? -> A: Retry once, then mark row failed.
- Q: If planner state changes after preview but before confirmation, what should execution do? -> A: Block execution and require a fresh preview.

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
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
