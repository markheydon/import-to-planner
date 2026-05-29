# Feature Specification: Frontend API Separation

**Feature Branch**: `009-frontend-api-separation`

**Created**: 2026-05-29

**Status**: Draft

**Input**: User description: "Refactor import-to-planner into a cleaner and more scalable architecture with a dedicated web frontend and a separate API service. Preserve the current CSV-to-Planner user workflow, including validation, preview, plan/container selection, import execution, and manual follow-up actions where Graph does not support a Planner feature directly.

The purpose of this feature is to improve separation of concerns, future hosting flexibility, testability, and maintainability, while keeping the current user experience and functional scope intact.

Define clear user stories, functional requirements, non-functional requirements, scope, out-of-scope boundaries, and acceptance criteria for an incremental migration rather than a full rewrite.

Reference: https://github.com/markheydon/import-to-planner/issues/57"

## Clarifications

### Session 2026-05-29

- Q: What security posture should the separated API use for migrated workflow slices? → A: Treat the API as a user-scoped service that authorises every request using the signed-in user's context.
- Q: Where should cross-step workflow state live for migrated slices? → A: Let the API own a server-tracked import session for migrated slices, with the frontend resuming that session across steps.
- Q: How should the system handle destination changes after preview? → A: Block execution and require the user to refresh or re-preview if plans or containers changed after preview.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Preserve The Import Journey (Priority: P1)

As a user importing Planner tasks from CSV, I want the existing guided workflow to keep working during the migration so that I can continue validating data, previewing changes, selecting the target plan and container, executing the import, and following any manual post-import actions without learning a new process.

**Why this priority**: Preserving the current end-user workflow is the primary product commitment. The architecture change has no value if it disrupts successful imports or removes safeguards.

**Independent Test**: Can be fully tested by running the migrated workflow from CSV upload through import completion and confirming that each existing user step, warning, and manual follow-up prompt remains available with equivalent behaviour.

**Acceptance Scenarios**:

1. **Given** a valid CSV file, **When** the user starts an import, **Then** the system still guides them through validation, preview, plan or container selection, confirmation, and execution in the same overall order as today.
2. **Given** a CSV file that includes data needing manual completion because Planner does not support the feature directly, **When** the user reviews the preview or final result, **Then** the system clearly identifies those manual follow-up actions and does not silently drop them.
3. **Given** a validation failure or import warning, **When** the user corrects the issue and retries, **Then** the workflow continues to provide actionable feedback without exposing internal service boundaries.

---

### User Story 2 - Migrate In Small Safe Slices (Priority: P2)

As a maintainer, I want the current application to be migrated incrementally to a dedicated web frontend and a separate API service so that the architecture can improve without requiring a risky full rewrite or a long-lived feature freeze.

**Why this priority**: Incremental migration reduces delivery risk, keeps releases practical, and allows parity to be proven a slice at a time instead of betting on a single cutover.

**Independent Test**: Can be fully tested by moving one workflow slice at a time behind the new service boundary and verifying that the migrated slice behaves the same as the existing flow before further slices are moved.

**Acceptance Scenarios**:

1. **Given** an incomplete migration state, **When** a user performs a supported import workflow, **Then** the system still completes the workflow successfully without requiring users to know which slices have migrated.
2. **Given** a workflow slice has been migrated to the separate API service, **When** maintainers validate that slice, **Then** they can demonstrate behavioural parity with the pre-migration experience before the next slice is moved.
3. **Given** a migration release introduces a defect in one migrated slice, **When** the defect is investigated, **Then** the affected boundary and responsibility can be isolated without needing to unwind the whole architectural change.

---

### User Story 3 - Support Independent Frontend And Backend Evolution (Priority: P3)

As an operator or maintainer, I want the web frontend and backend service to have clear responsibilities so that hosting, scaling, testing, and future feature delivery can evolve independently while preserving one coherent product experience.

**Why this priority**: The long-term benefit of the refactor is improved flexibility and maintainability, but it must remain subordinate to workflow continuity and safe migration.

**Independent Test**: Can be fully tested by verifying that the web frontend handles user interaction and presentation concerns while the API service owns import workflow execution and related business operations, with clear contracts between them.

**Acceptance Scenarios**:

1. **Given** the separated architecture is in place for a workflow slice, **When** that slice is reviewed, **Then** presentation responsibilities are owned by the frontend and workflow-processing responsibilities are owned by the API service.
2. **Given** a future hosting change affects only one runtime, **When** the change is prepared, **Then** it can be planned without redefining the end-user workflow or business rules.

### Edge Cases

- What happens when a user moves backwards in the workflow after validation or preview data has already been generated by the separate service?
- How does the system handle temporary loss of communication between the frontend and API service while preserving understandable recovery guidance?
- What happens when available plans or containers change between preview and execution?
- How does the system prevent duplicated imports if a user retries after an uncertain execution outcome?
- What happens when only part of the workflow has been migrated and a user moves across migrated and non-migrated slices in one session?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST preserve the current CSV-to-Planner workflow, including validation, preview, plan or container selection, import execution, and any manual follow-up actions required where Planner support is incomplete.
- **FR-002**: The system MUST introduce a dedicated web frontend and a separate API service with clear responsibility boundaries so that user interaction concerns and import-processing concerns are no longer combined in one delivery surface.
- **FR-003**: The system MUST support incremental migration of workflow slices to the separate API service without requiring a full rewrite or a single cutover release.
- **FR-004**: The system MUST allow users to complete the full import journey successfully during the migration regardless of whether a given workflow slice is already handled by the separate API service.
- **FR-005**: The system MUST present validation findings, preview information, import results, and manual follow-up actions in business terms that remain consistent across migrated and non-migrated slices.
- **FR-006**: The system MUST preserve the existing requirement for users to choose the destination plan and relevant container context before import execution where that choice is part of the current workflow.
- **FR-007**: The system MUST ensure import execution only proceeds after the user has reviewed the preview and explicitly confirmed the action.
- **FR-008**: The system MUST keep manual follow-up guidance visible when Planner does not support a feature directly, including after execution results are returned.
- **FR-009**: The system MUST give users actionable feedback when an import cannot proceed, when preview data becomes stale, when destination choices have changed since preview and execution is blocked pending refresh, or when execution completes with warnings.
- **FR-010**: The system MUST maintain a coherent workflow state across frontend and API interactions so that users do not lose their place when moving through the guided import steps.
- **FR-010a**: The system MUST let the separate API service own a server-tracked import session for migrated slices so validation, preview, destination selection, retry handling, and execution state can be resumed across steps.
- **FR-011**: The system MUST define service contracts that allow the frontend to request validation, preview generation, available destination choices, and import execution outcomes from the API service without relying on presentation-specific wording.
- **FR-011a**: The system MUST require the separate API service to authorise migrated workflow requests using the signed-in user's context rather than relying on the frontend as the final authority for user access decisions.
- **FR-012**: The system MUST allow maintainers to verify behavioural parity for each migrated slice before that slice is treated as complete.
- **FR-013**: The system MUST keep the current functional scope intact for the migration baseline and MUST NOT require new end-user capabilities in order to preserve existing behaviour.

### Non-Functional Requirements

- **NFR-001 Separation Of Concerns**: Responsibilities for presentation, workflow coordination, and import-processing logic MUST be clearly separated so each layer has a single understandable purpose.
- **NFR-002 Maintainability**: The architecture MUST make it easier to change frontend behaviour, backend workflow logic, or hosting arrangements independently without widespread ripple effects.
- **NFR-003 Testability**: Each migrated workflow slice MUST be verifiable through focused automated checks that prove parity of behaviour and boundary ownership.
- **NFR-004 Hosting Flexibility**: The architecture MUST support future hosting changes for the frontend and backend independently without redefining core workflow behaviour.
- **NFR-005 Reliability**: Users MUST receive clear, recoverable guidance for transient communication failures, stale preview data, destination changes detected after preview, and uncertain execution outcomes.
- **NFR-005a Workflow Recovery**: Migrated slices MUST support safe session resumption after navigation changes, transient frontend interruptions, or retried requests without creating ambiguous execution state.
- **NFR-006 Consistency**: User-facing behaviour, wording, and safeguards MUST remain coherent throughout the migration, regardless of where a slice executes.
- **NFR-007 Performance**: The migrated workflow MUST keep the user-perceived responsiveness of validation, preview, and import confirmation within the current interactive experience envelope.
- **NFR-008 Observability**: Maintainers MUST be able to identify which workflow boundary failed or regressed without requiring users to understand internal architecture.
- **NFR-008a Security And Authorisation**: The separated architecture MUST preserve user-scoped authorisation at the API boundary so migrated slices can be audited and validated against the signed-in user's effective access.
- **NFR-009 Accessibility And Usability**: The separated architecture MUST preserve the existing guided, understandable import experience for desktop and mobile browser users.
- **NFR-010 Language Quality**: New contributor-facing and user-facing wording introduced by this feature MUST remain in UK English.

### Scope Boundaries

#### In Scope

- Incrementally separating the current product into a dedicated web frontend and a separate API service.
- Preserving the existing CSV import workflow and safeguards while slices are migrated.
- Defining clear boundaries, contracts, and parity expectations between the frontend and API service.
- Preserving current manual follow-up guidance where Planner does not directly support a feature.
- Enabling future hosting flexibility through clearer architectural separation.

#### Out Of Scope

- Replacing the product workflow with a fundamentally different user journey.
- Expanding the baseline feature set beyond the current CSV-to-Planner import scope.
- Delivering a full rewrite that discards the existing working application before parity is proven.
- Removing manual follow-up steps for Planner limitations that still exist.
- Introducing unrelated planner capabilities, new import formats, or broad product redesign work as part of this migration baseline.

### Key Entities *(include if feature involves data)*

- **Import Session**: The server-tracked guided state for one user-led import attempt, including the source data, validation status, preview context, selected destination, confirmation state, execution outcome, and required manual follow-up actions.
- **Validation Result**: The structured outcome that explains whether uploaded CSV data is suitable for import, what issues must be fixed, and what warnings the user should review.
- **Import Preview**: The structured description of what the import would do, including destination choices, created or updated items, warnings, and any steps that still require manual action.
- **Destination Selection Context**: The available plan and container choices that the user must review before execution together with enough context to confirm the correct target.
- **Import Execution Result**: The structured record of completed work, skipped work, warnings, failures, and manual follow-up actions after execution.
- **Migration Slice**: A distinct portion of the workflow that can be moved behind the separate API service, verified for parity, and released independently of other slices.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete the primary CSV import journey with the same essential steps and safeguards as today throughout the migration period.
- **SC-002**: Each migrated workflow slice has documented parity evidence showing that validation, preview, destination selection, execution, and manual follow-up behaviour remain equivalent for the covered scenarios.
- **SC-003**: 100% of migrated slices can be assigned to a clear frontend responsibility or API-service responsibility with no unresolved overlap in ownership.
- **SC-004**: The migration can be released in multiple increments without requiring a full-service outage or a one-time cutover for users.
- **SC-005**: Users receive clear recovery guidance for transient service communication failures, destination changes detected after preview, and uncertain execution outcomes in all migrated slices.
- **SC-006**: Maintainers can identify the failing boundary for a migrated-slice defect quickly enough to repair the slice without rolling back unrelated migrated behaviour.
- **SC-007**: The migration does not reduce the product's supported baseline scope for CSV import into Planner.

## Assumptions

- The current import workflow, including its safeguards and manual follow-up guidance, is the behavioural baseline for the migration.
- Users should not need training on a new workflow in order for the migration to be considered successful.
- Incremental migration may temporarily involve both migrated and non-migrated workflow slices, but that mixed state must still feel like one product.
- For migrated slices, the API will own the resumable import-session record across validation, preview, destination selection, confirmation, and execution steps.
- If plans or containers change after preview, migrated slices will block execution until the user refreshes or re-runs the preview against current destination data.
- Planner limitations that currently require manual completion will continue to exist during this feature and should be surfaced clearly rather than hidden.
- The feature focuses on architecture separation, migration safety, and parity verification rather than on adding new end-user capabilities.
- The separate API will evaluate authorisation in the signed-in user's context for migrated workflow slices rather than treating the frontend as the sole trusted access boundary.
- Future hosting flexibility is a required outcome, but specific hosting technologies and deployment mechanics are decisions for planning and implementation rather than this specification.