# Data Model: Optional Assignees From CSV

No new persisted entities. Assignees are optional values on the import row, preview item, and create call. Graph user objects stay in the adapter.

## Existing entities (extended)

### CsvTaskRow

- Existing: `RowNumber`, `TaskName`, `Description`, `Priority`, `Bucket`, `Goal`, `DueDate`
- **Add**: `AssigneeAddresses` (`IReadOnlyList<string>`) — trimmed, de-duplicated CSV values in first-seen order; empty list when omitted

Invalid assignee text still produces a row. Directory matching is not a parse concern.

### ImportTaskPlanItem

- **Add**:
  - `AssigneeAddresses` — original CSV list (preview, including skip rows)
  - `ResolvedAssigneeIds` — opaque destination member ids to send on create (empty on skip)
  - `UnresolvedAssignees` — list of `{ Address, ReasonCode }` for preview follow-up (empty on skip for execution; may still show in preview as “will not assign”)

### ManualAction

- Existing: `ActionType`, `GoalName`, `TaskName`, `Details`
- **Add**: `PersonIdentifier` (`string?`) — CSV address for `AssignPersonToTask`
- New `ActionType`: `AssignPersonToTask`
- `Details` for that type: reason code only (`not-a-member`, `not-an-address`, `assignment-refused`, `destination-limit`)

### ImportValidationError

Unchanged shape.

- Request-level lookup failure: `RowNumber = 0`, `Field = "Assigned To"`, `HasValidationErrors = true` on the preview
- Heading `Assigned To` is no longer an extra-column error
- Individual unknown addresses are **not** validation errors

### Import request fingerprint

Include each row’s assignee addresses joined in stored order (already normalised). Planner-state fingerprint unchanged (bucket names + task titles).

### PlannerTaskSnapshot (Domain)

Unchanged. Do not add Graph assignment maps to Domain.

## New application types

### PlanMember

Returned by `GetPlanMembersAsync`.

| Field | Meaning |
| ----- | ------- |
| `Id` | Opaque destination user id used on create |
| `Mail` | Work email, may be empty |
| `SignInName` | Organisational sign-in name (UPN), may be empty |

Guest members are included when the destination lists them.

### CreatedPlannerTask

Gateway create result for Application:

| Field | Meaning |
| ----- | ------- |
| `Snapshot` | Existing `PlannerTaskSnapshot` |
| `AppliedAssigneeIds` | Ids actually present on the created task |

Execution follows up any intended id not in `AppliedAssigneeIds`, plus all unresolved CSV addresses from the plan item.

### UnresolvedAssignee

| Field | Meaning |
| ----- | ------- |
| `Address` | CSV value |
| `ReasonCode` | `not-a-member`, `not-an-address`, `assignment-refused`, `destination-limit` |

Display names without `@` use `not-an-address`. Alias-only values that do not match mail/UPN use `not-a-member`.

## Validation and matching rules

1. Canonical heading `Assigned To` (trim + case-insensitive match).
2. Split cell on comma and semicolon after CSV field isolation; trim; drop empties; de-dupe ignore-case.
3. Match ignore-case to `PlanMember.Mail` or `PlanMember.SignInName` only.
4. Successful lookup required when any address is present; failure is request-level.
5. Name-matched existing tasks: Skip; no assignee write; no `AssignPersonToTask`.
6. Never add members to the destination.

## State transitions

```text
Parse CSV
    → Assigned To absent / blank → AssigneeAddresses empty
    → values present → address list on CsvTaskRow (no directory)

Planning
    → no addresses in file → skip member lookup
    → addresses present and lookup fails → HasValidationErrors, execution blocked
    → lookup succeeds
        → match mail/UPN → ResolvedAssigneeIds
        → else UnresolvedAssignees
        → existing title → Skip (CSV addresses visible, not applied)

Execution (Create)
    → POST with ResolvedAssigneeIds
    → applied vs intended → AssignPersonToTask for remainder
    → continue other rows

Execution (Skip)
    → no create, no assignee follow-up
```

No new wizard steps.

## Traceability

| Spec | Model rule |
| ---- | ---------- |
| FR-001–FR-004 | Optional heading; split list |
| FR-005, FR-008 | Unresolved ≠ row error; create + follow-up |
| FR-006, FR-019 | Preview classification; lookup gate |
| FR-007 | Create assignments including guest members |
| FR-009–FR-010 | No group add; skip-only |
| FR-012 | Exact heading |
