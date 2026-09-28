# Feature Specification: CSV Column Mapping and Sample Files

**Feature Branch**: `015-csv-column-mapping`

**Created**: 2026-09-28

**Status**: Draft

**Input**: User description: "GitHub issue #127 — CSV sample files, column aliases, and remembered mapping. Help first-time and repeat importers succeed without already knowing exact CSV headings: offer downloadable sample files, map common column aliases onto required fields, and remember a user’s mapping when the same header layout appears again. CSV only; Excel/Planner export (#55) must reuse this layer later. Delimiter/BOM sniffing (#133) is separate. Not the personal template library (#45). Canonical fields today: Task Name (required), Description, Priority, Bucket, Goal; Due Date (#131) and Assigned To (#132) must plug into the same mapper when those features ship. Block preview until Task Name is mapped. Do not persist CSV row contents in mapping memory."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start from a valid example file (Priority: P1)

As a first-time importer, I can download a minimal or full example CSV from the upload step, fill it in, and import it without renaming columns or opening external documentation first.

**Why this priority**: Without a trustworthy starting file, new users guess headings and abandon the flow before they experience a successful import.

**Independent Test**: Download each offered example from the upload step, upload unchanged or with only row edits, and reach preview with Task Name resolved and no manual column mapping required.

**Acceptance Scenarios**:

1. **Given** a user on the upload step, **When** they choose the minimal example download, **Then** they receive a comma-separated UTF-8 file whose header row contains only Task Name and whose example rows are safe, non-personal placeholder content.
2. **Given** a user on the upload step, **When** they choose the full example download, **Then** they receive a file whose headers match all columns the app accepts at release time (including Due Date and Assigned To when those import capabilities are available) with valid example values for each column.
3. **Given** a user uploads a downloaded example without changing headers, **When** they continue the import workflow, **Then** preview is available without an extra column-mapping step.

---

### User Story 2 - Import files that use familiar column names (Priority: P1)

As someone exporting from another tool, I can upload a CSV whose headers use common synonyms (for example Title or Subject instead of Task Name) and have the app recognise them automatically when the mapping is unambiguous.

**Why this priority**: Repeat importers should not rename columns on every upload when their export format is stable and predictable.

**Independent Test**: Upload CSVs whose headers use alias names only (no canonical headings) for all present fields and confirm preview shows the correct field values without manual mapping.

**Acceptance Scenarios**:

1. **Given** a CSV whose first row uses alias headings that uniquely match canonical fields (for example Title for task title, Notes for description), **When** the file is uploaded, **Then** each mapped column is applied using the same validation rules as the canonical heading would use.
2. **Given** a CSV whose Task Name column is labelled with an accepted alias, **When** the user continues toward preview, **Then** Task Name is treated as satisfied and preview is not blocked for a missing task title mapping.
3. **Given** a CSV with extra columns that are not mapped to any import field, **When** the file is processed, **Then** those columns are ignored and do not block preview or import, consistent with today’s behaviour for unrecognised columns.
4. **Given** header text that differs only by case, surrounding spaces, or spacing/punctuation (for example `TaskName` versus `Task Name`), **When** headers are compared, **Then** they are treated as the same source column for matching and layout identity.

---

### User Story 3 - Fix incomplete or conflicting mappings (Priority: P2)

As an importer whose file does not auto-map cleanly, I can assign each source column to a canonical field (or mark optional fields as not imported) and confirm before preview so I know how the file will be read.

**Why this priority**: Automatic aliases cannot cover every export; explicit mapping prevents silent misreads when Task Name is missing or two columns compete for one field.

**Independent Test**: Upload files that fail auto-mapping (missing Task Name, duplicate target field) and complete mapping in the UI until preview succeeds.

**Acceptance Scenarios**:

1. **Given** a CSV where no source column resolves to Task Name, **When** the user attempts to reach preview, **Then** preview and import remain blocked until the user maps a source column to Task Name.
2. **Given** a CSV where two source columns would map to the same canonical field, **When** the file is uploaded, **Then** the user is prompted to resolve the conflict before preview.
3. **Given** a mapping step is shown, **When** the user opens it, **Then** suggested assignments from exact matches and aliases are pre-filled and the user can change any assignment or set optional canonical fields to “do not import”.
4. **Given** the user confirms a mapping, **When** they continue, **Then** preview reflects values read through the confirmed mapping.

---

### User Story 4 - Reuse a confirmed mapping for the same layout (Priority: P2)

As a repeat importer using the same export format, I do not need to remap columns every time if I have already confirmed a mapping for that header layout.

**Why this priority**: Layout memory removes repetitive work for the most common happy path after the first successful import from a given tool.

**Independent Test**: Confirm a mapping for a specific header row, upload a second file with the same headers (different file name and row data), and verify the saved mapping applies without repeating the full mapping interaction.

**Acceptance Scenarios**:

1. **Given** a user has confirmed a column mapping for a file, **When** they later upload a different file whose normalised header layout matches, **Then** the saved mapping is applied automatically before preview.
2. **Given** a remembered mapping exists, **When** the user edits and confirms a new mapping for the same layout, **Then** the updated mapping replaces the previous one for that layout on subsequent uploads.
3. **Given** mapping memory is used, **When** data is stored, **Then** only header layout and mapping choices are persisted, not task row contents from the CSV.

---

### User Story 5 - See when mapping was automatic (Priority: P3)

As any importer, when every required field mapped uniquely without my input, I still see a brief summary of how columns were interpreted and can open mapping to change it if something looks wrong.

**Why this priority**: Transparency builds trust and avoids silent wrong mappings when aliases are almost but not quite correct.

**Independent Test**: Upload a fully auto-mapped file and verify a compact summary is visible with a path to adjust mappings.

**Acceptance Scenarios**:

1. **Given** all required fields are uniquely resolved by exact match or alias, **When** upload processing completes, **Then** the user sees a concise summary of source column to import field assignments and can open the mapping UI to change them before preview.

---

### Edge Cases

- What happens when the header row is empty or contains duplicate source column names after normalisation?
- How does the system handle a CSV with only headers and no data rows after mapping is confirmed?
- What if a remembered mapping references a canonical field that is no longer applicable in a future release—can the user still adjust or clear it?
- What if new columns appear in a later upload with the same otherwise-matching layout—aliases fill gaps, and any still-unmapped optional fields remain optional?
- How are ambiguous aliases avoided (for example not treating unrelated export columns such as Status as Bucket)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The upload step MUST offer download of a **minimal** example CSV (Task Name header only, plus safe example rows) and a **full** example CSV (all accepted import columns at release time, with safe example values).
- **FR-002**: Example files MUST be valid for import at the time they ship (comma-separated UTF-8, UK English placeholders, no real personal data) and MUST be updated when accepted columns change.
- **FR-003**: Public getting-started and CSV format documentation MUST reference the same examples and MUST NOT state that only exact canonical headings are supported.
- **FR-004**: After upload, the system MUST map each source header to at most one canonical import field using, in order: case-insensitive trimmed exact match to canonical names, then a single maintained alias list extensible without rewriting core import logic.
- **FR-005**: The canonical field set MUST be defined so optional future fields (Due Date, Assigned To, and others added for import) can register aliases and participate in the same mapping and memory model.
- **FR-006**: Header comparison for matching and layout identity MUST be case-insensitive and MUST treat spacing and punctuation insensitively so common export variants align.
- **FR-007**: The system MUST NOT apply alias rules that collide with Planner-export semantics (for example MUST NOT map Status to Bucket); Planner-specific handling remains with Excel/Planner export work.
- **FR-008**: If Task Name cannot be resolved to exactly one source column, the system MUST block preview and import until the user maps Task Name.
- **FR-009**: If two or more source columns compete for one canonical field, the system MUST require user resolution before preview.
- **FR-010**: The mapping experience MUST allow optional canonical fields to be set to “do not import” and MUST require explicit user confirmation when automatic mapping is **incomplete** or **ambiguous** (see **Confirmation policy** below).
- **FR-011**: When automatic mapping fully satisfies required fields without conflict, the system MAY skip the full mapping interaction but MUST still show a compact mapping summary with a way to edit assignments before preview.

#### Confirmation policy (FR-010 and FR-011)

For this feature, terms in FR-010 are interpreted as follows:

- **Incomplete** — Task Name is not assigned to exactly one source column (`NeedsTaskName`). Preview and import MUST remain blocked until the user maps Task Name and confirms in the mapping editor.
- **Ambiguous** — Two or more source columns compete for the same canonical field (`Conflict`). Preview and import MUST remain blocked until the user resolves the competition and confirms in the mapping editor.
- **Explicit user confirmation** — The user completes the mapping editor (including **Confirm mapping**) for `NeedsTaskName` and `Conflict` only. Optional fields MAY be set to “do not import” in that editor.
- **Not incomplete or ambiguous** — When every required field is uniquely assigned and there is no conflict, including when matches used **aliases** (`NeedsConfirmation`) or **canonical** headings (`Ready`). FR-011 applies: show the compact summary, allow **Change mapping**, and MAY proceed to preview without a separate confirm click. Alias-based mapping is transparent via the summary, not a blocking gate under FR-010.
- **FR-012**: When the user confirms a mapping, the system MUST remember it keyed by normalised header layout (not file name, not row data) and MUST apply it on subsequent uploads with the same layout, while still applying aliases for any new headers.
- **FR-013**: Users MUST be able to change a remembered mapping; the latest confirmed mapping for that layout MUST be used on the next upload.
- **FR-014**: Mapping memory MUST NOT store CSV row contents or task data from imported files.
- **FR-015**: Unmapped source columns MUST continue to be ignored unless the user explicitly maps them, preserving existing ignore-extra-columns behaviour.
- **FR-016**: Excel workbook import and Planner-export-specific behaviour MUST remain out of scope for this feature but MUST be documented as consumers of this mapping layer.
- **FR-017**: Delimiter detection and UTF-8 BOM handling MUST remain out of scope and MUST run before column mapping per the separate delimiter/BOM feature.

### Key Entities *(include if feature involves data)*

- **Canonical import field**: A named slot in the import model (for example task title, description, priority) that validation and preview use; one source column maps to at most one canonical field per import.
- **Column alias**: A normalised synonym that maps a source header label to a canonical field when exact canonical text is not present.
- **Header layout**: The ordered set of normalised header labels from the first row of a file; used to recognise the same export format across uploads.
- **Column mapping**: The user-confirmed assignment from each relevant source header to a canonical field or to “do not import”.
- **Example CSV**: A published minimal or full template file offered for download that demonstrates valid structure and values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of evaluators in a structured walkthrough can download an example file and reach preview without external documentation or manual header renaming.
- **SC-002**: Files using the issue’s starter alias set for all present columns (Title/Subject, Notes, and equivalent synonyms) reach preview without manual mapping in a standard test pack.
- **SC-003**: When Task Name is absent from auto-mapping, 100% of test attempts block preview until the user assigns Task Name (no false passes).
- **SC-004**: For a fixed header layout, after one confirmed mapping, a second upload with the same headers but different file name applies the saved mapping without repeating the full mapping workflow in at least 95% of repeat-import test scenarios.
- **SC-005**: Public CSV format content no longer implies exact headings as the only path; documentation and in-app samples stay aligned so both import without mapping.

## Assumptions

- Column mapping applies to CSV uploads only in this release; Excel (#55) will call the same mapping behaviour later without a parallel mapper.
- Delimiter and BOM handling (#133) completes before or independently of mapping so the first parsed row is the true header row.
- Due Date (#131) and Assigned To (#132) may land before or after this work; the full sample and alias list will include those fields when they are accepted import columns, and the mapper will accept their registration without a second mapping system.
- Mapping memory for v1 uses browser-local storage so self-hosted and local use work without server-side per-user storage; data stays on the user’s device and is not synced across browsers or devices unless a future account feature replaces this.
- Description may be mapped from aliases such as Notes even if writing description to Planner is still incomplete elsewhere (#130); mapping correctness is still required so preview and validation reflect the file.
- The personal upload template library (#45) remains a separate concern (reusing task content, not column names).

## Dependencies and Out of Scope

- **Depends on (conceptually)**: Stable definition of accepted CSV columns and validation rules; delimiter/BOM preprocessing (#133) ordering before mapping.
- **Enables**: Excel and Planner export import (#55) reusing canonical fields, aliases, UI, and layout memory.
- **Out of scope**: Excel parsing, Planner-export-specific column sets, saved task template libraries (#45), persisting CSV row data, and inventing aggressive aliases that mis-map Planner export columns.

## Traceability

- Source story: [GitHub issue #127](https://github.com/markheydon/import-to-planner/issues/127) (milestone v1.0).
