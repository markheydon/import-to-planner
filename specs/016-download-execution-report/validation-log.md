# Validation log: Download Execution Report as CSV

Feature: `016-download-execution-report` (issue [#135](https://github.com/markheydon/import-to-planner/issues/135))

## Quickstart §5 manual browser smoke (T028)

**Date**: 2026-09-29

**Attempt**: Start `ImportToPlanner.Web` locally and complete a mixed-outcome import through step 5, then use **Download report**.

**Result**: Not completed end-to-end in this environment. `dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj` failed at startup because `AzureAd:TenantId` is still a placeholder (`StartupConfigurationValidator`).

**Substitute verification** (same session):

- Unit tests: `ExecutionReportCsvExporterTests` (mixed outcomes, comma and line-break quoting, filename pattern).
- Presenter tests: `ImportExecutionPresenter_BuildExecutionReportCsvDownload_*` and `ImportExecutionPresenter_CsvExport_UsesSameFailureCopyAsErrorsTab` (SC-002 alignment).
- Web tests: `HomePageWorkflowTests` (**Download report** visible only when report view model is present).

**Pre-merge recommendation**: On a machine with valid Azure AD / Graph configuration, run quickstart §5 steps 1–5 and confirm the downloaded CSV has eight columns, sorted source rows, manual rows at the bottom, and no secrets.
