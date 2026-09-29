# Research: Download Execution Report as CSV

**Feature**: [spec.md](spec.md) | **Issue**: [#135](https://github.com/markheydon/import-to-planner/issues/135)

## 1. Structured row data for export

**Decision**: Add `ImportSourceRowOutcome` rows to `ImportExecutionResult`, populated in `ImportExecutionUseCase` from each `ImportTaskPlanItem` (`RowNumber`, `TaskName`, outcome kind, optional Planner task id, neutral skip/failure reason). Keep existing `CreatedItems`, `ReusedOrSkippedItems`, and `FailureItems` lists for backward-compatible presenter summary tabs; export and future UI detail read from the structured list.

**Rationale**: FR-002 requires one CSV row per source row. Today’s report UI uses flattened strings without row numbers. Building outcomes during the execution loop reuses `taskAction.RowNumber` and failure mapping already keyed by task name.

**Alternatives considered**:

- Reconstruct export from preview + string lists only — rejected; failures and credit stops do not reliably carry row numbers today.
- Replace summary lists with structured rows only — rejected; larger refactor with no user benefit in this increment.

## 2. Manual follow-up in the same CSV

**Decision**: Append manual follow-up rows after all source-row outcomes, using the same header row. Use a **Record type** column with values `Source row` and `Manual follow-up`. Manual rows leave **Source row number** and **Task identifier** empty when not applicable; populate **Action type**, **Goal name**, **Task name**, and **Reason or details** to mirror `ManualActionViewModel` on screen.

**Rationale**: Matches issue preference for a single readable CSV (FR-003) without a second file.

**Alternatives considered**:

- Separate `manual-actions.csv` download — rejected by spec and issue.
- Embed manual actions only as extra columns on source rows — rejected; goal-level `EnsureGoalExists` is not 1:1 with a single source row.

## 3. CSV format and encoding

**Decision**: Comma-separated UTF-8 **without** BOM; RFC 4180-style quoting when fields contain comma, quote, or newline. Column headings fixed UK English (see contract). Outcome values: `Created`, `Reused or skipped`, `Failed`, and `Manual follow-up` for manual rows.

**Rationale**: Aligns with sample download policy (#133 / FR-009 in delimiter spec). Quoting satisfies Excel opening via double-click when fields contain commas.

**Alternatives considered**:

- UTF-8 BOM on export — deferred; not required if quoting is correct; keeps export consistent with public samples.
- Semicolon separator — rejected; export is not locale-sniffed; operators can import into Excel with locale settings.

## 4. Filename pattern

**Decision**: `import-execution-report-{yyyyMMdd-HHmmss}Z.csv` using UTC timestamp at generation time. Characters limited to alphanumerics, hyphens, and `.csv` suffix.

**Rationale**: Human-readable, sortable, safe on Windows/macOS/Linux, testable (SC-005).

**Alternatives considered**:

- Include plan name — rejected; plan names may contain invalid filename characters and are not required for reconciliation.

## 5. Download delivery (Blazor)

**Decision**: **Download report** on `HomeExecutionReport` generates CSV in memory from the presenter-owned export model and triggers a browser download via JavaScript interop (same pattern as existing `importToPlannerThemeStorage` module: small `wwwroot` helper or inline `IJSRuntime` call with base64/data URL). No server-side file persistence (FR-009).

**Rationale**: Dynamic content cannot use static `wwwroot` links like sample CSVs. Session-scoped view model already holds report data.

**Alternatives considered**:

- New minimal API endpoint — rejected; adds persistence/temp-file risk and crosses unnecessary boundary for session-local data.
- `<a download href="data:text/csv...">` only — acceptable fallback if JS module is avoided; interop preferred for large reports and consistent MIME type.

## 6. User-safe text in CSV

**Decision**: **Reason or details** column uses the same presenter-mapped, user-safe strings shown in the execution report (via `ImportExecutionPresenter` / existing failure and manual-action mappers). No raw diagnostic codes, tenant ids, or tokens (FR-010). Omit commercial **Credits used** / **Remaining credits** from CSV rows (summary tab only); not row-outcome data.

**Rationale**: Constitution VIII and FR-010; ledger remains separate (#126).

## 7. Testing strategy

**Decision**: Unit tests in `ImportToPlanner.Tests` for `ExecutionReportCsvExporter` (headings, quoting, row order, filename). Extend execution/presenter tests for structured row population. Optional bUnit assertion that report step renders a download control when `ExecutionResult` is set.

**Rationale**: Meets constitution VI and SC-005 without Playwright for this increment.

**Alternatives considered**:

- Web-only tests — insufficient for CSV content edge cases (commas in task names).

## 8. Documentation

**Decision**: Update `docs/import-workflow.md` (report step) and `docs/faq.md` (retry/share results) with one sentence each on CSV download purpose (FR-011).

**Rationale**: Issue acceptance criteria; no new docs site section required.
