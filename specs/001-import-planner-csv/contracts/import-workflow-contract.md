# Import Workflow Contract

## Scope
This contract defines the feature behaviour exposed through the Blazor UI flow and
application-layer abstractions.

## Startup and Authentication Contract

### Graph mode
- When `PlannerGateway:UseGraph` is `true`, the application enforces sign-in at startup.
- Unauthenticated requests are automatically redirected to the configured Entra/MSAL sign-in flow before the import UI is reachable.
- The UI exposes a sign-in/sign-out control in the application shell.
- A certificate compatibility shim is applied at startup: if `Certificates:0:Path` is present but unavailable, the application falls back to `CertificateDiskPath` if configured.

### In-memory mode
- When `PlannerGateway:UseGraph` is `false` (default), no authentication is required.
- The container/plan list is populated from in-memory stubs.
- The sign-in/sign-out control is not shown.

## UI Interaction Contract

### Step 1: Input and selection
- User selects planner container and plan.
- User uploads one CSV file.
- System accepts optional `Ignore extra columns` setting.
- **File size constraint**: files larger than 10 MB are rejected immediately; no parsing occurs.
- **Default parser options**: `Ignore extra columns` is **enabled** by default. The operator may disable it to treat unrecognised columns as validation errors.

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
  - Stale-preview detection: based on two SHA-256 fingerprints (request content + planner state). Execution is blocked when either fingerprint differs from the preview-time value.
  - Duplicate-execution guard: fingerprint mismatch is the sole guard. There is no separate one-time token; identical fingerprints do not guarantee a replay cannot occur in theory, but the fingerprint check is the defined contract boundary.
- Outputs:
  - Execution report with Created, ReusedOrSkipped, Errors, ManualActions

### Manual action contract
- The execution report MUST contain `EnsureGoalExists` actions for every distinct goal name across all tasks that are either:
  - Scheduled for creation (Action == Create), or
  - Matched as already-existing (Action == Skip, Reason == "already exists")
- The execution report MUST contain `LinkTaskToGoal` actions for each (goal, task) pair from both newly created tasks and already-existing-matched tasks.
- Duplicate (goal, task) pairs are deduplicated; each pair is emitted at most once.
- No manual actions are emitted for tasks that are skipped due to being duplicates within the CSV itself (Reason == "duplicate in CSV").

### Step 4: Reporting
- Execution report must include clear per-item outcomes and any manual follow-up actions.
- User-visible messages must not leak secrets or tenant-sensitive values.
- The report includes an `OutcomeSummary` with aggregate counts (created, reused/skipped, errors, manual actions) and derived status flags (partial success, full failure).

## Application Abstraction Contract

### ICsvImportParser
- Operation: `ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)`
  - Default: `ignoreExtraColumns = true` in the UI (the default value in the parser signature is `false` but the UI passes `true` by default).
- Returns:
  - `CsvParseResult.Rows` (normalized valid rows)
  - `CsvParseResult.ValidationErrors` (row/file-level issues)
- Contract notes:
  - Parser must be deterministic for identical CSV input and options.
  - Priority text tokens (`Urgent` → 1, `Important` → 3, `Medium` → 5, `Low` → 9) are resolved during parsing; the `CsvTaskRow.Priority` field always contains a numeric value or null.
  - Rows with an empty/null bucket are stored as-is; bucket defaulting to `General` is performed by the orchestrator, not the parser.

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
