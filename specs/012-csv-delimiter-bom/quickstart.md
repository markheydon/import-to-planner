# Quickstart: Detect CSV Delimiter and UTF-8 BOM

## Feature quality gates and traceability

1. Semicolon-delimited valid CSV parses equivalently to the comma-delimited twin (FR-002, FR-006, US1).
2. Leading U+FEFF does not break the first heading (FR-001, US2).
3. Ambiguous and unsupported separators fail as `Field = "File"`, `RowNumber = 0`, before heading validation (FR-003, FR-004, US3).
4. Quoted commas/semicolons stay inside fields (FR-005, US4).
5. Extra-column ignore behaviour and 10 MB upload limit unchanged (FR-007).
6. Public csv-format and troubleshooting pages describe Excel locale CSV and BOM (FR-010, US5).
7. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` passes before merge.

Issue: [#133](https://github.com/markheydon/import-to-planner/issues/133).

## Prerequisites

- .NET SDK from `global.json` (10.0.300, `rollForward: latestFeature`)
- Repository restored

## 1. Automated parser checks

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests"
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expected: new cases in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs` cover at least:

| Case | Expected |
| ---- | -------- |
| Comma headers + quoted commas in Description | Success, one description field |
| Semicolon twin of a known-good comma CSV | Same task names and fields |
| `"\uFEFFTask Name,Description\n..."` | Success; heading recognised |
| `"\uFEFFTask Name;Description\n..."` | Success |
| Header `Task Name,Bucket;Goal` (both unquoted) | File-level ambiguous; no “Task Name column is required” |
| Tab-separated header | File-level unsupported |
| Single-column `Task Name\nTask A` | Success (comma default) |
| Semicolon file + extra column + `ignoreExtraColumns: true` | Success |

Construct BOM cases in test strings so `File.ReadAllText` cannot strip the mark first.

## 2. Architecture guard

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"
```

Expected: Application and Domain still contain no `CsvHelper` token.

## 3. Docs check

Open `docs/csv-format.md` and `docs/troubleshooting.md` and confirm a non-technical reader can see:

- UK/EU Excel may use semicolon
- UTF-8 BOM is tolerated
- What to do if the app cannot detect the separator
- Examples still use commas and no BOM

## 4. Manual first-run (optional, in-memory)

```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

1. Save a small sheet from Excel as CSV UTF-8 in a semicolon locale (or hand-edit a `.csv` to use `;` and prepend a UTF-8 BOM in a hex editor).
2. Upload, preview: columns recognised; no missing Task Name error.
3. Upload a header that mixes unquoted `,` and `;`: file-level message; preview blocked.
4. Confirm a normal comma CSV still previews.

Self-hosted and hosted both use the same parser; Graph sign-in is not required to validate this feature.

## 5. Out of scope during validation

- Opening `.xlsx`
- Asking the UI to pick comma vs semicolon
- Windows-1252 files that are not valid UTF-8
