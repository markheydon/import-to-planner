# Data Model: Import Due Dates From CSV

No new persisted entities. Due date is an optional field on the existing import row and preview item. Graph timestamps are adapter-only.

## Existing entities (extended)

### CsvTaskRow

Normalised row after a successful parse.

- Existing: `RowNumber`, `TaskName`, `Description`, `Priority`, `Bucket`, `Goal`
- **Add**: `DueDate` (`DateOnly?`) — calendar date from CSV, or `null` when omitted

Relationship: produced by `CsvImportParser` after heading/row validation. Invalid due dates do not produce a row (same as invalid Priority).

### ImportTaskPlanItem

Dry-run decision for one CSV row.

- Existing: `RowNumber`, `TaskName`, `Bucket`, `Goals`, `Action`, `Reason`, …
- **Add**: `DueDate` (`DateOnly?`) — value that would be applied **if** `Action` is Create; still shown when Action is Skip so operators can see the CSV date will not be written

### ImportValidationError

Unchanged shape.

- Row-level due date failures: `Field = "Due Date"`, `RowNumber` = CSV data row, UK English message that the value must be empty or a recognised date.
- Heading `Due Date` is no longer an extra-column error.

### Import request fingerprint

Include each row’s due date as `yyyy-MM-dd` or empty after Goal. Planner-state fingerprint (bucket names + task titles) is unchanged; skip matching remains title-only.

### PlannerTaskSnapshot (Domain)

Unchanged. Idempotency still uses `Id`, `Title`, `PlanId`. Do not add Graph `dueDateTime` to Domain.

## Conceptual parse-time entity

### Due date cell

| Input | Outcome |
| ----- | ------- |
| Column absent | All rows `DueDate = null`; not an error |
| Empty / whitespace | `DueDate = null`; row may still be valid |
| Exact accepted format | `DateOnly` set |
| Any other non-empty text | Row-level error; no `CsvTaskRow` for that line |

Accepted format list is defined in [contracts/import-due-dates-contracts.md](contracts/import-due-dates-contracts.md).

## Validation rules

1. Canonical heading is `Due Date` (trim + case-insensitive match, same as other headings).
2. Date-only; times and serial numbers fail the row.
3. Numeric dates are day-first (UK).
4. Two-digit years map to 2000–2099.
5. Quoted cells parse if the inner text matches an accepted format.
6. Extra unknown columns still follow `ignoreExtraColumns`.
7. Name-matched existing tasks: plan Skip / already exists; execution must not write due date.

## State transitions

```text
Parse CSV
    → invalid Due Date → ParseErrors, preview blocked
    → valid / omitted Due Date → planning
        → existing title match → Skip (already exists); CSV date displayed, not applied
        → new title → Create with DueDate
            → execution POST task (adapter maps DateOnly → dueDateTime)
            → skip rows: no create, no update
```

No new wizard steps.

## Traceability

| Spec | Model rule |
| ---- | ---------- |
| FR-001–FR-003 | Optional heading; empty cell |
| FR-004–FR-005 | Format list and row-level error |
| FR-006 | `ImportTaskPlanItem.DueDate` + preview column |
| FR-007 | Create path maps calendar date |
| FR-008 | Skip does not write |
| FR-010 | Exact heading only |
| FR-014 | Semicolon and extra-column behaviour unchanged |
