# Data Model: CSV To Planner Import Workflow

## Entity: ImportRequest
- Purpose: Represents one user-initiated import attempt.
- Fields:
  - ContainerId (string, required)
  - ContainerType (enum, required)
  - PlanId (string, required)
  - PlanName (string, required)
  - Rows (list of CsvTaskRow, required, non-empty for execution)
- Validation rules:
  - ContainerId/PlanId must refer to currently selectable planner resources.
  - Rows must pass parse/validation gate before execution.

## Entity: CsvTaskRow
- Purpose: Normalized representation of one CSV row.
- Fields:
  - RowNumber (int, required, > 0)
  - TaskName (string, required, non-empty)
  - Description (string, optional)
  - Priority (int, optional, range 0-10)
  - Bucket (string, optional)
  - Goal (string, optional)
- Validation rules:
  - TaskName required.
  - Priority must be numeric and within supported range when present.

## Entity: ImportValidationError
- Purpose: Captures one validation issue.
- Fields:
  - RowNumber (int, required, 0 for file-level issues)
  - Field (string, required)
  - Message (string, required)
- Relationships:
  - Many-to-one with CsvTaskRow (or file-level when RowNumber == 0).

## Entity: CsvParseResult
- Purpose: Output of CSV parse and validation gate.
- Fields:
  - Rows (list of CsvTaskRow)
  - ValidationErrors (list of ImportValidationError)
  - HasErrors (derived bool)
- State transitions:
  - ParsedWithErrors -> PreviewBlocked
  - ParsedWithoutErrors -> PreviewEligible

## Entity: ImportPlanPreview
- Purpose: Non-destructive preview of import actions.
- Fields:
  - ContainerId (string, required)
  - PlanId (string, optional)
  - PlanName (string, required)
  - PlanAction (PlannedEntityAction, required)
  - BucketActions (map bucket name -> PlannedEntityAction, required)
  - TaskActions (list of ImportTaskPlanItem, required)
- Validation rules:
  - Must correspond to the same ImportRequest used during execution.
  - Must be invalidated when planner state drifts before confirmation.

## Entity: ImportTaskPlanItem
- Purpose: Per-row planned task decision.
- Fields:
  - RowNumber (int, required)
  - TaskName (string, required)
  - Bucket (string, required)
  - Goals (list string, optional)
  - Action (PlannedEntityAction, required)
  - Reason (string, optional)
- Clarified behaviour:
  - Existing match check is task-name only.
  - Existing matches become Skip/Reuse with `already exists` outcome.

## Entity: ImportExecutionResult
- Purpose: Final execution report.
- Fields:
  - PlanId (string, optional)
  - Created (list string, required)
  - ReusedOrSkipped (list string, required)
  - Errors (list string, required)
  - ManualActions (list of ManualAction, required)
- Clarified behaviour:
  - Partial success supported (continue processing remaining rows).
  - Transient row failure policy: one retry, then fail row.

## Entity: ManualAction
- Purpose: Tracks post-import steps that cannot be automated.
- Fields:
  - ActionType (string, required)
  - GoalName (string, optional)
  - TaskName (string, optional)
  - Details (string, optional)

## Enum: PlannedEntityAction
- Values:
  - Reuse
  - Create
  - Skip

## Relationships Summary
- ImportRequest 1..* CsvTaskRow
- CsvParseResult 1..* CsvTaskRow
- CsvParseResult 0..* ImportValidationError
- ImportPlanPreview 1..* ImportTaskPlanItem
- ImportExecutionResult 0..* ManualAction

## Workflow State Model
1. CSV Uploaded -> Parsed
2. Parsed + Errors -> ValidationShown (execution blocked)
3. Parsed + No Errors -> PreviewGenerated
4. PreviewGenerated -> Confirmed
5. Confirmed + PreviewFresh -> Executing
6. Executing -> CompletedWithSummary (created/reused/errors/manual actions)
7. Confirmed + PreviewStale -> ExecutionBlockedRequiresFreshPreview
