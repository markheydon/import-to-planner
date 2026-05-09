# Import Workflow Contract

## Scope
This contract defines the feature behaviour exposed through the Blazor UI flow and
application-layer abstractions.

## UI Interaction Contract

### Step 1: Input and selection
- User selects planner container and plan.
- User uploads one CSV file.
- System accepts optional `Ignore extra columns` setting.

### Step 2: Validate and preview
- Trigger: `Validate And Preview`
- Preconditions:
  - Container selected
  - Plan selected
  - CSV content present
- Outputs:
  - Validation error list (row/field/message) OR
  - Dry-run preview with plan action, bucket actions, and per-task actions
- Guarantees:
  - No write operation is performed in this step.

### Step 3: Confirm and execute
- Trigger: `Confirm And Execute`
- Preconditions:
  - Preview exists
  - Validation errors unresolved == false
  - Selected container/plan still match request context
  - Preview freshness check passes
- Execution semantics:
  - Existing task match rule: task name only
  - Matched tasks: skip and report `already exists`
  - Row failure handling: continue remaining rows
  - Transient Graph row failures: retry once, then mark row failed
- Outputs:
  - Execution report with Created, ReusedOrSkipped, Errors, ManualActions

### Step 4: Reporting
- Execution report must include clear per-item outcomes and any manual follow-up actions.
- User-visible messages must not leak secrets or tenant-sensitive values.

## Application Abstraction Contract

### ICsvImportParser
- Operation: `ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)`
- Returns:
  - `CsvParseResult.Rows` (normalized valid rows)
  - `CsvParseResult.ValidationErrors` (row/file-level issues)
- Contract notes:
  - Parser must be deterministic for identical CSV input and options.

### IImportPlannerOrchestrator
- Operation: `BuildPreviewAsync(ImportRequest request, CancellationToken cancellationToken)`
  - Produces `ImportPlanPreview` without side effects.
- Operation: `ExecuteAsync(ImportRequest request, ImportPlanPreview preview, CancellationToken cancellationToken)`
  - Executes import according to preview and clarified execution semantics.

### IPlannerGateway
- Provides planner resource access (containers/plans/buckets/tasks) and task creation.
- Runtime-mode contract:
  - In-memory and Graph implementations should preserve consistent behaviour for feature outcomes.

## Error and Consistency Contract
- Validation failures block execution.
- Stale preview blocks execution and requires user to regenerate preview.
- Execution report must remain truthful for partial success scenarios.
