# Quickstart: Import Due Dates From CSV

## Feature quality gates and traceability

1. Optional `Due Date` parses; missing column and blank cells succeed (FR-001–FR-003, US1).
2. ISO and UK day-first dates appear in preview; invalid values are row-level errors and block execution (FR-004–FR-006, US1).
3. Create sets the Planner due calendar date; skip/already-exists does not update dates (FR-007–FR-008, US2–US3).
4. Docs and Home accepted-fields list include optional Due Date; full sample only (FR-011–FR-013, US4).
5. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` passes before merge.

Issue: [#131](https://github.com/markheydon/import-to-planner/issues/131).

## Prerequisites

- .NET SDK from `global.json` (10.0.300, `rollForward: latestFeature`)
- Repository restored

## 1. Parser and fingerprint checks

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests"
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ImportPlanningUseCaseTests"
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expected parser cases (add to `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`):

| Case | Expected |
| ---- | -------- |
| No Due Date column | Success; `DueDate` null |
| Empty Due Date cell | Success; `DueDate` null |
| `2026-05-31` | `DateOnly(2026, 5, 31)` |
| `31/05/2026` or `31-05-2026` | Same date |
| `31/05/26` | `2026-05-31` |
| `not-a-date` or `2026-05-31T17:00` | Error, `Field = "Due Date"` |
| Semicolon file with Due Date | Same date as comma twin |
| Extra `Owner` column + ignore extras | Success; Due Date still parsed |

Extra-column fixture must not treat canonical `Due Date` as unexpected.

## 2. Execution and Graph adapter checks

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ImportExecutionUseCaseTests"
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~GraphPlannerGatewayTests"
```

Expected:

- Create passes `DateOnly` into the gateway; skip rows never create/update for that title.
- Graph POST includes `dueDateTime` `2026-05-31T10:00:00Z` (or equivalent) when due date is 31 May 2026; property omitted when null.

## 3. Architecture guard

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"
```

Expected: Application and Domain contain no `Microsoft.Graph` / Kiota tokens; due date on inner models is `DateOnly?`.

## 4. Docs and Home copy

Review:

- `docs/csv-format.md` — optional Due Date, formats, full example only
- `docs/troubleshooting.md` — invalid date as row-level validation
- Home compact accepted-fields line includes Due Date

## 5. Manual first-run (optional)

In-memory / local Web:

1. Upload a file with `Task Name,Due Date` and `Kick-off,31/05/2026`. Preview shows **31/05/2026** and Create.
2. Confirm import in Graph mode when available: Planner card due date is 31 May 2026.
3. Re-upload the same title with a different date: preview Skip / already exists; live due date unchanged.
4. Upload `Due Date` value `banana`: row error; confirm disabled.

Self-hosted uses the same parser and gateway mapping; no hosted-only dependency.

## 6. Out of scope during validation

- `DueDate` / `Deadline` headings without ignore-extras
- Start date, labels, checklists, `.xlsx`
- Updating dates on existing tasks
