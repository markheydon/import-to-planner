# Data Model: Clean Architecture Alignment

## Scope

This feature does not introduce new persistence models. It restructures application-boundary,
presentation-boundary, and workflow-state models so that business policy remains neutral and
outer layers own provider, file-format, and UI concerns.

## Entities

### ImportPlanningRequest

- Purpose: Input contract for the import-planning use case.
- Fields:
  - `ContainerId`: selected planner container identifier.
  - `ContainerType`: business-relevant container type enum.
  - `PlanId`: existing target plan identifier when reusing a plan; otherwise null.
  - `PlanName`: target plan title.
  - `Rows`: parsed task rows from the CSV parser boundary.
- Validation rules:
  - `ContainerId` must be present.
  - `PlanName` must be present after trimming.
  - `Rows` must be non-null; row-level validation remains explicit in planning results.
- Relationships:
  - Produced by the Web workflow coordinator from selected UI state and parser output.
  - Consumed by the planning use case.

### ImportPlanningResult

- Purpose: Structured, presenter-neutral result produced by the planning use case.
- Fields:
  - `ContainerId`
  - `PlanId`
  - `PlanName`
  - `PlanAction`
  - `BucketActions`
  - `TaskActions`
  - `ValidationFindings`: neutral validation issues or warnings.
  - `HasValidationErrors`
  - `RequestFingerprint`
  - `PlannerStateFingerprint`
  - `GeneratedAtUtc`
- Validation rules:
  - Fingerprints must be present for any successful preview.
  - `TaskActions` and `BucketActions` must be structurally consistent with the request rows.
- Relationships:
  - Produced by planning use case.
  - Presented by a Web planning presenter.
  - Used to authorise later execution.

### ImportExecutionRequest

- Purpose: Input contract for the import-execution use case.
- Fields:
  - `ApprovedPlan`: the approved planning result or an execution-ready summary derived from it.
  - `ConfirmationToken` or equivalent parity data derived from preview fingerprints.
  - `Rows`: either carried forward directly or referenced through the approved planning result.
- Validation rules:
  - Must match the approved planning fingerprints.
  - Must only be accepted after explicit confirmation in the Web workflow.
- Relationships:
  - Produced by the Web workflow coordinator after confirmation.
  - Consumed by execution use case.

### ImportExecutionResult

- Purpose: Structured execution outcome without user-facing prose.
- Fields:
  - `PlanId`
  - `CreatedItems`: structured created entities or action records.
  - `ReusedOrSkippedItems`: structured reuse/skip records.
  - `FailureItems`: neutral failure records with category and affected target.
  - `ManualActions`: structured follow-up actions.
  - `OutcomeSummary`
- Validation rules:
  - Summary counts must reconcile with the underlying collections.
  - Failure records must use neutral categories rather than provider names.
- Relationships:
  - Produced by execution use case.
  - Presented by a Web execution presenter.

### PlannerOperationFailure

- Purpose: Neutral failure model shared by use cases and presenters.
- Fields:
  - `Category`: authentication, authorisation, validation, conflict, unavailable, unknown, or equivalent neutral classification.
  - `Target`: plan, bucket, task, container, or workflow-level scope.
  - `Reference`: optional identifier/name for the affected entity.
  - `Retryable`: whether the failure is safe to retry.
  - `DiagnosticCode`: optional stable internal code for logging or support.
- Validation rules:
  - `Category` and `Target` are required.
  - No provider-branded or transport-branded message text belongs in this model.

### WorkflowCoordinationState

- Purpose: Minimal Web-owned state required to move between validation, preview, confirmation, and execution.
- Fields:
  - Selected container and plan references.
  - Uploaded file metadata and parsed-row availability.
  - Current step and step-completion markers.
  - Latest planning presenter output.
  - Preview freshness state and confirmation eligibility.
  - Latest execution presenter output.
  - Busy/error flags for UI coordination only.
- Validation rules:
  - Execution cannot be enabled without a fresh approved preview.
  - Re-entering earlier steps invalidates stale preview/execution-dependent state where required.
- Relationships:
  - Owned by Web workflow collaborator(s).
  - Feeds `Home.razor` bindings and action dispatch.

### PlannerPlan

- Purpose: Domain representation of a planner plan.
- Retained fields:
  - `Id`
  - `Title`
  - `ContainerId`
  - `ContainerType`
- Removed fields:
  - `ContainerUrl`
  - `RawContainerType`
- Rationale: removed fields are adapter-specific API residue rather than business concepts.

## Relationships Overview

- The parser adapter produces parsed rows for the Web workflow coordinator.
- The Web workflow coordinator constructs `ImportPlanningRequest` and sends it to the planning use case.
- The planning use case returns `ImportPlanningResult` through an output boundary.
- The planning presenter converts that result into UI-facing preview state and wording.
- After explicit confirmation, the Web workflow coordinator constructs `ImportExecutionRequest`.
- The execution use case returns `ImportExecutionResult` through an output boundary.
- The execution presenter converts the result into the final execution report shown by the page.

## State Transitions

### Planning flow

1. User selects container, plan, and CSV input.
2. Parser boundary returns parsed rows and validation findings.
3. Web coordinator creates `ImportPlanningRequest`.
4. Planning use case emits `ImportPlanningResult`.
5. Planning presenter shapes preview view state.
6. Confirmation becomes available only when the preview is fresh and acceptable.

### Execution flow

1. User explicitly confirms import based on the latest preview.
2. Web coordinator creates `ImportExecutionRequest` using the approved preview fingerprints.
3. Execution use case emits `ImportExecutionResult`.
4. Execution presenter shapes report sections and wording.
5. Any step changes that invalidate parity mark the preview stale and disable execution until re-planned.

## Mapping Boundaries

- Infrastructure maps CSV and Graph specifics to repository-owned parser and planner gateway contracts.
- Application owns business decisions and structured outcomes only.
- Web presenters own all end-user wording, display grouping, and message severity mapping.
- Razor components bind to presenter/workflow output and do not assemble business or provider messages directly.
