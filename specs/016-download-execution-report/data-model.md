# Data Model: Download Execution Report as CSV

Traceability: [spec.md](spec.md) FR-001–FR-013; issue [#135](https://github.com/markheydon/import-to-planner/issues/135).

## Entity: ImportSourceRowOutcome (new, Application)

Purpose: One final automated outcome for a row from the uploaded source CSV.

| Field | Type | Required | Notes |
| ----- | ---- | -------- | ----- |
| RowNumber | int | yes | Same convention as `CsvTaskRow.RowNumber` (data row index in source file). |
| TaskName | string | yes | Task name from source row. |
| Outcome | ImportSourceRowOutcomeKind | yes | Created, ReusedOrSkipped, or Failed. |
| TaskIdentifier | string? | no | Planner task id when created; empty for reused/skipped/failed. |
| Reason | string? | no | User-safe explanation for failed or skipped-with-reason rows; empty when not needed. |

Invariants:

- Exactly one `ImportSourceRowOutcome` per task row in the approved preview that was eligible for execution (same cardinality as task actions processed for reporting, including skipped preview rows that appear in execution reporting today).
- `TaskIdentifier` populated only when `Outcome = Created` and the gateway returned an id.
- `Reason` MUST NOT contain secrets or tenant identifiers.

## Enum: ImportSourceRowOutcomeKind (new, Application)

| Value | Meaning |
| ----- | ------- |
| Created | New Planner task created for this source row. |
| ReusedOrSkipped | Row not created (reuse or skip); includes already-exists matches. |
| Failed | Execution failed for this row after preview approval. |

## Entity: ImportExecutionResult (extended)

Existing fields unchanged. Add:

| Field | Type | Required | Notes |
| ----- | ---- | -------- | ----- |
| SourceRowOutcomes | IReadOnlyList&lt;ImportSourceRowOutcome&gt; | yes | Ordered by `RowNumber` ascending. |

Relationship: `SourceRowOutcomes` is derived during execution alongside existing list fields; lists remain for aggregate UI until optionally refactored later.

## Entity: ExecutionReportCsvRow (export projection)

Purpose: One row in the downloaded CSV file (source outcome or manual follow-up).

| Field | Type | Required | Notes |
| ----- | ---- | -------- | ----- |
| RecordType | ExecutionReportCsvRecordType | yes | Source row or Manual follow-up. |
| SourceRowNumber | int? | no | Set for source rows; null for manual. |
| TaskName | string? | no | Task name when applicable. |
| Outcome | string | yes | UK English label per contract. |
| TaskIdentifier | string? | no | Planner task id when created. |
| ReasonOrDetails | string? | no | Failure/skip reason or manual details. |
| ActionType | string? | no | Manual follow-up only; presenter labels. |
| GoalName | string? | no | Manual follow-up when shown on screen. |

Built in Web presenter layer from `ImportExecutionResult` + existing `ManualActionViewModel` mapping (single builder method shared by UI export button).

## Enum: ExecutionReportCsvRecordType

| Value | CSV **Record type** column |
| ----- | -------------------------- |
| SourceRow | `Source row` |
| ManualFollowUp | `Manual follow-up` |

## Entity: ExecutionReportCsvFile (transient)

| Field | Type | Notes |
| ----- | ---- | ----- |
| FileName | string | `import-execution-report-{yyyyMMdd-HHmmss}Z.csv` |
| Content | byte[] | UTF-8, no BOM |
| MediaType | string | `text/csv` |

Not persisted server-side; exists only for the browser download response.

## Validation rules (export)

- Export MUST NOT run when `ImportExecutionReportViewModel` is null (no completed report).
- All string fields MUST pass through CSV escaping (quotes, commas, newlines).
- Manual rows MUST follow all source-row outcomes in the file.
- Column set MUST be stable (same headers on every download); empty cells allowed.

## State

No new persisted state. Export reads the in-memory execution report already held in `WorkflowCoordinationState` / presenter view model for the current session.
