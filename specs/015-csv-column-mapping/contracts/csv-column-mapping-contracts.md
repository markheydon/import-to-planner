# Contract: CSV Column Mapping and Sample Files

## Scope

Column header mapping for CSV import, downloadable sample files, upload-step mapping UI, browser-local layout memory, and public docs updates. Consumes delimiter/BOM output from the existing parser peek path (#133). Does not add Excel (#55), template libraries (#45), or server-side mapping sync.

Traceability: GitHub issue [#127](https://github.com/markheydon/import-to-planner/issues/127); spec FR-001–FR-017.

## Application catalog (`ImportColumnFieldCatalog`)

- Single registry of `ImportColumnFieldDefinition` entries (see [data-model.md](../data-model.md)).
- MUST be the only source of alias strings for automatic mapping in v1.
- Fields MUST be registrable without changing parser control flow (add definition + parser field read branch keyed by `FieldId`).
- MUST NOT include aliases that map Planner export columns incorrectly (for example Status → Bucket).

## Header normalisation (normative)

For matching and `LayoutSignature`:

1. Trim outer whitespace.
2. Compare using invariant culture case folding.
3. For signature and alias match, remove spaces and characters `.`, `-`, `_` from the folded string.

Raw header text is preserved for CsvHelper column lookup and for display in the mapping UI.

## Mapping service (`ICsvColumnMappingService`)

Application boundary. Implementations MUST NOT reference CsvHelper or MudBlazor.

### `BuildProposal`

```text
BuildProposal(
  IReadOnlyList<string> rawHeaders,
  SavedLayoutMapping? savedForLayout)
  → ColumnMappingProposal
```

Rules:

1. Compute `LayoutSignature` from `rawHeaders` order.
2. If `savedForLayout` matches signature, seed assignments from saved map where source headers still exist.
3. For each unassigned source header, try canonical exact match, then alias match (catalog order).
4. Detect duplicate assignment to one `FieldId` → `Status = Conflict`.
5. If Task Name unassigned → `Status = NeedsTaskName` (even if other fields mapped).
6. If all required fields uniquely assigned and no conflict → `Status = Ready` (may still require explicit user confirmation per product rules).

### `ToConfirmedMapping`

```text
ToConfirmedMapping(ColumnMappingProposal proposal, userAssignments)
  → CsvColumnMapping
```

User assignments override suggestions. MUST reject confirmation without Task Name.

## Parser boundary (`ICsvImportParser`)

### `PeekHeadersAsync`

```text
PeekHeadersAsync(string csvContent, CancellationToken cancellationToken)
  → CsvHeaderPeekResult
```

- MUST apply the same BOM strip and delimiter detection as `ParseAsync` before reading the header record.
- MUST NOT require Task Name canonical heading to be present.
- Duplicate normalised headers → file-level error in peek result.

### `ParseAsync` (extended)

```text
ParseAsync(
  string csvContent,
  CancellationToken cancellationToken,
  CsvColumnMapping columnMapping,
  bool ignoreExtraColumns = false)
  → CsvParseResult
```

- `columnMapping` is required for import workflow calls after confirmation.
- Row reading MUST use source column names from `columnMapping.Assignments`.
- Heading validation MUST verify Task Name is mapped via `columnMapping`, not literal `"Task Name"` header presence.
- Unmapped source headers: if `ignoreExtraColumns` is false, emit unexpected column errors; if true, ignore.
- Row-level validation (priority, due date, assignees, description length) unchanged.

Equivalence: A file with canonical headers and identity mapping MUST produce the same `CsvTaskRow` set as today’s parser for the same bytes.

## Layout store (Web — `IImportColumnMappingLayoutStore`)

```text
GetAsync(layoutSignature) → SavedLayoutMapping?
SaveAsync(SavedLayoutMapping mapping) → Task
```

- Storage: browser `localStorage`, namespaced key agreed in implementation (version suffix required).
- MUST NOT persist row data or full CSV text.
- Self-hosted and hosted use the same client-side store.

## UI contract (Home upload step)

MUST on step 3 (upload):

- Offer download of minimal and full sample CSVs (`/samples/import-minimal.csv`, `/samples/import-full.csv` or equivalent stable paths).
- After successful peek, show mapping summary (source → import field) when `Status = Ready`.
- Provide “Change mapping” opening editor with one control per canonical field (dropdown of source headers + “Do not import” for optional fields).
- Block preview action until `IsColumnMappingConfirmed` and Task Name mapped.
- When `Status` is `Conflict` or `NeedsTaskName`, MUST surface mapping editor before preview.

Copy MUST be UK English. Mapping memory privacy note on first save is recommended.

## Public docs contract

Align with `specs/007-end-user-docs-site/contracts/docs-site-contract.md`.

### `/csv-format` and getting-started

- MUST link to the same sample files as the app (relative `/samples/...` URLs on the docs site where static files are published, or duplicated static assets with identical content).
- MUST state that common synonyms are accepted via mapping (not only exact headings).
- Remove or replace “Use these exact headings in the first row” as the sole guidance.

### Troubleshooting (if needed)

- Short entry for “Task Name could not be found” → use mapping UI or download samples.

## Non-goals

- Excel workbook parsing (#55)
- Planner export preset mappings (#55)
- User template libraries (#45)
- Server-side mapping sync across devices
- Delimiter picker or encoding detection beyond #133

## Supersedes

Issue #132 contract note that `Assignee` / `Email` remain unexpected until #127 — after this feature, those aliases map to Assigned To when present in the catalog.
