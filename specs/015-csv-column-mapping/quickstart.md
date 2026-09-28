# Quickstart: CSV Column Mapping and Sample Files

## Feature quality gates and traceability

1. Minimal and full sample downloads import without manual mapping (FR-001, US1).
2. Alias-only headers (for example `Title`, `Notes`) parse to correct preview values (FR-004, SC-002).
3. Missing Task Name mapping blocks preview (FR-008, SC-003).
4. Conflicting alias mappings force resolution before preview (FR-009).
5. Confirmed mapping for a layout reapplies on a second file with the same headers (FR-012, SC-004).
6. `localStorage` entry contains no row payloads (FR-014).
7. `docs/csv-format.md` matches in-app samples and alias guidance (FR-003, SC-005).
8. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` passes before merge.

Issue: [#127](https://github.com/markheydon/import-to-planner/issues/127).

## Prerequisites

- .NET SDK from `global.json` (10.0.300, `rollForward: latestFeature`)
- Repository restored
- Delimiter/BOM behaviour from #133 already in tree

## 1. Automated tests

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvColumnMapping"
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests"
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expected new or extended coverage includes at least:

| Area | Case | Expected |
| ---- | ---- | -------- |
| Catalog / mapping service | `Title` + `Notes` headers | Proposal maps to Task Name + Description |
| Catalog / mapping service | Two aliases → Priority | `Conflict` until user resolves |
| Catalog / mapping service | No task title alias | `NeedsTaskName` |
| Layout signature | `Task Name` vs `TaskName` | Same signature |
| Parser + mapping | Alias headers only | Same rows as canonical-header twin |
| Parser + mapping | Extra column + `ignoreExtraColumns: true` | Success, column ignored |
| Samples | Read `wwwroot/samples/*.csv` | Parse with identity mapping, zero errors |

## 2. Architecture guard

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"
```

Expected: Application and Domain contain no `CsvHelper` or `MudBlazor` references; Web layout store does not pull Graph types inward.

## 3. Static sample smoke

```bash
curl -fsS http://localhost:<port>/samples/import-minimal.csv | head
curl -fsS http://localhost:<port>/samples/import-full.csv | head
```

Run the app locally (Aspire or Web project), confirm both URLs return UTF-8 CSV with expected headers.

## 4. Manual browser validation

1. Open Home → upload step.
2. Download minimal sample, upload unchanged, confirm mapping summary shows Task Name, confirm preview succeeds.
3. Upload a CSV with headers `Title,Notes,Priority` only; confirm auto-map summary; preview shows correct columns.
4. Upload CSV with headers `Subject,Title` (both alias Task Name); confirm conflict UI; resolve; preview succeeds.
5. Upload CSV with header `Work item` only; confirm preview blocked until user maps Task Name.
6. Confirm mapping, upload second file same headers different name; mapping applies without full re-edit.
7. Inspect browser `localStorage` for mapping key — JSON must list headers and field ids only, no task titles from rows.

## 5. Docs check

Open `docs/csv-format.md` (and getting-started if linked) and confirm:

- Sample download links match app paths
- Synonyms / mapping mentioned
- Examples still valid comma-separated UTF-8

## Contract reference

See [contracts/csv-column-mapping-contracts.md](contracts/csv-column-mapping-contracts.md) and [data-model.md](data-model.md).
