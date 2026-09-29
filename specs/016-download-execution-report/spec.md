# Feature Specification: Download Execution Report as CSV

**Feature Branch**: `016-download-execution-report`

**Created**: 2026-09-29

**Status**: Draft

**Input**: User description: "[Story] Download execution report as CSV (https://github.com/markheydon/import-to-planner/issues/135). After import execution, let the operator download the report as CSV (created, reused/skipped, failed, and manual follow-up rows, with row number and reason). Operators need an artefact they can keep, share, or use to correct the source file. On-screen report is not enough once they leave the wizard. Re-upload already skips existing names, so downloading failures is the practical retry path for v1.0 (no separate retry-failed-only control). Single CSV with an Outcome column preferred. UK English headings; no tenant secrets. Do not persist the CSV on the server beyond the current session/report boundary."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Download the report after import (Priority: P1)

As an operator who has finished an import, I can download the execution report as a CSV file from the report step, so I can keep or share a record after I leave the wizard.

**Why this priority**: This is the core deliverable of the story. Without a download action, operators cannot take the report offline or attach it to project communication.

**Independent Test**: Complete any import that produces a report, choose **Download report** (or equivalent UK English label) on the report step, and confirm a CSV file is saved to the device.

**Acceptance Scenarios**:

1. **Given** an import has finished and the report step is visible, **When** the operator chooses to download the report, **Then** the browser receives a CSV file containing the completed execution outcomes.
2. **Given** an import finished with partial success (some rows created or reused and some failed), **When** the operator downloads the report, **Then** the file includes both successful and failed row outcomes in one file.
3. **Given** an import finished with only failures, **When** the operator downloads the report, **Then** the download still succeeds and lists the failed rows with reasons.
4. **Given** the operator has not yet run an import in the current session, **When** they are not on a completed report, **Then** no report download is offered (or it is disabled with a clear reason).

---

### User Story 2 - Reconcile the file with the source CSV (Priority: P1)

As an operator correcting a source spreadsheet, I can use the downloaded CSV to see which source row failed, what the task was called, and why, so I can fix the file and re-upload.

**Why this priority**: The download is only useful if it is reconcilable with the uploaded CSV. Row numbers and plain-language outcomes are the minimum for retry-by-re-upload.

**Independent Test**: Run a mixed-outcome import, download the report, and verify each row in the file can be matched to a source row or manual follow-up item shown on screen.

**Acceptance Scenarios**:

1. **Given** a completed execution report, **When** the operator opens the downloaded CSV, **Then** each automated row outcome includes the source row number and task name from the import.
2. **Given** a row was created in Planner, **When** the operator reads that row in the CSV, **Then** the outcome indicates creation and includes a stable task identifier when one was returned by the import (empty when not applicable).
3. **Given** a row was reused or skipped because a matching task already existed, **When** the operator reads that row in the CSV, **Then** the outcome and reason make clear that no new task was created.
4. **Given** a row failed during execution, **When** the operator reads that row in the CSV, **Then** the file includes an error or reason column sufficient to understand the failure without opening developer logs.
5. **Given** the report lists manual follow-up actions (for example goal linking or assignee follow-up), **When** the operator downloads the report, **Then** those items appear in the same CSV with an outcome or row type that distinguishes them from per-source-row automated outcomes, and includes action type and details aligned with the on-screen report.

---

### User Story 3 - Learn that a downloadable report exists (Priority: P2)

As an operator reading public help, I can find that the final report can be downloaded as CSV and what it is for, so I know to use it before closing the browser.

**Why this priority**: Discovery in the import workflow guide and FAQ prevents the feature from being hidden on the report step alone.

**Independent Test**: Review public import workflow and FAQ content and confirm they mention CSV download of the execution report and typical uses (keeping a record, sharing, fixing the source file).

**Acceptance Scenarios**:

1. **Given** a user reads the import workflow guide, **When** they reach the section about the final report, **Then** they can see that the report can be downloaded as CSV after import completes.
2. **Given** a user reads the FAQ, **When** they look for how to retry or share import results, **Then** they can find guidance that points to downloading the report and correcting the source file for re-upload (not a separate in-app retry-only control in v1.0).

---

### Edge Cases

- Import completes with zero rows created and zero failures (for example all rows reused): download still reflects every source row outcome.
- Import produces many manual follow-up rows alongside automated outcomes: the CSV remains readable in a single file with a clear distinction between source-row outcomes and manual follow-up rows.
- Report contains no created tasks but lists errors only: download is still available and truthful.
- Operator refreshes or navigates away after import: report data follows the same session and storage boundary as today’s on-screen report (no new long-lived server copy of the CSV).
- Sensitive values must not appear in the file: no access tokens, secrets, or tenant identifiers that are not already shown safely on the on-screen report.
- CSV field values that contain commas or quotes are encoded so the file opens correctly in Excel and other spreadsheet tools.
- Filename is human-readable and safe for common operating systems (no path separators or ambiguous characters).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: On the report step, after import execution has completed, the product MUST offer a clear **Download report** action (UK English labelling) that saves the execution report as a CSV file.
- **FR-002**: The downloaded file MUST include one row per source-row execution outcome (created, reused or skipped, and failed) for the completed import.
- **FR-003**: The downloaded file MUST include manual follow-up items from the same execution report in the same CSV file, with columns or outcome values that distinguish manual follow-up from automated per-row outcomes.
- **FR-004**: The CSV MUST use UK English column headings.
- **FR-005**: Each source-row outcome row MUST include at minimum: source row number, task name, outcome, and error or reason when the outcome is failed or skipped for a documented reason.
- **FR-006**: When a task was created and a stable Planner task identifier is available in the execution report, the CSV MUST include it on that row; when not applicable, the identifier column MUST be empty rather than omitted from the header set.
- **FR-007**: Manual follow-up rows MUST include action type and details consistent with the on-screen manual actions list (including goal and task names where the on-screen report shows them).
- **FR-008**: The download MUST work for partial success, full success, and full failure executions, as long as a report was produced.
- **FR-009**: The product MUST NOT store the generated CSV on the server beyond the same session and report boundary used for the on-screen execution report today.
- **FR-010**: The CSV MUST NOT contain secrets, access tokens, or tenant-sensitive values beyond what is already acceptable on the user-facing execution report.
- **FR-011**: Public import workflow documentation and FAQ MUST mention that operators can download the execution report as CSV and briefly state why (record-keeping, sharing, correcting the source file for re-upload).
- **FR-012**: This increment MUST NOT add a separate in-app **retry failed rows only** control; correcting failures via download, edit source CSV, and re-upload remains the v1.0 path.
- **FR-013**: This increment MUST NOT add PDF or other export formats; CSV only.

### Key Entities *(include if feature involves data)*

- **Execution report download**: A user-triggered export of the completed import execution report for the current session, derived from the same data that powers the report step UI.
- **Source-row outcome row**: One CSV data row representing the final result for a single row from the uploaded source file, including row index, task name, outcome category, optional created task identifier, and failure or skip reason when present.
- **Manual follow-up row**: One CSV data row representing a post-import action the operator must perform outside full automation, with action type and descriptive fields aligned to the on-screen manual actions list.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a moderated test, at least 90% of operators who complete an import can successfully download the report CSV on the first attempt from the report step.
- **SC-002**: For a mixed-outcome test import (at least one created, one reused or skipped, one failed, and at least one manual follow-up item), the downloaded CSV contains a matching row for every outcome category shown on screen.
- **SC-003**: Operators can identify the source row number and failure reason for every failed row in the download without using browser developer tools.
- **SC-004**: Public import workflow and FAQ each contain at least one sentence describing CSV download of the execution report and its purpose.
- **SC-005**: Automated checks verify that a representative mixed-outcome report produces a CSV with expected column headings (UK English), expected filename pattern, and row content aligned to the on-screen report.

## Assumptions

- The on-screen execution report already exposes created, reused or skipped, failed, and manual follow-up data for the current session; this feature exports that view rather than defining new business rules for outcomes.
- Re-upload continues to skip tasks that already exist by name; operators use the downloaded failure and skip reasons to fix the source file before re-uploading.
- Column heading names and outcome labels will align with existing user-facing wording on the report step and public docs (UK English), without introducing a second vocabulary for the same states.
- The commercial credit ledger (#126) remains separate; this CSV is an operator artefact, not a billing or usage ledger export.
- Excel workbook import (#55) and delimiter or mapping features do not change the obligation to export whatever report the app already produced for that execution.

## Out of Scope

- In-app retry of failed rows only without re-uploading a corrected CSV.
- PDF or printable report export.
- Long-term server-side storage or history of past import report downloads.
- Export of preview or validation results before execution (execution report only).
