# Contract: Execution Report CSV Download

## Scope

Operator-facing CSV export of the completed import execution report on wizard step 5 (Report). Session-local generation only; no new API routes or server file storage. Does not change execution business rules, credit ledger (#126), or preview validation.

Traceability: GitHub issue [#135](https://github.com/markheydon/import-to-planner/issues/135); spec FR-001–FR-013, SC-005.

## UI boundary (Report step)

### Visibility

- **Download report** control is visible when `ImportExecutionReportViewModel` is present (import has completed and report is shown).
- Control is absent or disabled when no execution report exists (FR-001, US1 scenario 4).

### Action

- Label: **Download report** (UK English).
- Invoking the action downloads one CSV file to the operator’s device without navigating away from the wizard.
- No PDF or other formats (FR-013).

### Non-goals

- **Retry failed rows only** button (FR-012).
- Export of preview/validation-only data before execution.
- Long-lived download history or email/share integrations.

## CSV file format (normative)

### Encoding and separator

- UTF-8 without BOM.
- Comma field separator.
- Fields containing comma (`U+002C`), double quote (`U+0022`), CR, or LF MUST be wrapped in double quotes; internal quotes doubled per RFC 4180.

### Filename

- Pattern: `import-execution-report-{yyyyMMdd-HHmmss}Z.csv`
- `{yyyyMMdd-HHmmss}` is UTC at generation time.
- No path separators or characters invalid on common desktop operating systems.

### Header row (exact column titles, UK English)

| Column order | Title |
| ------------ | ----- |
| 1 | Record type |
| 2 | Source row number |
| 3 | Task name |
| 4 | Outcome |
| 5 | Task identifier |
| 6 | Reason or details |
| 7 | Action type |
| 8 | Goal name |

Every data row MUST include all eight columns (empty cells allowed).

### Record type values

| Value | Use |
| ----- | --- |
| `Source row` | Automated per-source-row execution outcome. |
| `Manual follow-up` | Post-import manual action from the report. |

### Outcome values (column 4)

| Value | When |
| ----- | ---- |
| `Created` | Source row created a new Planner task. |
| `Reused or skipped` | Source row was not created (reuse/skip/already exists). |
| `Failed` | Source row execution failed. |
| `Manual follow-up` | Row is a manual follow-up record (`Record type` = `Manual follow-up`). |

Wording MUST match on-screen report vocabulary (spec assumptions).

### Source row records

One row per source-row outcome in the execution report (FR-002).

| Column | Population |
| ------ | ------------ |
| Record type | `Source row` |
| Source row number | Integer matching source CSV row numbering used in validation messages. |
| Task name | Task name from source row. |
| Outcome | `Created`, `Reused or skipped`, or `Failed`. |
| Task identifier | Planner task id when created; empty otherwise. |
| Reason or details | User-safe failure or skip reason when applicable; empty when not needed. |
| Action type | Empty. |
| Goal name | Empty. |

Rows MUST appear sorted by **Source row number** ascending.

### Manual follow-up records

One row per manual action shown in the execution report manual tab (FR-003, FR-007).

| Column | Population |
| ------ | ------------ |
| Record type | `Manual follow-up` |
| Source row number | Empty. |
| Task name | Task name when shown on screen; empty otherwise. |
| Outcome | `Manual follow-up` |
| Task identifier | Empty. |
| Reason or details | Presenter **Details** text (same as UI). |
| Action type | Presenter action type label (e.g. `Ensure Goal Exists`, `Assign person to task`). |
| Goal name | Goal name when shown on screen; empty otherwise. |

Manual rows MUST follow all source-row records.

### Content exclusions (security)

MUST NOT include:

- Access tokens, refresh tokens, or client secrets.
- Commercial tenant identifiers not already shown safely on the report UI.
- Raw exception stacks or diagnostic codes as the primary reason text (presenter-mapped messages only).

MUST NOT include credit ledger columns in this increment (credits remain on-screen summary only).

## Application boundary

### Structured outcomes

`ImportExecutionResult` MUST expose `SourceRowOutcomes` (`ImportSourceRowOutcome`) populated during `ImportExecutionUseCase` with row number, task name, outcome kind, optional task id, and optional neutral reason before presenter mapping.

### CSV generation

- Serializer lives in Application (`ExecutionReportCsvExporter` or equivalent) accepting export projection rows with the column contract above.
- Presenter or Web adapter maps `ImportExecutionResult` and manual actions to export rows with user-safe strings before calling the serializer.

## Public documentation

| Document | Obligation |
| -------- | ---------- |
| `docs/import-workflow.md` | At least one sentence: report can be downloaded as CSV after import; purpose (keep, share, fix source file). |
| `docs/faq.md` | At least one sentence: how to use downloaded report for re-upload / sharing (FR-011). |

## Automated verification (SC-005)

Tests MUST assert for a mixed-outcome fixture:

- Filename matches `import-execution-report-*Z.csv` pattern.
- Header row matches the eight titles exactly.
- At least one row each for Created, Reused or skipped, Failed, and Manual follow-up with expected column values.
- A field containing a comma is quoted correctly.
