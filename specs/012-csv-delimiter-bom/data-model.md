# Data Model: Detect CSV Delimiter and UTF-8 BOM

No new persisted entities. Detection is a transient parse-time concern on the existing CSV import models.

## Existing entities (unchanged shape)

### CsvTaskRow

Normalised row after a successful parse.

- `RowNumber` (int): source row index from the parser
- `TaskName` (string, required)
- `Description`, `Priority`, `Bucket`, `Goal` (optional; existing rules)

Relationship: produced only after delimiter detection succeeds and heading/row validation runs.

### CsvParseResult

- `Rows`: valid `CsvTaskRow` list
- `ValidationErrors`: file-level and row-level `ImportValidationError` items
- `HasErrors`: true when any validation error exists (preview remains blocked)

### ImportValidationError

- `RowNumber`: `0` for file-level issues (delimiter, empty file, missing header row, missing required heading)
- `Field`: `"File"` for delimiter/BOM-independent file failures; `"Task Name"` (and others) only after a separator is chosen
- `Message`: structured outcome text displayed by the Web grid (UK English)

## New conceptual entities (parse-time only)

### DetectedFieldSeparator

Not stored. Values for this increment:

| Value        | Meaning |
| ------------ | ------- |
| Comma        | Unquoted commas in the header, no unquoted semicolons |
| Semicolon    | Unquoted semicolons in the header, no unquoted commas |
| SingleColumn | Neither comma nor semicolon unquoted, and no unsupported separator character; parse as comma |
| Ambiguous    | Both comma and semicolon appear unquoted in the header → file-level error, no rows |
| Unsupported  | Tab, pipe, or other multi-field separator without a supported delimiter → file-level error, no rows |

### HeaderLine

First logical header line after stripping a leading U+FEFF. Used only for separator detection. Quoted segments are opaque: commas and semicolons inside quotes do not count toward detection.

## Validation rules

1. Strip at most one leading U+FEFF before empty-file checks.
2. Empty or whitespace-only content (after BOM strip) keeps the existing empty-file error.
3. Separator detection runs before CsvHelper record parsing and before `Task Name` heading validation.
4. Ambiguous and unsupported outcomes emit **one** file-level error and **zero** rows.
5. After a successful choice, existing heading rules apply (`Task Name` required; extra columns per `ignoreExtraColumns`).
6. Quoted field values may contain comma and semicolon; they must not split columns.
7. File size remains a Web upload concern (10 MB) and is unchanged.
8. Sample CSV examples in public docs remain comma-separated UTF-8 without a BOM.

## State transitions (upload → preview)

```text
Upload CSV
    → (optional BOM strip)
    → Detect separator from header
        → Ambiguous / Unsupported → ParseErrors (File) → stay on upload/preview blocked
        → Comma / Semicolon / SingleColumn → parse rows
            → heading/row errors → ParseErrors → preview blocked
            → success → preview as today
```

Delimiter detection does not add workflow steps or user-selectable options.

## Traceability

| Spec | Model rule |
| ---- | ---------- |
| FR-001 | BOM strip before heading match |
| FR-002, FR-006 | Comma / Semicolon / SingleColumn |
| FR-003 | Ambiguous |
| FR-004 | Unsupported |
| FR-005 | Quoted fields after delimiter is set |
| FR-007 | Unchanged 10 MB and extra-column behaviour |
| FR-008 | Detection inside parser adapter, before any future mapping |
| FR-011, FR-012 | No workbook type; no delimiter picker entity |
