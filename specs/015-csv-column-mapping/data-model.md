# Data Model: CSV Column Mapping and Sample Files

## Persisted entities

### SavedLayoutMapping (browser localStorage, v1)

Represents one user-confirmed mapping for a header layout. Not stored on the server.

| Field | Type | Rules |
| ----- | ---- | ----- |
| `LayoutSignature` | string | Stable key from ordered normalised headers; no file name |
| `Assignments` | map source header → `FieldId` or `Skip` | Source keys use trimmed text as shown in the file |
| `UpdatedUtc` | DateTimeOffset | Last confirmation time |

Constraints:

- MUST NOT contain CSV row values, task names, or file contents.
- Replacing an entry for the same `LayoutSignature` overwrites the previous assignments.

## Application policy entities

### ImportColumnFieldDefinition (catalog entry)

| Field | Type | Rules |
| ----- | ---- | ----- |
| `FieldId` | string | Stable code (for example `task-name`, `due-date`) |
| `DisplayName` | string | User-facing; aligns with validation `Field` where practical |
| `CanonicalHeaders` | string[] | Exact accepted headings (case-insensitive match after trim) |
| `Aliases` | string[] | Synonyms; maintained in one catalog |
| `IsRequired` | bool | Only Task Name is true in v1 |
| `IsEnabled` | bool | Allows future fields to register without second mapper |

Initial alias set (extensible in catalog only): per issue #127 — Title, Subject, Task, Name, TaskName, Task title → Task Name; Notes, Body, Details, Comments → Description; Pri, Importance → Priority; Bucket Name, BucketName → Bucket; Objective, Theme → Goal; Due date, DueDate, Deadline → Due Date; Assignee, Assignees, Email → Assigned To.

### CsvColumnMapping (confirmed, per upload session)

| Field | Type | Rules |
| ----- | ---- | ----- |
| `Assignments` | map `FieldId` → source header name | Each canonical field at most one source; Task Name required |
| `LayoutSignature` | string | Computed at upload from raw headers |

Used by `ParseAsync` to read row values. Inverse view of saved layout for persistence.

### ColumnMappingProposal (transient)

| Field | Type | Rules |
| ----- | ---- | ----- |
| `SourceHeaders` | string[] | Raw first-row labels after delimiter detection |
| `SuggestedAssignments` | map `FieldId` → source header or null | From exact match, alias, or saved layout |
| `Status` | enum | `Ready` (unique Task Name, no conflicts), `NeedsTaskName`, `Conflict`, `NeedsConfirmation` |
| `Conflicts` | list | Pairs of source headers competing for one `FieldId` |

### CsvHeaderPeekResult (transient)

| Field | Type | Rules |
| ----- | ---- | ----- |
| `Headers` | string[] | Raw header cells |
| `ValidationErrors` | `ImportValidationError[]` | File-level: empty file, delimiter failure, missing header row, duplicate normalised headers |

## Existing entities (extended usage)

### CsvTaskRow / CsvParseResult / ImportValidationError

Unchanged shape. Parser populates rows using `CsvColumnMapping` instead of hard-coded header names. File-level mapping errors use `RowNumber = 0` and `Field = "Mapping"` or `"File"` as appropriate.

### WorkflowCoordinationState (Web)

New session fields:

| Field | Purpose |
| ----- | ------- |
| `HeaderPeek` | Last peek result for current file |
| `ColumnMappingProposal` | Current proposal |
| `ConfirmedColumnMapping` | User-confirmed mapping |
| `IsColumnMappingConfirmed` | Gate for preview |
| `ShowMappingEditor` | UI flag |

Invalidated when CSV content cleared or replaced.

## Static content

### Example CSV assets

| Asset | Headers | Rows |
| ----- | ------- | ---- |
| `import-minimal.csv` | Task Name | 2+ placeholder tasks |
| `import-full.csv` | All enabled catalog canonical headers | Example values valid for parser rules |

## Validation rules

1. Peek runs delimiter/BOM pipeline before mapping (no duplicate delimiter logic).
2. Duplicate normalised source headers → file-level error; no mapping proposal.
3. Preview and execute require `IsColumnMappingConfirmed` and Task Name assigned.
4. Alias catalog MUST NOT map Status → Bucket or similar Planner-export collisions.
5. Saved layout applies before alias fill-in for matching source headers; aliases apply only for unassigned sources.
6. Changing mapping after preview marks preview stale (same as other upload changes).

## State transitions

```text
Choose / upload CSV
  → PeekHeadersAsync
      → errors → ParseErrors, stay on upload
      → success → build Proposal (saved layout + aliases)
          → Conflict / NeedsTaskName → mapping editor required
          → Ready → show summary; optional auto-confirm or one-click continue
User confirms mapping
  → persist SavedLayoutMapping (localStorage)
  → IsColumnMappingConfirmed = true
Preview
  → ParseAsync(csv, mapping, ignoreExtraColumns)
      → row/file validation as today
```

## Traceability

| Spec | Model rule |
| ---- | ---------- |
| FR-001–FR-002 | Example CSV assets |
| FR-004–FR-006 | Catalog + normalisation + `LayoutSignature` |
| FR-008–FR-010 | Proposal status + `CsvColumnMapping` |
| FR-012–FR-014 | `SavedLayoutMapping` |
| FR-015 | Unmapped headers + `ignoreExtraColumns` |
