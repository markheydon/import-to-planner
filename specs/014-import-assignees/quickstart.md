# Quickstart: Optional Assignees From CSV

## Feature quality gates and traceability

1. Optional `Assigned To` parses; missing column and blank cells succeed; lists split on comma/semicolon (FR-001–FR-004, US1).
2. Preview shows matched vs follow-up people; lookup failure with addresses present blocks execution (FR-006, FR-019, US1).
3. Create assigns resolved members including destination guests; unmatched and execute-time refusals follow-up; skip does not write (FR-005, FR-007–FR-010, US2–US4).
4. Docs and Home accepted-fields list include optional Assigned To; full sample only; no people-picker (FR-011–FR-015, US5).
5. `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` passes before merge.

Issue: [#132](https://github.com/markheydon/import-to-planner/issues/132).

## Prerequisites

- .NET SDK from `global.json` (10.0.300, `rollForward: latestFeature`)
- Repository restored

## 1. Parser and planning checks

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests"
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ImportPlanningUseCaseTests"
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expected parser cases (add to `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`):

| Case | Expected |
| ---- | -------- |
| No Assigned To column | Success; empty assignee list |
| Empty cell | Empty list |
| `a@contoso.com; b@contoso.com` | Two addresses |
| Quoted `a@contoso.com, b@contoso.com` | Two addresses |
| Duplicate ignore-case | One address |
| `Not a person` | Still a successful row (value kept for follow-up) |
| Semicolon file with Assigned To | Same list as comma twin |

Expected planning cases:

| Case | Expected |
| ---- | -------- |
| No addresses in file | `GetPlanMembersAsync` not required |
| Members cannot be loaded | `HasValidationErrors`; field `Assigned To` |
| Mail or UPN match | Resolved id on Create rows |
| Guest already in destination | Resolved |
| Alias-only / unknown | Unresolved `not-a-member` |
| Existing title | Skip; no resolved ids for write |

## 2. Execution and Graph adapter checks

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ImportExecutionUseCaseTests"
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~GraphPlannerGatewayTests"
```

Expected:

- Create passes resolved ids; `AssignPersonToTask` for unresolved and for ids not in `AppliedAssigneeIds`.
- Skip rows do not call create and do not emit assignee follow-up.
- Graph POST includes `assignments` for given user ids; omitted when the list is empty.
- Graph member lookup uses group members (or `/me` for user containers); 403 is an authorisation failure.

## 3. Architecture guard

```bash
dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"
```

Expected: Application and Domain contain no `Microsoft.Graph` / Kiota tokens; no assignment dictionary types inward.

## 4. Docs and Home copy

Review:

- `docs/csv-format.md` — optional Assigned To, match rule, follow-up vs row error, full example only
- `docs/troubleshooting.md` — lookup block and unmatched people
- Home compact accepted-fields line includes Assigned To
- `docs-internal/entra-app-registration-setup.md` / Graph guidelines — existing `GroupMember.Read.All` used for this feature; no new scope

## 5. Manual first-run (optional)

In-memory / local Web (stub members with known mail):

1. Upload `Task Name,Assigned To` with a member email. Preview shows the person under Assigned to and Create.
2. Upload a mixed cell (member + unknown). Preview shows both; Confirm still enabled.
3. Confirm: created task has the member; report lists `Assign person to task` for the unknown.
4. Re-upload the same title with Assigned To: Skip / already exists; live assignees unchanged.
5. Simulate member-lookup failure: preview blocked; Confirm disabled.

Graph mode when available: same outcomes against a real group; guests already in the group are assigned.

Self-hosted uses the same parser, planning, and gateway; no hosted-only directory.

## 6. Out of scope during validation

- `Assignee` / `Email` headings without ignore-extras
- People-picker, adding users to the group, `.xlsx`
- Updating assignees on existing tasks
- New Graph scopes beyond the current four
