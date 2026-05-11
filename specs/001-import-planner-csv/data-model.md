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
- Invariants:
  - Bucket may be null/empty in the row; the orchestrator resolves a null/empty bucket to `General` before adding the row to a plan item.
  - Priority is stored as the resolved numeric value (0–10); text tokens are mapped by the parser before the row is created.

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
- Additional fields (implementation):
  - RequestFingerprint (string, required) — SHA-256 digest of the serialised import request content. Used to detect changes to the request between preview and execution.
  - PlannerStateFingerprint (string, required) — SHA-256 digest of live bucket names and task titles at preview generation time. Used to detect planner-state drift before execution.
  - GeneratedAtUtc (DateTimeOffset, required) — timestamp of preview generation for audit/display purposes.
  - HasValidationErrors (bool, required) — true when the request rows had unresolved validation errors; blocks execution when true.

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
- Additional fields (implementation):
  - ReportStatus (string, required) — short label used in the execution report UI (e.g. `Pending`, `Skipped`). Set at preview time; not updated during execution. The report derives final display state from the execution result, not from this field.
- Invariants:
  - Bucket is always non-empty (defaulted to `General` if the CSV row had no bucket).
  - When Action == Skip and Reason == "already exists", the task matched an existing Planner task by name. When Reason == "duplicate in CSV", the task name appeared more than once in the source CSV.

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
- Additional fields (implementation):
  - OutcomeSummary (ImportExecutionOutcomeSummary, required) — aggregated counters derived from the Created/ReusedOrSkipped/Errors/ManualActions lists at result construction time.

## Entity: ImportExecutionOutcomeSummary
- Purpose: Aggregated numeric counters for the execution report header.
- Fields:
  - CreatedCount (int, required) — number of entries in Created.
  - ReusedOrSkippedCount (int, required) — number of entries in ReusedOrSkipped.
  - ErrorCount (int, required) — number of entries in Errors.
  - ManualActionCount (int, required) — number of entries in ManualActions.
  - IsPartialSuccess (bool, derived) — true when ErrorCount > 0 and (CreatedCount > 0 or ReusedOrSkippedCount > 0).
  - IsFullFailure (bool, derived) — true when CreatedCount == 0 and ReusedOrSkippedCount == 0 and ErrorCount > 0.
- Invariants:
  - Constructed from the corresponding lists at result build time; never mutated independently.

## Entity: ManualAction
- Purpose: Tracks post-import steps that cannot be automated.
- Fields:
  - ActionType (string, required)
  - GoalName (string, optional)
  - TaskName (string, optional)
  - Details (string, optional)
- Known action types (implementation):
  - `EnsureGoalExists` — emitted once per distinct goal name across all tasks scheduled for creation or matched as already-existing. Instructs the operator to confirm the goal/category exists in Planner (and create it if it does not).
  - `LinkTaskToGoal` — emitted once per (goal, task) pair for both newly created tasks and already-existing-matched tasks. Instructs the operator to manually link the task to the goal in Planner.
- Invariants:
  - Duplicate (goal, task) pairs are deduplicated; each pair is emitted at most once.

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
