# Research: Import Due Dates From CSV

## Date parsing location and accepted shapes

- **Decision**: Parse the optional `Due Date` column in `CsvImportParser` (Infrastructure), using `DateOnly.TryParseExact` with a fixed format list and invariant culture. Accepted formats: `yyyy-MM-dd`; UK day-first `d/M/yyyy`, `dd/MM/yyyy`, `d/M/yy`, `dd/MM/yy`; the same four with `-` instead of `/`. Configure two-digit years so 00–99 map to 2000–2099. Empty or whitespace cells become `null`. Any other non-empty text, including times (`2026-05-31T17:00`) and Excel serial numbers, is a row-level error on field `Due Date`.
- **Rationale**: Spec FR-004/FR-005 and the existing Priority pattern: format policy stays in the CSV adapter; Application receives a calendar date or a structured validation error. Invariant exact formats avoid host-locale month-first parsing. The spec already records two-digit-year and no-time assumptions.
- **Alternatives considered**:
  - `DateTime.Parse` / current culture: would accept US month-first on some hosts and violate UK day-first.
  - Application-layer parser: would pull CSV heading and format strings into inner policy without a second caller.
  - Accepting Excel serials: convenient for some exports, but ambiguous integers and not required by the issue.

## Inner-layer type versus Graph timestamp

- **Decision**: Store an optional `DateOnly? DueDate` on `CsvTaskRow` and related Application planning models. Do **not** put `DateTimeOffset` or Graph `plannerTask` types in Application or Domain. Map to Graph only in `GraphPlannerGateway`.
- **Rationale**: Constitution II–III and `docs-internal/microsoft-graph-guidelines.md`: vendor timestamps stay at the adapter. `DateOnly` matches the spec’s calendar-date language and is BCL, not a framework leak.
- **Alternatives considered**: `DateTimeOffset` on `CsvTaskRow`: forces UTC policy into every test and fingerprint. String `yyyy-MM-dd` on the row: weaker typing and extra parse at execute time.

## Planner `dueDateTime` mapping (create only)

- **Decision**: On create, set Graph `plannerTask.dueDateTime` in the existing `Tasks.PostAsync` payload to **10:00 UTC on the CSV calendar date** (`DateTimeOffset` with offset zero). Do not PATCH existing tasks. Do not add `startDateTime`.
- **Rationale**: [plannerTask.dueDateTime](https://learn.microsoft.com/en-us/graph/api/resources/plannertask?view=graph-rest-1.0) is a UTC timestamp, not a date-only field. Planner’s own web client stores a picked date as 10:00 UTC so conversion to local time stays on the same calendar date in most zones (documented community behaviour; Graph itself only specifies UTC ISO-8601). Midnight UTC (`T00:00:00Z`) can display as the previous day west of UTC. Create API [`POST /planner/tasks`](https://learn.microsoft.com/en-us/graph/api/planner-post-tasks?view=graph-rest-1.0) accepts a `plannerTask` body; due date can be set with title and priority on the same POST, matching how priority is already sent. Skip/already-exists rows never call create, so they cannot update dates.
- **Alternatives considered**:
  - Midnight UTC: simpler, but fails SC-001 for operators west of UTC.
  - Host `TimeZoneInfo.Local`: hosted and self-hosted servers are not the operator’s zone.
  - Separate PATCH after create: extra conflict/ETag path for a field that create already supports.

## Gateway contract change

- **Decision**: Add `DateOnly? dueDate` to `IPlannerGateway.CreateTaskAsync` (after `goal`, before `CancellationToken`). Pass `sourceRow.DueDate` from `ImportExecutionUseCase`. Update Graph gateway, all test doubles, and Graph create tests to assert `dueDateTime` on POST when present and omitted when null.
- **Rationale**: The gateway already uses positional optional fields (`description`, `priority`, `goal`). A new request DTO would be cleaner long-term but is a larger refactor than this story needs. Execution already loads `CsvTaskRow` for create; skip paths must not call update APIs.
- **Alternatives considered**: Create-task command object: better arity, more churn across credits and web stubs. Optional parameter with default `null`: hides missing updates in doubles. PATCH-only due dates: contradicts create-only spec.

## Preview and fingerprint

- **Decision**: Carry `DateOnly? DueDate` on `ImportTaskPlanItem` (including skip rows, for operator visibility). Presenter shows a UK `dd/MM/yyyy` string or blank. Add a **Due date** column on the existing preview grid. Include `yyyy-MM-dd` (or empty) in `ImportFingerprintBuilder` request fingerprints so changing a date requires a fresh preview.
- **Rationale**: FR-006. Showing dates on skipped rows makes the “will not update existing cards” behaviour visible (`Reason` remains `already exists`). Fingerprint must include dates or operators could confirm a stale preview after editing CSV dates.
- **Alternatives considered**: Preview-only join back to `CsvTaskRow` in Web: leaks parse models into UI mapping. ISO-only display: less familiar for UK Excel users.

## Extra-column fixture and aliases

- **Decision**: Treat exact heading `Due Date` as supported (case-insensitive match already used by `PrepareHeaderForMatch`). Do not accept `Due date` as a distinct alias beyond that trim/case fold; do not accept `DueDate` or `Deadline`. Update `tests/ImportToPlanner.Tests/Fixtures/with-extra-columns.csv` so remaining extras (for example `Owner`) still exercise ignore-extra-columns; stop asserting `Due Date` as an unexpected column.
- **Rationale**: FR-010 defers aliases to #127. Today `Due Date` in the extra-columns fixture is an unknown heading; after this story it is canonical.
- **Alternatives considered**: Keep Due Date as extra until mapping ships: directly contradicts the issue.

## Documentation and in-app cues

- **Decision**: Update `docs/csv-format.md` (accepted columns, date shapes, full example, common mistakes) and a short troubleshooting bullet for invalid dates. Extend Home compact accepted-fields copy and the wizard/import-guidance contracts’ accepted-field lists. Minimal docs example stays Task Name only. Follow the end-user-docs skill (UK English).
- **Rationale**: FR-011–FR-013. There is no separate in-app sample download today; public docs examples are the samples.
- **Alternatives considered**: Docs-only without parser/gateway change: rejected by the issue.

## Testing approach

- **Decision**: Parser unit tests for valid ISO/UK formats, blank, invalid, two-digit year, quoted dates, semicolon files, and heading absence. Execution tests: create passes due date; skip/already-exists does not call create (or update). Graph gateway tests: POST body includes `dueDateTime` at 10:00Z for a known `DateOnly`, omitted when null. Planning/fingerprint tests: date in preview and fingerprint. bUnit only if Home accepted-fields copy or preview column tests already exist and would otherwise fail. No Playwright; no AppHost tests. Architecture tests unchanged unless Graph types leak inward.
- **Rationale**: Engineering policies and constitution VI: smallest automated checks. Parser and gateway stubs cover policy without live Graph.
- **Alternatives considered**: Live Planner round-trip in CI: not repeatable. Playwright preview screenshot: disproportionate for one column.
