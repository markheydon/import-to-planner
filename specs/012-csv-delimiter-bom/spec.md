# Feature Specification: Detect CSV Delimiter and UTF-8 BOM

**Feature Branch**: `012-csv-delimiter-bom`

**Created**: 2026-09-09

**Status**: Draft

**Input**: User description: "[Story] Detect CSV delimiter and UTF-8 BOM (https://github.com/markheydon/import-to-planner/issues/133). Detect comma vs semicolon CSV delimiters and tolerate a UTF-8 BOM so files saved from Excel (especially UK/EU locales) parse on first upload. After reading the header line (skipping BOM), detect comma vs semicolon when the choice is unambiguous. If both appear and detection is ambiguous, fail at file level with a clear message. Preserve quoted fields. Keep the 10 MB limit and existing ignore-extra-columns behaviour. Public CSV / troubleshooting docs must describe locale Excel exports and BOM. Sample downloads remain comma-separated UTF-8 without a BOM. Not Excel workbook (.xlsx) support. Detection happens before any later heading alias or mapping work."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upload a semicolon CSV from Excel (Priority: P1)

As a user who saved a spreadsheet as CSV in a locale that uses semicolon as the list separator, I can upload that file and see a normal preview of my tasks, instead of being told the required heading is missing because the whole header row was treated as one column.

**Why this priority**: This is the primary failure that blocks first-run Excel users in UK and European locales. Fixing it delivers the story’s core value on its own.

**Independent Test**: Upload a valid semicolon-delimited CSV whose first row uses the accepted headings and confirm that preview treats columns the same as an equivalent comma-delimited file.

**Acceptance Scenarios**:

1. **Given** a CSV whose header and values are separated by semicolons and whose first row includes the required heading, **When** the user uploads the file for preview, **Then** the app recognises the columns and produces the same preview outcome as an equivalent comma-delimited file.
2. **Given** a semicolon-delimited CSV that is otherwise valid, **When** the user uploads it, **Then** they are not shown a missing required-heading error caused solely by the delimiter.
3. **Given** two files with the same tasks that differ only by comma versus semicolon separators, **When** both are uploaded, **Then** both produce equivalent column interpretation and preview content.

---

### User Story 2 - Upload a UTF-8 CSV that starts with a BOM (Priority: P1)

As a user who saved “CSV UTF-8” from Excel, I can upload the file and have the first heading recognised, even though the file starts with a byte-order mark.

**Why this priority**: Excel’s UTF-8 CSV export commonly prepends a BOM. If the first heading is poisoned, the required column appears missing even when the rest of the file is correct. This is independently valuable and often occurs together with semicolon export.

**Independent Test**: Upload a valid comma-delimited CSV whose first character is a UTF-8 BOM and confirm the required heading is recognised and preview proceeds.

**Acceptance Scenarios**:

1. **Given** a valid comma-delimited CSV that begins with a UTF-8 BOM, **When** the user uploads it for preview, **Then** the first heading matches the expected name and is not treated as an unknown or missing column.
2. **Given** a valid semicolon-delimited CSV that begins with a UTF-8 BOM, **When** the user uploads it, **Then** both the BOM and the semicolon separator are handled so that columns are recognised as they would be without the BOM.
3. **Given** a valid CSV with no BOM, **When** the user uploads it, **Then** existing successful parse behaviour is unchanged.

---

### User Story 3 - Understand why a file cannot be read (Priority: P2)

As a user whose CSV uses an unclear or unsupported separator, I receive a file-level explanation before preview, so I know to save as comma-separated UTF-8 rather than hunting for a missing column that is not actually missing.

**Why this priority**: Ambiguous files must not look like ordinary validation failures. Clear file-level messaging unblocks users without adding a delimiter picker in this increment.

**Independent Test**: Upload files where both comma and semicolon appear in the header in an ambiguous way, and files that use an unsupported separator, and confirm a file-level error appears before preview rather than a missing-column message.

**Acceptance Scenarios**:

1. **Given** a CSV header in which comma versus semicolon cannot be chosen unambiguously, **When** the user uploads the file, **Then** the app rejects the file as a whole with a message that tells them the separator could not be determined and that they should save as comma-separated UTF-8.
2. **Given** a CSV that uses an unsupported separator (for example tab-separated values presented as CSV), **When** the user uploads the file, **Then** the app rejects the file as a whole with a clear unsupported-separator message before any preview of rows.
3. **Given** such a file-level failure, **When** the user reads the message, **Then** they are not told that the required heading is missing unless that heading is genuinely absent after a successful delimiter choice.

---

### User Story 4 - Keep quoted descriptions intact (Priority: P2)

As a user whose task descriptions contain commas or semicolons, I can still import those rows because quoted fields are treated as a single value, not extra columns.

**Why this priority**: Delimiter detection must not break existing files that put punctuation inside quoted text. This can be verified independently of Excel-locale files.

**Independent Test**: Upload comma-delimited and semicolon-delimited files that include quoted field values containing the other separator and confirm those values appear as a single description (or other field) in preview.

**Acceptance Scenarios**:

1. **Given** a comma-delimited CSV with a quoted description that contains commas, **When** the user uploads it, **Then** that description remains one field in preview.
2. **Given** a semicolon-delimited CSV with a quoted description that contains semicolons, **When** the user uploads it, **Then** that description remains one field in preview.
3. **Given** a quoted field that contains the other recognised separator (comma inside a semicolon file, or semicolon inside a comma file), **When** the user uploads it, **Then** delimiter detection still succeeds when the header makes the choice unambiguous, and the quoted value is not split.

---

### User Story 5 - Learn how Excel locale exports should look (Priority: P3)

As a user preparing a file, I can read public CSV and troubleshooting guidance that explains Excel locale separators, UTF-8 BOM, and that sample downloads stay comma-separated UTF-8 without a BOM.

**Why this priority**: Detection removes most first-run failures, but documentation still needs to match real Excel behaviour so remaining issues are diagnosable.

**Independent Test**: Review the public CSV format and troubleshooting pages and confirm they describe locale Excel CSV export, BOM, supported separators, and sample-file encoding.

**Acceptance Scenarios**:

1. **Given** a user opens public CSV format guidance, **When** they look for Excel export advice, **Then** they can see that UK/EU Excel CSV often uses semicolons and may start with a UTF-8 BOM, and that the app accepts both comma and semicolon when detection is unambiguous.
2. **Given** a user opens troubleshooting guidance that previously mentioned a wrong delimiter, **When** they read it, **Then** it no longer implies the app only ever accepts commas, and it explains what to do if the app cannot detect the separator.
3. **Given** a user downloads a sample CSV from the app or docs, **When** they inspect it, **Then** it is comma-separated UTF-8 without a BOM.

---

### Edge Cases

- Header line contains both commas and semicolons inside and outside quotes, making comma versus semicolon ambiguous.
- File uses only one recognised separator in the header, while later rows contain the other character inside quoted fields.
- File has a UTF-8 BOM plus spaces around headings.
- File is empty, headers-only, or exceeds the existing 10 MB upload limit (limit and extra-column behaviour stay as today).
- File is a genuine Excel workbook rather than CSV (out of scope; users should still receive the existing non-CSV rejection, not delimiter detection).
- Sample download files must remain comma-separated UTF-8 without a BOM even after the app starts tolerating BOM on upload.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: After the user uploads a CSV, the app MUST ignore a leading UTF-8 BOM when reading the header row so the first heading is compared as the user wrote it.
- **FR-002**: The app MUST recognise comma and semicolon as CSV separators and MUST choose between them from the header row when that choice is unambiguous.
- **FR-003**: When comma versus semicolon cannot be determined unambiguously from the header row, the app MUST fail the file as a whole before preview, with a message that the separator could not be determined and that the user should save as comma-separated UTF-8.
- **FR-004**: When the file uses a separator other than comma or semicolon, the app MUST fail the file as a whole before preview with a message that the separator is not supported, not a missing-column error caused by treating the header as a single field.
- **FR-005**: Quoted fields MUST remain a single value even when they contain commas, semicolons, or the chosen separator.
- **FR-006**: A valid semicolon-delimited CSV MUST produce the same column mapping and preview outcomes as the equivalent comma-delimited CSV (same headings and values, different separator).
- **FR-007**: Existing upload limits MUST remain: files larger than 10 MB are rejected before this detection. Extra unknown columns MUST continue to follow the existing ignore-extra-columns behaviour.
- **FR-008**: Delimiter and BOM handling MUST occur before any later heading alias or column-mapping behaviour, so mapping sees already-split, BOM-free headings.
- **FR-009**: Sample CSV files offered by the product MUST remain comma-separated UTF-8 without a BOM.
- **FR-010**: Public CSV format and troubleshooting documentation MUST describe Excel locale semicolon exports, UTF-8 BOM on Excel UTF-8 CSV, supported separators, and what to do when detection fails.
- **FR-011**: This feature MUST NOT add Excel workbook (.xlsx) import. Workbook files remain out of scope.
- **FR-012**: This increment MUST NOT require the user to pick a delimiter in the interface. Detection is automatic; only an explicit file-level error is shown when detection cannot succeed.

### Key Entities

- **Uploaded CSV file**: A text import file with a header row and zero or more data rows. May begin with a UTF-8 BOM. Uses either comma or semicolon as the field separator when accepted.
- **Header row**: The first data-bearing line after any BOM. Used both to detect the separator and to identify required and optional columns.
- **Field separator**: Either comma or semicolon for this increment. Chosen automatically when unambiguous; otherwise the file is rejected.
- **Quoted field**: A column value wrapped so that separator characters inside it are part of the value, not column breaks.
- **File-level parse error**: A whole-file rejection shown before preview, distinct from row-level validation (missing task name on a row, invalid priority, and similar).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can upload a valid semicolon-delimited CSV (typical Excel locale export) and reach a usable preview on the first attempt, without renaming columns or re-saving solely to switch separator.
- **SC-002**: Users can upload a valid CSV that starts with a UTF-8 BOM and reach a usable preview on the first attempt, with the required heading recognised.
- **SC-003**: In 100% of cases where the separator cannot be determined or is unsupported, users see a file-level explanation before preview and do not receive a false “required heading missing” result caused by delimiter mishandling.
- **SC-004**: Quoted descriptions that contain commas or semicolons remain a single field in preview for both recognised separators.
- **SC-005**: A reader of public CSV and troubleshooting pages can identify Excel locale separator and BOM behaviour in under two minutes without contacting support.
- **SC-006**: Sample files remain usable in tools that expect comma-separated UTF-8 without a BOM.

## Assumptions

- Source of this work is GitHub issue [#133](https://github.com/markheydon/import-to-planner/issues/133) (story, high priority, targeted at v1.0). Related later work: heading mapping ([#127](https://github.com/markheydon/import-to-planner/issues/127)); Excel workbooks remain [#55](https://github.com/markheydon/import-to-planner/issues/55). Description, due date, and assignee stories are separate.
- Only comma and semicolon are in scope. Tab, pipe, and other separators are unsupported and must fail at file level.
- “Unambiguous” means the header row clearly indicates one of the two supported separators after accounting for quoted text. Mixed unquoted commas and semicolons in the header are treated as ambiguous.
- Detection-only for v1.0: no in-app delimiter selector in this increment, as the issue allows provided the error is explicit.
- Encoding remains UTF-8. This story covers BOM tolerance, not additional encodings (for example Windows-1252) unless they happen to be valid UTF-8.
- Existing required heading (`Task Name`), optional columns, 10 MB limit, and ignore-extra-columns defaults are unchanged.
- Public documentation updates are in scope; in-app copy may mention the file-level error but does not need a new dedicated settings screen.
