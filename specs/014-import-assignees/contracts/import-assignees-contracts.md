# Contract: Optional Assignees From CSV

## Scope

Optional CSV `Assigned To` parse, destination member resolve at preview, create-only assignments through `IPlannerGateway`, Goal-style manual follow-up, public CSV docs, and compact Home accepted-fields copy.

Traceability: GitHub issue [#132](https://github.com/markheydon/import-to-planner/issues/132); spec FR-001–FR-019.

Non-goals: people-picker, heading aliases (#127), adding people to the destination, updating existing tasks, Excel workbooks (#55), new Graph scopes.

## Parser (`ICsvImportParser`)

Signature unchanged.

### Supported headings

Add `Assigned To` alongside `Task Name`, `Description`, `Priority`, `Bucket`, `Goal`, and `Due Date`. Matching remains trim + case-insensitive. `Assignee` and `Email` remain unexpected unless `ignoreExtraColumns` is true.

### Row field `Assigned To`

1. Missing column: each row’s `AssigneeAddresses` is empty; not an error.
2. Empty or whitespace cell: empty list.
3. Otherwise split the isolated CSV field on `,` and `;`, trim each part, drop empties, de-duplicate with ordinal ignore-case preserving first-seen order.
4. Do not emit a row-level validation error for values that are not addresses.

Delimiter/BOM behaviour is unchanged. Semicolon files MUST match the comma twin for names and assignee lists.

## Application gateway (`IPlannerGateway`)

```text
GetPlanMembersAsync(containerId, containerType, cancellationToken)
  → IReadOnlyList<PlanMember>

CreateTaskAsync(
    planId, bucketId, taskName, description, priority, goal,
    dueDate,
    assigneeUserIds,   // IReadOnlyList<string>; new, empty = none
    cancellationToken)
  → CreatedPlannerTask  // Snapshot + AppliedAssigneeIds
```

- Call `GetPlanMembersAsync` from planning only when any row has assignee addresses.
- Lookup failure MUST surface as a structured planner/authorisation/unavailable failure the use case maps to request-level `ImportValidationError`.
- Create MUST send assignments for `assigneeUserIds` on Graph POST. Skip / already exists MUST NOT call create or task update for assignees.
- If Graph rejects the create because of assignments, retry create without assignments and return empty `AppliedAssigneeIds` (task still created when the retry succeeds).
- Application and Domain MUST NOT reference `Microsoft.Graph` assignment types.
- Do not add group members.

Matching (planning, not Graph): ignore-case equality of CSV value to `PlanMember.Mail` or `PlanMember.SignInName`.

## Preview / UI

- Preview **Task actions** grid adds an **Assigned to** column: people who will be assigned (matched addresses) and, distinctly, people who will need follow-up.
- Skip rows may show CSV addresses; **Reason** remains `already exists` / `duplicate in CSV`.
- Compact header accepted fields MUST include **Assigned To** as optional (UK English). No people-picker.
- Request-level lookup errors appear in the existing validation surface and keep Confirm disabled (`HasValidationErrors`).
- Request fingerprint MUST include per-row assignee lists.

Manual follow-up presenter:

| ActionType | Display | Details source |
| ---------- | ------- | -------------- |
| `AssignPersonToTask` | Assign person to task | UK English from reason code; show `PersonIdentifier` and task name |

Reason codes: `not-a-member`, `not-an-address`, `assignment-refused`, `destination-limit`.

## Graph adapter notes

- Scopes remain `User.Read`, `Group.Read.All`, `GroupMember.Read.All`, `Tasks.ReadWrite`. Document that `GroupMember.Read.All` is used to resolve Assigned To.
- Group containers: paginated group user members.
- User containers: `/me` only.
- Roster: fail closed if members cannot be listed.
- Create body: `assignments` map of user id → `plannerAssignment` with `orderHint`.
- Trust boundary: untrusted CSV; do not log raw file or member dumps; delegated session only.

## Public docs

Align with `specs/007-end-user-docs-site/contracts/docs-site-contract.md`:

### `/csv-format`

MUST:

- List `Assigned To` as optional.
- Describe work email or sign-in name; several people per cell (comma or semicolon); not other aliases; empty is valid.
- State that people who cannot be assigned become manual follow-up, not a row error.
- State that if destination members cannot be loaded, preview stops.
- Include Assigned To on the **full** example only (at least one single address and one multi-person cell).

### `/troubleshooting`

SHOULD mention blocked preview when destination members cannot be loaded, and unmatched people appearing as follow-up.

Hosted and self-hosted operator docs that list Graph permissions (`docs-internal/entra-app-registration-setup.md`, guidelines) MUST note Assigned To uses existing `GroupMember.Read.All` — no new scope.

## Fixture impact

Keep a non-canonical extra heading (for example `Owner`) in extra-column tests. `Assigned To` is supported.

## Credits / auth

Unchanged credit counting (one credit per created task, including tasks created with partial or no assignees). Consent set unchanged.
