# Quickstart: Download Execution Report as CSV

## Feature quality gates and traceability

1. Report step offers **Download report** when a completed execution report is visible (FR-001, US1).
2. CSV contains every source-row outcome plus manual follow-up rows in one file (FR-002, FR-003, SC-002).
3. Source row number and user-safe failure reasons are present for failed rows (FR-005, SC-003).
4. Filename and columns match [execution-report-csv-contracts.md](contracts/execution-report-csv-contracts.md) (SC-005).
5. No server persistence of generated CSV beyond session report (FR-009).
6. `docs/import-workflow.md` and `docs/faq.md` mention CSV download (FR-011, SC-004).
7. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` passes before merge.

Issue: [#135](https://github.com/markheydon/import-to-planner/issues/135).

## Prerequisites

- .NET SDK from `global.json` (10.0.300, `rollForward: latestFeature`)
- Repository restored
- In-memory runtime mode is enough for most checks; Graph mode optional for manual smoke

## 1. Unit tests (CSV exporter and execution rows)

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ExecutionReportCsv"
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ImportExecutionUseCaseTests|FullyQualifiedName~ImportPresenterTests"
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expected:

- New tests cover mixed outcomes: headings, quoted fields, row order, manual section, filename pattern.
- Execution or presenter tests prove `SourceRowOutcomes` (or equivalent) carries row numbers and task ids for created rows.

## 2. Web component smoke (optional)

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~HomeExecutionReport|FullyQualifiedName~HomePageWorkflowTests"
```

Expected: report with view model renders **Download report**; absent when no report.

## 3. Architecture guard

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"
```

Expected: CSV exporter in Application; no Graph or MudBlazor references in Application/Domain.

## 4. Public docs

Open `docs/import-workflow.md` and `docs/faq.md` and confirm CSV download is mentioned with purpose (record, share, correct source for re-upload).

## 5. Manual end-to-end (in-memory)

```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Use a CSV fixture that yields mixed outcomes (or staged in-memory planner data if available):

1. Complete import through step 5.
2. Choose **Download report**.
3. Open CSV in a spreadsheet tool: verify eight columns, source rows sorted by row number, manual rows at bottom.
4. Confirm no tokens or tenant ids appear in the file.
5. Refresh browser: on-screen report behaviour unchanged; download only when report still in session.

## 6. Out of scope during validation

- PDF export
- Retry-failed-only workflow
- Credit ledger CSV
- Playwright journey (unless explicitly added later)
