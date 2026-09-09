# Feature Specification: Import Due Dates From CSV

**Feature Branch**: `013-import-due-dates`

**Created**: 2026-09-09

**Status**: Draft

**Input**: User description: "[Story] Import due dates from CSV (https://github.com/markheydon/import-to-planner/issues/131). Accept an optional Due Date column on CSV import and set that due date on newly created Planner tasks. Parse ISO dates (yyyy-MM-dd) and common UK Excel date formats; invalid non-empty values fail that row in validation (same pattern as invalid Priority). Empty Due Date is allowed. Apply the date only when creating a task; do not change due dates on name-matched existing tasks (skip / already exists stays skip-only). Heading aliases (for example Due date, DueDate, Deadline) are later work and must not block this story. Sample CSVs and public CSV docs include Due Date on the full example only. Start date, labels, checklists, and progress stay out of scope. Do not wait on Excel workbook import."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Preview optional due dates from CSV (Priority: P1)

As an operator preparing a typical spreadsheet of work, I can include an optional Due Date column, upload the file, and see each row’s due date in the preview before anything is created.

**Why this priority**: Preview is the safety gate for the rest of the import. Operators must see how dates were interpreted (or that a row was rejected) before they confirm.

**Independent Test**: Upload valid files with and without a Due Date column, plus a file with an invalid date on one row, and confirm preview shows recognised dates and blocks execution while that row error remains.

**Acceptance Scenarios**:

1. **Given** a CSV whose first row includes the accepted Due Date heading and whose rows contain valid dates, **When** the operator requests preview, **Then** each planned new task shows the due date from that row.
2. **Given** a CSV with no Due Date column, **When** the operator requests preview, **Then** preview succeeds as it does today and no due date is required.
3. **Given** a CSV with a Due Date column where some cells are empty, **When** the operator requests preview, **Then** those rows remain valid and are shown without a due date.
4. **Given** a CSV row with a non-empty Due Date that cannot be interpreted as a date, **When** the operator requests preview, **Then** that row receives a row-level validation error, execution is blocked until the value is corrected, and other rows are still validated.

---

### User Story 2 - Create tasks with the CSV due date (Priority: P1)

As an operator confirming a valid preview, I want newly created Planner tasks to carry the due date from the CSV so I do not have to edit every card afterwards.

**Why this priority**: This is the outcome that makes a title-and-dates spreadsheet feel complete. It can be demonstrated independently of documentation.

**Independent Test**: Confirm a preview that includes valid due dates and inspect the created tasks to confirm each new task shows the same calendar date as the CSV row.

**Acceptance Scenarios**:

1. **Given** a validated preview in which a row will create a new task and has a valid due date, **When** the operator confirms execution, **Then** the created task shows that calendar date as its due date.
2. **Given** a validated preview in which a row will create a new task and the Due Date cell is empty or the column is absent, **When** the operator confirms execution, **Then** the created task has no due date from this import.
3. **Given** a preview that still has an invalid due date on a row, **When** the operator attempts execution, **Then** execution remains blocked until that row is corrected.

---

### User Story 3 - Leave existing tasks unchanged (Priority: P2)

As an operator re-importing a file that includes tasks already in the plan, I need name-matched existing tasks to stay skipped. A due date in the CSV must not update those cards.

**Why this priority**: Existing skip / already-exists behaviour must remain skip-only. Changing dates on live cards would be a silent overwrite and is explicitly out of scope.

**Independent Test**: Preview and execute a file that mixes new titles with titles that already exist in the destination, including due dates on both, and confirm existing matches are reported as already exists with no due-date change.

**Acceptance Scenarios**:

1. **Given** a CSV row whose task name matches an existing task in the destination, **When** preview runs, **Then** the row is treated as already exists (skip), even if the CSV includes a due date.
2. **Given** such a skipped row, **When** the operator confirms execution, **Then** the existing task’s due date is not created, replaced, or cleared from the CSV value.
3. **Given** a mix of new and existing names in one file, **When** execution completes, **Then** only newly created tasks receive CSV due dates; skipped matches remain unchanged.

---

### User Story 4 - Document due dates on the full sample only (Priority: P3)

As an operator learning the file format, I can see Due Date on the full-featured sample and in public CSV guidance, while the minimal sample still shows only the required heading.

**Why this priority**: Documentation and samples prevent operators from treating due date as required, and they keep the simple first example small.

**Independent Test**: Review public CSV format guidance, any in-app accepted-field cues, and the minimal versus full sample files, and confirm Due Date appears only on the full example and in the accepted-columns list.

**Acceptance Scenarios**:

1. **Given** public CSV format guidance, **When** an operator reads required and accepted columns, **Then** Due Date is listed as optional, with allowed date shapes and the same “invalid value fails the row” rule as Priority.
2. **Given** the full-featured sample CSV (in docs and any in-app download), **When** an operator inspects it, **Then** it includes a Due Date column with at least one valid example date.
3. **Given** the minimal sample CSV, **When** an operator inspects it, **Then** it still contains only the required Task Name heading and does not include Due Date.
4. **Given** in-app guidance that names accepted columns, **When** an operator reads it, **Then** Due Date is included as an optional accepted column.

---

### Edge Cases

- Due Date cell contains only spaces: treat as empty (row remains valid, no due date).
- Date is a valid ISO calendar date (`yyyy-MM-dd`).
- Date is a common UK spreadsheet date such as `dd/MM/yyyy`, `d/M/yyyy`, `dd/MM/yy`, or the same shapes with hyphens instead of slashes.
- Two-digit years are interpreted as a year in the current century in a way that is documented for operators (see Assumptions).
- Ambiguous numeric dates such as `05/06/2026` are read day-first (UK), not month-first.
- Values with a time of day, unrecognised text, or numbers that are not a recognised date fail that row (same class of error as an invalid Priority).
- Quoted due date values still parse if the inner text is a recognised date.
- Semicolon-delimited files with a Due Date column behave the same as equivalent comma-delimited files.
- Extra unknown columns continue to follow the existing ignore-extra-columns behaviour; Due Date is an accepted column, not an extra column.
- Excel workbooks (`.xlsx`) remain out of scope; this story applies to CSV only.
- Heading variants such as `Due date`, `DueDate`, or `Deadline` are not accepted in this increment unless they exactly match the canonical heading.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST accept an optional canonical CSV heading `Due Date` in addition to the existing accepted columns.
- **FR-002**: Absence of the Due Date column MUST NOT cause validation failure.
- **FR-003**: An empty or whitespace-only Due Date cell MUST be treated as omitted: the row MAY still be imported, and no due date is applied for that row.
- **FR-004**: A non-empty Due Date MUST be parsed as a calendar date. Accepted shapes are ISO `yyyy-MM-dd` and common UK spreadsheet dates: day/month/year with two- or four-digit years, with slash or hyphen separators, and with or without leading zeros on day and month.
- **FR-005**: A non-empty Due Date that does not match an accepted shape MUST produce a row-level validation error on that field, and MUST block execution until the row is corrected (same pattern as an invalid Priority).
- **FR-006**: Preview MUST show the interpreted due date for each valid row that has one, so the operator can confirm the calendar date before execution.
- **FR-007**: When creating a new task from a valid row that has a due date, the app MUST set that task’s due date so the calendar date shown in Planner matches the CSV date.
- **FR-008**: When a CSV row is matched to an existing Planner task by name, the app MUST skip that row as already exists and MUST NOT create, replace, or clear that task’s due date from the CSV.
- **FR-009**: This increment MUST NOT import start dates, labels, checklists, or progress.
- **FR-010**: This increment MUST NOT add heading aliases or a column-mapping interface. Only the exact heading `Due Date` is accepted here; alias work remains a separate story.
- **FR-011**: Public CSV format documentation MUST list Due Date as optional, describe accepted date shapes (including UK day-first dates), and state that invalid non-empty values fail the row.
- **FR-012**: The full-featured sample CSV (documentation example and any in-app full sample) MUST include a Due Date column. The minimal sample MUST remain Task Name only.
- **FR-013**: In-app accepted-column guidance MUST include Due Date as optional.
- **FR-014**: Due Date handling MUST work for both comma-delimited and semicolon-delimited CSV once the file is successfully read, and MUST preserve existing upload size limits and ignore-extra-columns behaviour.
- **FR-015**: Changed parse, preview, skip, and create-with-date behaviour MUST be covered by automated checks for valid dates, invalid dates, omitted dates, and name-matched existing tasks that must not be updated.

### Key Entities

- **CSV task row**: One import row, now optionally carrying a due date alongside existing fields (task name, description, priority, bucket, goal).
- **Due date value**: An optional calendar date on a row. Empty means “no date”. Invalid non-empty text is a validation failure, not a skipped date.
- **Preview plan item**: The dry-run view of what will happen for a row, including the due date that will be applied if the row creates a new task.
- **Existing-task skip**: A name match against a task already in the destination. Outcome remains already exists; CSV due date is ignored for writes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can import a valid CSV that includes due dates and have 100% of newly created tasks show the same calendar date as the corresponding CSV row, without editing those cards afterwards.
- **SC-002**: Files without a Due Date column, and rows with a blank Due Date, continue to preview and import successfully; due date remains optional.
- **SC-003**: 100% of rows with a non-empty unrecognised due date are rejected at preview with a row-level error, and execution cannot start until those rows are corrected.
- **SC-004**: 100% of name-matched existing tasks remain skipped, with no due date change, when the CSV includes dates for those rows.
- **SC-005**: A first-time operator can confirm from public CSV guidance and the full sample, in under two minutes, that Due Date is optional, which heading to use, and which date shapes are accepted.
- **SC-006**: The minimal sample still demonstrates a valid file with only Task Name; operators are not forced to supply dates to try the product.

## Assumptions

- Source of this work is GitHub issue [#131](https://github.com/markheydon/import-to-planner/issues/131) (story, high priority, targeted at v1.0). Related but separate: description persistence ([#130](https://github.com/markheydon/import-to-planner/issues/130)), heading aliases ([#127](https://github.com/markheydon/import-to-planner/issues/127)), assignees ([#132](https://github.com/markheydon/import-to-planner/issues/132)), delimiter/BOM ([#133](https://github.com/markheydon/import-to-planner/issues/133)). Excel workbooks remain [#55](https://github.com/markheydon/import-to-planner/issues/55) and MUST NOT block this story.
- Canonical heading is exactly `Due Date` (capital D on both words), matching the accepted-column style of `Task Name`. Planner export’s `Due date` casing is deferred to alias work.
- UK day-first reading is the only numeric date convention in this increment (no US month-first).
- Two-digit years such as `31/05/26` are interpreted as 2026 (years 00–99 map to 2000–2099). This is documented for operators.
- Date-only values are in scope. Times of day, time zones, and recurrence are not imported; a value that includes a time is treated as invalid.
- Excel serial numbers (plain integers representing spreadsheet day counts) are not accepted; they fail the row like other unrecognised values.
- Existing match-by-task-name-only and skip-already-exists rules from the core import workflow remain unchanged except that skipped rows MUST NOT receive a due-date write.
- Due dates apply in both in-memory and live destination modes in the same way from the operator’s point of view: created tasks show the date; skipped tasks do not change.
- Public documentation updates and in-app accepted-column cues are in scope; no new wizard step is required solely for due dates.
