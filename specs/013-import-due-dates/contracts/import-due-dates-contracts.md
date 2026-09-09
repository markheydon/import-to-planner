# Contract: Import Due Dates From CSV

## Scope

Optional CSV `Due Date` parse, preview display, create-only write through `IPlannerGateway`, public CSV docs, and compact Home accepted-fields copy.

Traceability: GitHub issue [#131](https://github.com/markheydon/import-to-planner/issues/131); spec FR-001–FR-015.

Non-goals: start date, labels, checklists, progress, heading aliases (#127), Excel workbooks (#55), updating existing tasks.

## Parser (`ICsvImportParser`)

Signature unchanged:

`ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)`

### Supported headings

Add `Due Date` to the supported heading set alongside `Task Name`, `Description`, `Priority`, `Bucket`, and `Goal`. Matching remains trim + case-insensitive. `DueDate` and `Deadline` remain unexpected columns unless `ignoreExtraColumns` is true.

### Row field `Due Date`

1. Missing column: do not error; each row’s `DueDate` is `null`.
2. Empty or whitespace: `DueDate` is `null`.
3. Otherwise parse with invariant `DateOnly.TryParseExact` and these formats only (order does not change meaning):

   - `yyyy-MM-dd`
   - `d/M/yyyy`, `dd/MM/yyyy`, `d/M/yy`, `dd/MM/yy`
   - `d-M-yyyy`, `dd-MM-yyyy`, `d-M-yy`, `dd-MM-yy`

   Two-digit year window: 2000–2099.

4. Failure: `ImportValidationError` with `Field = "Due Date"` and a message that the value must be empty or a recognised date (ISO `yyyy-MM-dd` or a UK day/month/year date). Do not add a `CsvTaskRow` for that line (same control flow as invalid Priority).

Successful rows expose `CsvTaskRow.DueDate` as `DateOnly?`.

Delimiter/BOM behaviour is unchanged. Semicolon files with `Due Date` MUST match the comma twin for names and due dates.

## Application gateway (`IPlannerGateway`)

```text
CreateTaskAsync(
    planId, bucketId, taskName, description, priority, goal,
    dueDate,   // DateOnly?; new
    cancellationToken)
```

- When `dueDate` has a value, the Graph adapter MUST set `plannerTask.dueDateTime` on **create** to `yyyy-MM-ddT10:00:00Z` for that calendar date (10:00 UTC).
- When `dueDate` is `null`, omit `dueDateTime` (do not clear a server default beyond not sending the property).
- Skip / already exists MUST NOT call create or any task update/PATCH for due date.
- Domain `PlannerTaskSnapshot` MUST NOT gain Graph timestamp fields for this story.

`ICsvImportParser` stays free of Graph types. Application stays free of `Microsoft.Graph` types.

## Preview / UI

- `ImportTaskPlanItem` includes `DueDate`.
- Home preview **Task actions** grid adds a **Due date** column. Display `dd/MM/yyyy` when present; blank when omitted. Skip rows still show the CSV date; **Reason** remains `already exists` (or duplicate) so it is clear the date will not be written.
- Compact header guidance accepted fields MUST include **Due Date** as optional (UK English).
- No new wizard step, delimiter control, or mapping UI.
- Validation grid continues to show row-level `Due Date` errors and block preview/execution while `HasErrors` is true.

Request fingerprint MUST include each row’s due date (`yyyy-MM-dd` or empty) so date edits invalidate preview.

## Graph adapter notes

- Set `DueDateTime` on the existing `Planner.Tasks.PostAsync` body together with `PlanId`, `BucketId`, `Title`, and `Priority`.
- Do not introduce `startDateTime` or description-style compensate/delete for due date alone.
- Trust boundary: untrusted CSV already parsed in-process; do not log raw file contents; no workbook load.

## Public docs

Align with `specs/007-end-user-docs-site/contracts/docs-site-contract.md`, extending CSV format obligations:

### `/csv-format`

MUST:

- List `Due Date` as an optional accepted column.
- Describe ISO `yyyy-MM-dd` and UK day-first dates (slashes or hyphens; two- or four-digit years; two-digit years mean 2000–2099).
- State that an invalid non-empty due date fails that row, like Priority.
- Include `Due Date` on the **full** example only; minimal example remains `Task Name` only.
- Add a common-mistake bullet for invalid dates.

### `/troubleshooting`

SHOULD mention invalid Due Date as a row-level CSV validation cause (not a file-level separator error).

## Fixture impact

`with-extra-columns.csv` currently includes `Due Date` as an extra heading. After this contract, treat `Owner` (or another non-canonical heading) as the extra column under test; `Due Date` is supported.

## Credits / auth

Unchanged. Due date does not affect credit counting (still one credit per created task).
