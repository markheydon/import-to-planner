# Research: CSV Column Mapping and Sample Files

## 1. Where mapping policy lives

**Decision**: Canonical import fields, alias lists, header normalisation rules, layout signature calculation, and automatic mapping proposals live in **Application** (`ImportToPlanner.Application`). CSV tokenisation (delimiter/BOM, CsvHelper record read) stays in **Infrastructure** (`CsvImportParser`).

**Rationale**: Constitution II and III require technology-neutral policy and explicit Application seams. Column naming is import policy, not Graph or CsvHelper detail.

**Alternatives considered**:

- **All logic inside `CsvImportParser`**: Rejected — duplicates policy in Infrastructure and blocks Excel (#55) from reusing the same mapper without pulling CsvHelper inward.
- **Domain entities for aliases**: Rejected — no business invariant; this is import-format policy owned by Application.

## 2. Parser contract extension

**Decision**: Extend `ICsvImportParser` with (a) `PeekHeadersAsync` returning raw header labels after delimiter detection, and (b) `ParseAsync` accepting a required `CsvColumnMapping` (confirmed assignments) plus existing `ignoreExtraColumns`. Legacy “exact heading only” behaviour becomes the outcome when the confirmed mapping maps canonical display names 1:1.

**Rationale**: Issue #127 requires blocking preview until Task Name is mapped; the coordinator must validate mapping before row parse. Delimiter/BOM (#133) already runs inside the parser before headers are read — mapping consumes those headers without re-sniffing.

**Alternatives considered**:

- **Pre-process CSV to rewrite header row in Web**: Rejected — fragile for quoted headers and semicolon files; duplicates delimiter logic.
- **Separate `ICsvHeaderReader` type**: Acceptable later; for v1 a second method on the existing parser interface keeps DI registration count low.

## 3. Canonical field catalog shape

**Decision**: One maintainable catalog in Application (static data file or single registry class) listing for each field: stable `FieldId`, user-facing label (matches validation error field names where applicable), canonical header strings, alias strings, `Required` flag, and `Enabled` flag (for future toggles). Due Date and Assigned To are included when those columns are already accepted by the parser.

**Rationale**: FR-004, FR-005, and issue acceptance criteria require one extensible list without scattered `if` chains.

**Alternatives considered**:

- **JSON in Web wwwroot**: Rejected — policy would be editable without tests and harder for Excel reuse from server-side paths.
- **Database-backed aliases**: Rejected for v1 scope and self-hosted simplicity.

## 4. Header normalisation and layout identity

**Decision**: Normalise headers for matching and layout keys by: trim, case-fold (invariant), remove spaces and common punctuation (`-`, `_`, `.`). Layout signature = ordered list of normalised header tokens from the file’s first row, joined with a stable delimiter (for example `|`), then hashed or used as a dictionary key string. File name is not part of the key.

**Rationale**: Matches spec FR-006 and user story acceptance for `TaskName` vs `Task Name`. Order is preserved so two files with the same set but different column order remain distinct layouts (issue: “order, if needed to distinguish files”).

**Alternatives considered**:

- **Sorted-set signature (order-insensitive)**: Rejected — different tools export different column order; users may rely on remembered mapping per export shape.

## 5. Conflict and ambiguity rules

**Decision**:

1. Exact canonical match (after normalisation) wins over alias for that header.
2. Each source column maps to at most one canonical field.
3. If two source columns map to the same canonical field via aliases, status = **Conflict** until the user picks one source per field.
4. Task Name is the only required canonical field for mapping completeness.
5. No alias entries for Planner-export collision cases (for example **Status** → Bucket).

**Rationale**: FR-007–FR-009 and issue “do not invent aggressive aliases”.

## 6. Remembered mappings (v1 persistence)

**Decision**: Browser `localStorage` via a Web adapter (`IImportColumnMappingLayoutStore`) keyed by layout signature. Stored payload: layout signature, map of source header text (as appeared in file) to `FieldId` or “skip”, and last-updated timestamp. No row data.

**Rationale**: Spec assumption and issue #127 explicitly allow browser storage for v1; works for self-hosted and hosted without account storage (#45).

**Alternatives considered**:

- **Server-side per-user store**: Deferred — depends on commercial account features; not blocking v1.
- **Session-only memory**: Rejected — fails repeat-import value proposition.

**Privacy**: User-facing copy on first save should note mappings stay on this browser only.

## 7. Sample CSV delivery

**Decision**: Ship `import-minimal.csv` and `import-full.csv` under `wwwroot/samples/`. Upload step offers MudBlazor download buttons (same URLs as docs links). Full sample includes every column the parser accepts at release time with valid example values aligned to `docs/csv-format.md`.

**Rationale**: FR-001, FR-002, SC-005; static files are testable and identical in hosted/self-hosted.

**Alternatives considered**:

- **Docs-only samples**: Rejected by spec — in-app download is mandatory.
- **Dynamic generation endpoint**: Rejected — unnecessary moving parts for two fixed files.

## 8. Mapping UI placement

**Decision**: Upload step (wizard step 3) hosts sample downloads, mapping summary, and mapping editor (dialog or expandable panel). Preview step stays blocked until `ColumnMappingConfirmed` and Task Name mapped. Auto-complete mappings still show compact summary with “Change mapping”.

**Rationale**: FR-010, FR-011; keeps mapping adjacent to file selection.

**Alternatives considered**:

- **Dedicated wizard step**: Rejected for v1 — increases stepper churn when most canonical files need no interaction beyond summary.

## 9. Interaction with `ignoreExtraColumns`

**Decision**: Unmapped source columns behave as today: ignored when `ignoreExtraColumns` is true; otherwise file-level “Unexpected column” for headers not assigned to any canonical field and not explicitly left unmapped in the mapping model.

**Rationale**: FR-015 aligns with existing switch on Home.

## 10. Testing strategy

**Decision**: Application unit tests for normalisation, alias proposal, conflict detection, layout signatures, and remembered-mapping merge rules; Infrastructure `CsvImportParserTests` for parse-with-mapping (alias headers → correct `CsvTaskRow`); optional bUnit smoke for mapping summary visibility if existing Web test patterns allow.

**Rationale**: Constitution VI and IX; issue requires unit/integration coverage for aliases, required-field failure, layout reuse, and sample validity.

**Alternatives considered**:

- **Playwright-only**: Rejected by engineering policy for this increment unless explicitly requested later.
