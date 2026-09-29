---

description: "Task list for Download Execution Report as CSV"
---

# Tasks: Download Execution Report as CSV

**Input**: Design documents from `/specs/016-download-execution-report/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/execution-report-csv-contracts.md](contracts/execution-report-csv-contracts.md), [quickstart.md](quickstart.md)

**Tests**: Included per SC-005 and [execution-report-csv-contracts.md](contracts/execution-report-csv-contracts.md) automated verification (mixed-outcome CSV, filename, quoting).

**Organization**: Tasks grouped by user story (US1 → US2 → US3). Application-layer export data and serializer are foundational so US1 download and US2 reconciliation share one pipeline.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label for story phases only
- Include exact file paths in descriptions

## Path Conventions

- `src/ImportToPlanner.Application/` — domain outcomes and CSV serializer
- `src/ImportToPlanner.Web/` — presenter mapping, report UI, JS download
- `tests/ImportToPlanner.Tests/` — exporter and use case unit tests
- `tests/ImportToPlanner.Web.Tests/` — report step UI tests
- `docs/` — public end-user documentation

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm feature context and register the new Application service.

- [X] T001 Confirm active branch `016-download-execution-report` and design artefacts under `specs/016-download-execution-report/` align with issue #135
- [X] T002 Register `ExecutionReportCsvExporter` (or `IExecutionReportCsvExporter` if introduced) in `src/ImportToPlanner.Application/DependencyInjection.cs` after the exporter type exists (T008)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Structured per-row execution outcomes and CSV serialization in Application — **required before user story UI and reconciliation work**.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Add `ImportSourceRowOutcomeKind` enum (`Created`, `ReusedOrSkipped`, `Failed`) in `src/ImportToPlanner.Application/Models/ImportSourceRowOutcomeKind.cs`
- [X] T004 [P] Add `ImportSourceRowOutcome` in `src/ImportToPlanner.Application/Models/ImportSourceRowOutcome.cs` with required `RowNumber` (int), `TaskName` (string), `Outcome` (`ImportSourceRowOutcomeKind`); optional `TaskIdentifier` (string?, populated only when `Outcome = Created`); optional `Reason` (string?, user-safe; MUST NOT contain secrets or tenant identifiers)
- [X] T005 Extend `ImportExecutionResult` with required `SourceRowOutcomes` (`IReadOnlyList<ImportSourceRowOutcome>`, ordered by `RowNumber` ascending) in `src/ImportToPlanner.Application/Models/ImportExecutionResult.cs`
- [X] T006 [P] Add `ExecutionReportCsvRecordType` enum and `ExecutionReportCsvRow` export projection in `src/ImportToPlanner.Application/Models/ExecutionReportCsvRow.cs` (fields per `specs/016-download-execution-report/data-model.md`)
- [X] T007 Populate `SourceRowOutcomes` in the execution loop in `src/ImportToPlanner.Application/Services/ImportExecutionUseCase.cs` (one `ImportSourceRowOutcome` per eligible source task row; reuse `taskAction.RowNumber` and existing failure mapping; keep existing `CreatedItems` / `ReusedOrSkippedItems` / `FailureItems` lists unchanged)
- [X] T008 Implement `ExecutionReportCsvExporter` in `src/ImportToPlanner.Application/Services/ExecutionReportCsvExporter.cs`: UTF-8 without BOM; comma separator; RFC 4180 quoting when fields contain comma, quote, CR, or LF; fixed eight UK English headers per `specs/016-download-execution-report/contracts/execution-report-csv-contracts.md`; source rows sorted by source row number; manual rows after source rows; filename `import-execution-report-{yyyyMMdd-HHmmss}Z.csv` (UTC at generation)
- [X] T009 Complete T002 service registration in `src/ImportToPlanner.Application/DependencyInjection.cs` once T008 is merged

**Checkpoint**: Application can produce contract-shaped CSV bytes from `ExecutionReportCsvRow` sequences; execution results carry structured row outcomes.

---

## Phase 3: User Story 1 - Download the report after import (Priority: P1) 🎯 MVP

**Goal**: Operators with a completed execution report can save the report as a CSV from wizard step 5 via **Download report** (FR-001, FR-008, FR-009).

**Independent Test**: Complete any import that produces a report, choose **Download report** on the report step, and confirm a CSV file is saved (mixed, all-success, and all-failure runs; no offer when no report — US1 scenario 4).

### Tests for User Story 1

- [X] T010 [P] [US1] Add or extend Web tests so the report step renders a **Download report** control when `ImportExecutionReportViewModel` is set and does not when absent in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs` (or dedicated `HomeExecutionReportTests.cs`)

### Implementation for User Story 1

- [X] T011 [P] [US1] Add browser download helper (in-memory bytes, `text/csv`) in `src/ImportToPlanner.Web/wwwroot/js/execution-report-download.js` following the `importToPlannerThemeStorage` interop pattern
- [X] T012 [P] [US1] Reference `js/execution-report-download.js` from `src/ImportToPlanner.Web/Components/App.razor` (same pattern as `js/theme-session-storage.js`)
- [X] T013 [US1] Add **Download report** `MudButton` on the report step when `ExecutionResult` is non-null in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor` (absent or disabled when no completed report)
- [X] T014 [US1] Inject `IJSRuntime` and wire click handler to trigger browser download without server file persistence in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor` (handler completes end-to-end after T017–T018 connect presenter export to exporter)

**Checkpoint**: Report step shows download affordance; browser download works once Phase 4 presenter wiring is complete.

---

## Phase 4: User Story 2 - Reconcile the file with the source CSV (Priority: P1)

**Goal**: Downloaded CSV rows match on-screen outcomes with source row numbers, task names, UK English outcomes, task identifiers when created, user-safe reasons, and manual follow-up rows in one file (FR-002–FR-007, FR-010, SC-002, SC-003, SC-005).

**Independent Test**: Run a mixed-outcome import, download the report, and verify each CSV row matches the on-screen report (including manual follow-up at the bottom).

### Tests for User Story 2

- [X] T015 [P] [US2] Add `tests/ImportToPlanner.Tests/ExecutionReportCsvExporterTests.cs` asserting SC-005: filename `import-execution-report-*Z.csv`, exact eight header titles, rows for Created / Reused or skipped / Failed / Manual follow-up, and correct quoting when a field contains a comma
- [X] T016 [US2] Extend `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs` to assert `SourceRowOutcomes` includes row numbers, created task identifiers when applicable, and failure/skip reasons without secrets

### Implementation for User Story 2

- [X] T017 [US2] Map `ImportExecutionResult.SourceRowOutcomes` to `ExecutionReportCsvRow` with contract Outcome labels (`Created`, `Reused or skipped`, `Failed`) and user-safe `ReasonOrDetails` in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- [X] T018 [US2] Append manual follow-up `ExecutionReportCsvRow` entries from `ManualActionViewModel` (`Record type` = `Manual follow-up`, Outcome = `Manual follow-up`, Action type / Goal name / Reason or details aligned with on-screen manual tab; omit credit columns) in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- [X] T019 [US2] Expose a single presenter method (e.g. build export rows + call `ExecutionReportCsvExporter`) for the download handler in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs`
- [X] T020 [US2] Complete T014 download handler to call presenter export + `IJSRuntime` download with generated filename and bytes in `src/ImportToPlanner.Web/Features/Import/Pages/Home/HomeExecutionReport.razor`

**Checkpoint**: Mixed-outcome imports produce a reconcilable CSV; unit tests pass for exporter and use case row population.

---

## Phase 5: User Story 3 - Learn that a downloadable report exists (Priority: P2)

**Goal**: Public help mentions CSV download and typical uses (FR-011, SC-004).

**Independent Test**: Read `docs/import-workflow.md` and `docs/faq.md` and confirm each mentions execution report CSV download (record-keeping, sharing, correcting source for re-upload; no separate retry-failed-only control — FR-012).

### Implementation for User Story 3

- [X] T021 [P] [US3] Add at least one UK English sentence on downloading the execution report as CSV (purpose: keep, share, fix source file) in the final report section of `docs/import-workflow.md`
- [X] T022 [P] [US3] Add at least one UK English sentence on using the downloaded report for re-upload and sharing (not in-app retry-failed-only) in `docs/faq.md`

**Checkpoint**: SC-004 satisfied; docs match on-screen **Download report** labelling.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Quality gates and validation across stories.

- [X] T023 Run `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ExecutionReportCsv|FullyQualifiedName~ImportExecutionUseCaseTests|FullyQualifiedName~ImportPresenterTests|FullyQualifiedName~HomePageWorkflowTests"` per `specs/016-download-execution-report/quickstart.md`
- [X] T024 [P] Run `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"` to confirm CSV code stays in Application without MudBlazor/Graph references
- [X] T025 Execute manual smoke steps 1–5 in `specs/016-download-execution-report/quickstart.md` (in-memory run, download, verify eight columns, no secrets in file)
- [X] T026 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` per `AGENTS.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 immediately; T002 completes after T008/T009
- **Foundational (Phase 2)**: Blocks all user stories — complete T003–T009 before Phase 3+
- **User Story 1 (Phase 3)**: UI and JS after foundational; T014 fully functional after T019–T020
- **User Story 2 (Phase 4)**: Presenter mapping and SC-005 tests; T020 finishes US1 download pipeline
- **User Story 3 (Phase 5)**: Can proceed in parallel with late US1/US2 once copy is stable; merge with feature
- **Polish (Phase 6)**: After desired stories complete

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2; download bytes depend on US2 presenter/export (T019–T020)
- **US2 (P1)**: Depends on Phase 2 only; independently verifiable via unit tests without browser
- **US3 (P2)**: No code dependency on US1/US2; should ship in the same increment as UI (FR-011)

### Within Each User Story

- Tests for US2 (T015–T016) can be written against T008 exporter and T007 use case outcomes
- Presenter mapping (T017–T019) before end-to-end download (T020)
- Models and use case (Phase 2) before presenter and UI

### Parallel Opportunities

- T004 and T006 in parallel after T003
- T011 and T012 in parallel; T010 parallel with T011 once exporter tests exist
- T015 parallel with T017–T018 (different files)
- T021 and T022 in parallel
- T024 parallel with T026 after tests pass

---

## Parallel Example: User Story 2

```bash
# Unit tests while presenter mapping is implemented:
Task T015: tests/ImportToPlanner.Tests/ExecutionReportCsvExporterTests.cs
Task T017: src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs (source rows)
Task T018: same file (manual follow-up rows)
```

---

## Parallel Example: User Story 1

```bash
# Browser plumbing while waiting on presenter export API:
Task T011: src/ImportToPlanner.Web/wwwroot/js/execution-report-download.js
Task T012: src/ImportToPlanner.Web/Components/App.razor
Task T013: HomeExecutionReport.razor (button markup)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1–2 (structured outcomes + exporter)
2. Complete Phase 4 core (T015–T020) so CSV content is correct
3. Complete Phase 3 (T010–T014) for visible download
4. **STOP and VALIDATE** using quickstart unit tests and one manual mixed-outcome download
5. Add Phase 5 (docs) before merge

### Incremental Delivery

1. Foundation → contract-valid CSV bytes from tests (no UI)
2. US2 presenter + tests → reconcilable file content
3. US1 button + JS → operator-facing download
4. US3 docs → discoverability
5. Polish → CI format and architecture gates

### Parallel Team Strategy

- Developer A: Phase 2 use case + `ImportExecutionUseCaseTests` (T007, T016)
- Developer B: `ExecutionReportCsvExporter` + T015
- Developer C: US1 UI/JS (T011–T013) then T020 when presenter API ready
- Developer D: US3 docs (T021–T022) anytime after wording is agreed

---

## Notes

- Do not add CsvHelper in Application; manual RFC 4180 escaping only (plan.md)
- Do not add PDF export, retry-failed-only control, or credit ledger columns (FR-012, FR-013, contract exclusions)
- Label **Download report** must match UK English spec (FR-001)
- `[P]` tasks = different files; avoid concurrent edits to `ImportExecutionPresenter.cs` and `HomeExecutionReport.razor` across tasks without ordering T017 → T019 → T020

---

## Phase 7: Convergence

- [X] T027 Align source-row `Reason or details` in CSV export with on-screen execution report copy by mapping failed (and credit-related created) rows through the same presenter user-safe messaging as the Errors tab in `src/ImportToPlanner.Web/Features/Import/Presenters/ImportExecutionPresenter.cs` per SC-002 and `specs/016-download-execution-report/contracts/execution-report-csv-contracts.md` (partial)
- [X] T028 Complete quickstart §5 manual browser validation (mixed-outcome import, **Download report**, verify eight columns and no secrets in file) and note the result in the PR or feature notes per `specs/016-download-execution-report/quickstart.md` T025 (partial)
- [X] T029 Add an exporter unit test asserting RFC 4180 quoting when a field contains a line break in `tests/ImportToPlanner.Tests/ExecutionReportCsvExporterTests.cs` per SC-005 and contract encoding rules (partial)
