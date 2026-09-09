# Research: Optional Assignees From CSV

## Where to parse Assigned To

- **Decision**: Parse the optional `Assigned To` cell in `CsvImportParser` into an ordered, de-duplicated list of trimmed address strings on `CsvTaskRow`. After CsvHelper has isolated the field (quoted commas already handled), split on comma and semicolon. Empty fragments are dropped. Duplicate addresses on the same row (ordinal ignore-case) collapse to the first occurrence. Do **not** treat unknown text as a row-level parse error.
- **Rationale**: Spec FR-004/FR-005: unassignable people are follow-up, not validation failures (unlike Priority and Due Date). Split-after-parse keeps delimiter detection unchanged for semicolon files.
- **Alternatives considered**:
  - Row-level email-format validation: contradicts create-and-follow-up.
  - Application-layer splitting: would pull CSV heading and list-separator rules into inner policy without a second caller.

## Matching identity

- **Decision**: At preview, match each CSV value to a destination member using ordinal ignore-case equality against **work email** and **sign-in name** only (`mail` and `userPrincipalName` in the Graph adapter). Other aliases and display names do not match. Resolve to an opaque member id for create. The same person listed twice (email plus sign-in name) is one assignee.
- **Rationale**: Clarification session 2026-09-09. Predictable operator rule; no `User.Read.All` or extra mailbox enumeration.
- **Alternatives considered**: Match `otherMails` / proxy addresses: more hits, harder to explain, extra Graph shape in the adapter. Email-only: would miss UPN-only members.

## Destination member lookup

- **Decision**: Add `IPlannerGateway.GetPlanMembersAsync(containerId, containerType, cancellationToken)` returning repository-owned `PlanMember` (`Id`, `Mail`, `SignInName`). Call it from `ImportPlanningUseCase` **only when** any parsed row has at least one assignee address. Map Graph in the gateway:
  - **Group**: paginated `groups/{containerId}/members/microsoft.graph.user` with `id`, `mail`, `userPrincipalName` (existing `GroupMember.Read.All`). Include guests who are members.
  - **User**: signed-in user via existing `User.Read` (`/me` mail and UPN) as the only destination member.
  - **Roster**: list roster members through the existing Graph client if the SDK path works with current scopes; otherwise fail closed as lookup failure.
- **Rationale**: FR-019. Existing scopes already include `GroupMember.Read.All`; FR-017 is satisfied with **no new Graph permission**. Guests already in the group are assignable (clarification). Personal plans have no group roster.
- **Alternatives considered**: `User.Read.All` directory search: larger consent, people-picker-like, out of spec. Lookup on every preview including files without Assigned To: extra latency and false blocks. Skip lookup and resolve only at execute: preview could not show assignable vs follow-up.

## Lookup failure versus unknown person

- **Decision**: If member lookup fails (403, 404 container, timeout after existing Graph retry policy), planning sets `HasValidationErrors = true` and a **request-level** `ImportValidationError` (`RowNumber = 0`, `Field = "Assigned To"`) with a presenter-safe reason. Do not emit per-person follow-up. Execution already refuses `HasValidationErrors`. Files with no assignee addresses skip lookup.
- **Rationale**: Clarification: stop until lookup works. Unknown addresses after a **successful** lookup remain create-and-follow-up.
- **Alternatives considered**: Treat everyone as follow-up when lookup fails: silent loss of assignment. Fail only rows with Assigned To: still creates other tasks while operators think owners will apply.

## Create-time assignments

- **Decision**: Extend `CreateTaskAsync` with `IReadOnlyList<string> assigneeUserIds` (empty means none). Graph adapter sets `plannerTask.assignments` on the existing create POST (`plannerAssignment` + `orderHint`). Do not PATCH existing tasks. If the create POST fails because of assignments, retry **create without assignments**, return which ids were applied (none in that case), and let execution emit follow-up for every intended person. If POST succeeds, applied ids are those sent. Do not add people to the group.
- **Rationale**: Graph create accepts assignments in the same body as title. A failed assignment must not lose the task (spec US3). Skip rows never call create. Domain `PlannerTaskSnapshot` stays title/id/plan for idempotency; applied ids live on an Application create result wrapper or an extra return field on the gateway result type owned by Application (`CreatedPlannerTask`).
- **Alternatives considered**: Create then PATCH each assignee: more ETag traffic; nicer partial success, deferred unless POST-with-all proves too brittle in tests. Create-request DTO replacing positional parameters: cleaner arity, larger churn across credits and stubs; deferred one more increment by adding a single list parameter. Auto-add to group: forbidden by FR-009.

## Preview, execute, and fingerprints

- **Decision**: Carry on `ImportTaskPlanItem`: CSV addresses, resolved member ids (create rows), unresolved addresses with reason codes. Presenter shows who will be assigned vs follow-up. Skip/duplicate rows still **display** CSV addresses but execution emits **no** `AssignPersonToTask` actions. Request fingerprint includes the normalised assignee list per row. Planner-state fingerprint stays buckets + task titles (membership drift does not force re-preview).
- **Rationale**: Clarification: execute continues if a previewed assignee can no longer be assigned. Execution uses preview-resolved ids; Graph refusal → follow-up, not stale-preview block.
- **Alternatives considered**: Fingerprint group membership: noisy on large groups. Re-lookup at execute: extra Graph call; still need follow-up if someone left.

## Manual actions

- **Decision**: New action type `AssignPersonToTask`. Extend `ManualAction` with optional `PersonIdentifier` (the CSV address). `Details` holds a stable reason code (`not-a-member`, `not-an-address`, `assignment-refused`, `destination-limit`). Web presenter maps type + code to UK English. Do not emit these actions for skip/already-exists rows.
- **Rationale**: Same report family as Goal; distinct type for UI badges (spec 010). Wording stays in the presenter (constitution III).
- **Alternatives considered**: Reuse `GoalName` for the email: confusing in the grid. Prose `Details` from Application: presenter leakage in reverse.

## Extra columns, aliases, docs

- **Decision**: Exact heading `Assigned To` (trim + existing case-insensitive header match). `Assignee`, `Email`, `Assigned to` as a distinct alias remain unexpected unless ignore-extras. Update extra-column fixtures so a true extra (for example `Owner`) remains. Docs and Home accepted-fields copy include optional Assigned To; full sample only; no people-picker.
- **Rationale**: FR-012–FR-015, same pattern as Due Date.
- **Alternatives considered**: Accept Planner export `Assigned to` casing beyond case-fold: deferred to #127.

## Testing approach

- **Decision**: Parser tests for split/dedupe/blank/absent/semicolon. Planning tests: lookup skip when no addresses; block when lookup fails; match mail/UPN; guest member assignable; alias-only unresolved; skip rows show addresses but are Skip. Execution tests: create receives ids; mixed cells; skip does not create or emit assignee follow-up; execute-time assignment miss still creates. Graph tests: POST assignments JSON; lookup paging; 403 lookup maps to authorisation failure. Stub members on gateway doubles. No Playwright; no AppHost tests; no new Graph scopes in `appsettings.json`.
- **Rationale**: Engineering policies; constitution VI.
- **Alternatives considered**: Live Graph CI: not repeatable.
