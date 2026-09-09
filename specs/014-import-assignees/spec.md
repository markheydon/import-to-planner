# Feature Specification: Optional Assignees From CSV

**Feature Branch**: `014-import-assignees`

**Created**: 2026-09-09

**Status**: Draft

**Input**: User description: "[Story] Optional assignees by email with manual follow-up (https://github.com/markheydon/import-to-planner/issues/132). Accept an optional Assigned To column (email or UPN). Resolve people against the destination group and assign them on create. If someone cannot be assigned, still create the task and emit a manual follow-up action, same pattern as Goal."

## Clarifications

### Session 2026-09-09

- Q: If the app cannot look up who belongs to the destination when the operator requests preview, should import still go ahead, or should it stop until that lookup works? → A: Block preview and execution until destination members can be looked up, with a human-friendly reason (for example missing permission or destination unavailable).
- Q: If someone is already in the destination as a guest, should the import assign them, or leave them as manual follow-up? → A: Assign guests who are already destination members, same as ordinary members.
- Q: When matching a CSV address to a destination member, which of that person’s addresses should count? → A: Match only work email and sign-in name (case-insensitive). Other aliases become manual follow-up.
- Q: If someone looked assignable at preview but can no longer be assigned when the import actually runs, should the rest of the import still continue? → A: Continue: create the task, assign remaining people, follow-up for anyone who can no longer be assigned.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Preview optional assignees from CSV (Priority: P1)

As an operator seeding a plan for a team, I can include an optional Assigned To column of work email addresses (or equivalent sign-in names), upload the file, and see who will be assigned to each new task before anything is created. People who cannot be assigned are visible as forthcoming manual follow-up, not as a reason to reject the file.

**Why this priority**: Preview is the safety gate. Operators must see who will land on the card versus who they will need to assign by hand, without the whole import being blocked by one bad address.

**Independent Test**: Upload files with no Assigned To column, with blank cells, with addresses that match destination members, and with addresses that do not, and confirm preview lists intended assignees and does not block execution solely because some people cannot be assigned.

**Acceptance Scenarios**:

1. **Given** a CSV whose first row includes the accepted Assigned To heading and whose cells contain addresses that match people in the destination, **When** the operator requests preview, **Then** each planned new task shows those people as assignees who will be applied on create.
2. **Given** a CSV with no Assigned To column, **When** the operator requests preview, **Then** preview succeeds as it does today and no assignees are required.
3. **Given** a CSV with an Assigned To column where some cells are empty, **When** the operator requests preview, **Then** those rows remain valid and are shown with no assignees.
4. **Given** a CSV cell that lists one or more addresses of which some match destination members and some do not, **When** the operator requests preview, **Then** the row remains valid, matched people are shown as assignees who will be applied, unmatched people are shown as needing manual follow-up, and execution is not blocked.
5. **Given** a CSV cell with several addresses separated by commas or semicolons (with surrounding spaces), **When** the operator requests preview, **Then** each trimmed address is treated as a separate person.
6. **Given** a CSV that contains at least one Assigned To address, and the app cannot look up destination members (missing permission, destination unavailable, or similar), **When** the operator requests preview, **Then** preview is blocked with a human-friendly reason, no task plan is offered as if people had been resolved, and execution remains blocked until lookup succeeds.

---

### User Story 2 - Create tasks with resolved assignees (Priority: P1)

As an operator confirming a valid preview, I want newly created tasks to have every person who could be assigned already on the card, so I am not assigning the whole team by hand.

**Why this priority**: This is the outcome that makes a team seeding spreadsheet feel complete. It can be demonstrated independently of documentation.

**Independent Test**: Confirm a preview that includes fully resolved assignees and inspect the created tasks to confirm those people are assigned.

**Acceptance Scenarios**:

1. **Given** a validated preview in which a row will create a new task and every listed address can be assigned, **When** the operator confirms execution, **Then** the created task has all of those people assigned.
2. **Given** a validated preview in which a listed person is already in the destination as a guest, **When** the operator confirms execution, **Then** that guest is assigned on the new task in the same way as an ordinary member.
3. **Given** a validated preview in which a row will create a new task and the Assigned To cell is empty or the column is absent, **When** the operator confirms execution, **Then** the created task has no assignees from this import.
4. **Given** a validated preview in which a row lists several people and only some can be assigned, **When** the operator confirms execution, **Then** the task is still created, every assignable person is assigned, and each person who could not be assigned is listed as a manual follow-up item naming the person, the task, and why they were not assigned.

---

### User Story 3 - Keep going when someone cannot be assigned (Priority: P1)

As an operator importing a real team list, I need one unknown or otherwise unassignable address to produce a clear manual follow-up item rather than failing the row or silently dropping that person.

**Why this priority**: Failing the whole import, or quietly omitting owners, both hurt first use. Goal already teaches “created, finish this by hand”.

**Independent Test**: Import a file that mixes destination members (including guests already in the destination) with unknown addresses and cases where assignment is not allowed, and confirm every new task is created and every unassigned person appears in the report.

**Acceptance Scenarios**:

1. **Given** an address that does not match anyone in the destination, **When** execution creates that task, **Then** the task is created without that person and the report lists a manual follow-up for that person, task, and reason.
2. **Given** an address for someone who is not already a destination member (including a guest who is not in the destination), **When** execution creates that task, **Then** the app does not add them to the destination membership, does not assign them, and lists a manual follow-up instead.
3. **Given** a person who is a destination member (ordinary or guest) but cannot be assigned because of permission or destination rules, **When** execution creates that task, **Then** the task is still created, any other assignable people on that row are still assigned, and the blocked person is listed as manual follow-up with a reason.
4. **Given** a person who was shown as assignable at preview but can no longer be assigned at execution (they left the destination, or assignment is now refused), **When** execution runs, **Then** the task is still created if it was a create, remaining assignable people on that row are still assigned, that person is listed as manual follow-up, and other rows are not blocked.
5. **Given** only unassignable addresses on a new-task row, **When** execution completes, **Then** the task is created unassigned and every listed person appears as manual follow-up.

---

### User Story 4 - Leave existing tasks unassigned from CSV (Priority: P2)

As an operator re-importing a file that includes tasks already in the plan, I need name-matched existing tasks to stay skipped. Assigned To in the CSV must not add, replace, or clear people on those cards.

**Why this priority**: Existing skip / already-exists behaviour must remain skip-only. Changing owners on live cards would be a silent overwrite and is explicitly out of scope.

**Independent Test**: Preview and execute a file that mixes new titles with titles that already exist, including Assigned To on both, and confirm existing matches are reported as already exists with no assignment change.

**Acceptance Scenarios**:

1. **Given** a CSV row whose task name matches an existing task in the destination, **When** preview runs, **Then** the row is treated as already exists (skip), even if the CSV includes Assigned To.
2. **Given** such a skipped row, **When** the operator confirms execution, **Then** the existing task’s assignees are not created, replaced, or cleared from the CSV, and no assignee manual follow-up is emitted solely to assign that existing card.
3. **Given** a mix of new and existing names in one file, **When** execution completes, **Then** only newly created tasks receive CSV assignees; skipped matches remain unchanged.

---

### User Story 5 - Document Assigned To on the full sample only (Priority: P3)

As an operator learning the file format, I can see Assigned To on the full-featured sample and in public CSV guidance, while the minimal sample still shows only the required heading. I am not offered a people-picker; the spreadsheet plus the report is enough.

**Why this priority**: Documentation and samples prevent operators from treating assignees as required, and they keep the simple first example small.

**Independent Test**: Review public CSV format guidance, any in-app accepted-field cues, and the minimal versus full sample files, and confirm Assigned To appears only on the full example and in the accepted-columns list.

**Acceptance Scenarios**:

1. **Given** public CSV format guidance, **When** an operator reads required and accepted columns, **Then** Assigned To is listed as optional, with work email or sign-in name values (not other aliases), support for several people in one cell, and the rule that people who cannot be assigned become manual follow-up rather than row errors.
2. **Given** the full-featured sample CSV (in docs and any in-app download), **When** an operator inspects it, **Then** it includes an Assigned To column with at least one example address and at least one example of several people in one cell.
3. **Given** the minimal sample CSV, **When** an operator inspects it, **Then** it still contains only the required Task Name heading and does not include Assigned To.
4. **Given** in-app guidance that names accepted columns, **When** an operator reads it, **Then** Assigned To is included as an optional accepted column.
5. **Given** the import wizard, **When** an operator prepares assignees, **Then** there is no people-picker control in this increment; CSV values and the report are the only assignment surfaces.

---

### Edge Cases

- Assigned To cell contains only spaces: treat as empty (row remains valid, no assignees).
- Addresses are trimmed of surrounding spaces before matching.
- Several people in one cell may be separated by commas or semicolons; empty fragments after splitting are ignored.
- Duplicate addresses on the same row are treated as one person.
- Matching is not case-sensitive for the local and domain parts of an address as operators type them.
- A CSV address matches a destination member only against that member’s work email and sign-in name. Other aliases, extra mailboxes, and display names do not match and become manual follow-up.
- If the same person appears twice on one row (same address twice, or work email plus sign-in name), they are assigned once.
- Display names without an address are not matched; they are unassignable and become manual follow-up.
- Quoted cells still parse if the inner text is a list of addresses.
- Semicolon-delimited files with an Assigned To column behave the same as equivalent comma-delimited files; list separators inside a cell remain comma or semicolon after the file has been read.
- Extra unknown columns continue to follow the existing ignore-extra-columns behaviour; Assigned To is an accepted column, not an extra column.
- Heading variants such as `Assignee`, `Email`, or `Assigned to` are not accepted in this increment unless they exactly match the canonical heading.
- The app never adds people to the destination membership in order to make assignment succeed.
- Guests who are already destination members are assignable, same as ordinary members. Guests who are not already members are treated like any other unknown person.
- If the destination refuses further assignees (for example a people-per-task limit), already-assigned people stay assigned, refused people become manual follow-up, and the task remains created.
- If a person was assignable at preview but cannot be assigned at execution, the import continues: the task is still created, remaining people are assigned, and the failed person is follow-up. Membership drift does not require a fresh preview by itself.
- Excel workbooks (`.xlsx`) remain out of scope for this increment; when workbook import arrives, it SHOULD reuse this Assigned To behaviour rather than inventing a second rule.
- In demonstration destinations without a live directory, created tasks still receive assignees who exist in that destination, and anyone who cannot be assigned is listed as manual follow-up, so the operator-visible outcomes match live use.
- Destination member lookup is required when the CSV contains at least one Assigned To address. If that lookup fails, it is a request-level failure: preview and execution are blocked with a human-friendly reason. Individual unknown addresses (when lookup succeeded) remain create-and-follow-up, not a block.
- Files with no Assigned To column, or with only empty Assigned To cells, do not require member lookup and keep today’s preview path.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST accept an optional canonical CSV heading `Assigned To` in addition to the existing accepted columns.
- **FR-002**: Absence of the Assigned To column MUST NOT cause validation failure.
- **FR-003**: An empty or whitespace-only Assigned To cell MUST be treated as omitted: the row MAY still be imported, and no assignees are applied for that row.
- **FR-004**: A non-empty Assigned To cell MUST be read as one or more people identified by work email or equivalent sign-in name, split on comma or semicolon, with each value trimmed. A value MUST match a destination member only when it equals that member’s work email or sign-in name, compared without regard to letter case. Other aliases MUST be treated as unassignable.
- **FR-005**: An address that cannot be assigned (unknown, not already a destination member, permission failure, destination limit, or not an address) MUST NOT produce a row-level validation error and MUST NOT block execution. Guest membership MUST NOT by itself make a person unassignable.
- **FR-006**: Preview MUST show, for each new-task row, which listed people will be assigned on create and which will become manual follow-up, so the operator can confirm before execution. When the CSV contains at least one Assigned To address, that classification MUST be based on a successful destination member lookup; the app MUST NOT invent assignable versus follow-up outcomes without that lookup.
- **FR-007**: When creating a new task from a valid row, the app MUST assign every listed person who can be assigned in the destination at create time, including guests who are already destination members.
- **FR-008**: When a listed person cannot be assigned on a newly created task, including when they looked assignable at preview but cannot be assigned at execution, the app MUST still create the task, MUST assign every other assignable person on that row, MUST emit a manual follow-up item that names the person, the task, and why they were not assigned, in the same report family as Goal follow-up, and MUST continue other rows. This MUST NOT by itself block execution or require a fresh preview.
- **FR-009**: The app MUST NOT add people to the destination membership automatically.
- **FR-010**: When a CSV row is matched to an existing task by name, the app MUST skip that row as already exists and MUST NOT add, replace, or clear assignees from the CSV, and MUST NOT emit assignee follow-up solely to change that existing card.
- **FR-011**: This increment MUST NOT add a people-picker or other interactive directory search in the wizard.
- **FR-012**: This increment MUST NOT add heading aliases or a column-mapping interface. Only the exact heading `Assigned To` is accepted here; alias work remains a separate story.
- **FR-013**: Public CSV format documentation MUST list Assigned To as optional, describe work email or sign-in name values (not other aliases), several people per cell, and the “cannot assign becomes manual follow-up” rule.
- **FR-014**: The full-featured sample CSV (documentation example and any in-app full sample) MUST include an Assigned To column. The minimal sample MUST remain Task Name only.
- **FR-015**: In-app accepted-column guidance MUST include Assigned To as optional.
- **FR-016**: Assigned To handling MUST work for both comma-delimited and semicolon-delimited CSV once the file is successfully read, and MUST preserve existing upload size limits and ignore-extra-columns behaviour.
- **FR-017**: If extra operator consent is required to look up destination members and assign them, that extra consent MUST be the smallest set needed for those two actions, and MUST be described for both hosted and self-hosted operators.
- **FR-018**: Changed parse, preview, skip, create-with-assignees, and follow-up behaviour MUST be covered by automated checks for fully resolved assignees (including a guest already in the destination), fully unresolved assignees, alias-only addresses that must not match, mixed cells, omitted assignees, name-matched existing tasks that must not be updated, and blocked preview when destination members cannot be looked up despite Assigned To addresses being present.
- **FR-019**: If destination members cannot be looked up when at least one Assigned To address is present, preview MUST fail as a request-level error with a human-friendly, actionable reason (for example missing permission or destination unavailable). Execution MUST remain blocked until a later preview succeeds after lookup works. This MUST NOT be reported as per-person manual follow-up.

### Key Entities

- **CSV task row**: One import row, now optionally carrying assignees alongside existing fields (task name, description, priority, bucket, goal, due date).
- **Assignee value**: One person identifier from a cell (work email or sign-in name) after splitting and trimming. Resolution uses only those two fields on each destination member.
- **Assignable person**: A listed person who is already a destination member (ordinary member or guest) and can be placed on the new task at create time.
- **Assignee follow-up**: A post-import manual action naming the person, the created task, and why assignment did not happen. Distinct from Goal follow-up, but presented in the same report.
- **Preview plan item**: The dry-run view of what will happen for a row, including assignees who will be applied and people who will need follow-up if the row creates a new task.
- **Existing-task skip**: A name match against a task already in the destination. Outcome remains already exists; CSV assignees are ignored for writes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can import a valid CSV that includes Assigned To and have 100% of newly created tasks show every listed person who could be assigned, without editing those cards afterwards for those people.
- **SC-002**: Files without an Assigned To column, and rows with a blank Assigned To, continue to preview and import successfully; assignees remain optional.
- **SC-003**: 100% of listed people who cannot be assigned still result in a created task (when the row is a create), plus a manual follow-up item for that person; 0% of those cases fail the row, fail the whole import, or silently omit the person from the report. This includes people who were assignable at preview but not at execution.
- **SC-004**: 100% of name-matched existing tasks remain skipped, with no assignee change, when the CSV includes Assigned To for those rows.
- **SC-005**: A first-time operator can confirm from public CSV guidance and the full sample, in under two minutes, that Assigned To is optional, which heading to use, how to list several people, and that unmatched people become follow-up rather than errors.
- **SC-006**: The minimal sample still demonstrates a valid file with only Task Name; operators are not forced to supply assignees to try the product.
- **SC-007**: Operators never need a people-picker in this increment: CSV plus the execution report is sufficient to complete or finish assignment by hand.
- **SC-008**: When destination members cannot be looked up and the file contains at least one Assigned To address, 100% of such preview attempts are blocked with a clear reason and 0% proceed to create tasks as if assignees had been resolved.

## Assumptions

- Source of this work is GitHub issue [#132](https://github.com/markheydon/import-to-planner/issues/132) (story, high priority, targeted at v1.0). Related but separate: heading aliases ([#127](https://github.com/markheydon/import-to-planner/issues/127)), description persistence ([#130](https://github.com/markheydon/import-to-planner/issues/130)), due dates ([#131](https://github.com/markheydon/import-to-planner/issues/131)), delimiter/BOM ([#133](https://github.com/markheydon/import-to-planner/issues/133)). Excel workbooks remain [#55](https://github.com/markheydon/import-to-planner/issues/55) and MUST NOT block this story; that work SHOULD reuse this Assigned To behaviour.
- Canonical heading is exactly `Assigned To` (capital A and T), matching the accepted-column style of `Task Name` and typical Planner export wording. Casing and synonym aliases (`Assignee`, `Email`) are deferred to alias work.
- People are identified only by work email or equivalent organisational sign-in name, not by display name and not by other aliases or extra mailboxes.
- Resolution is against members of the selected destination (the group or plan context the operator already chose). The app uses the signed-in operator’s access; it does not invent a second identity.
- Guests already in the destination are treated as assignable members. Anyone not already in the destination remains unassignable; they are never added automatically.
- Unlike invalid Priority or Due Date, an unassignable person is never a validation failure. Create-and-follow-up is the Goal-style pattern.
- Existing match-by-task-name-only and skip-already-exists rules from the core import workflow remain unchanged except that skipped rows MUST NOT receive an assignee write.
- Assignment applies in both demonstration and live destination modes in the same way from the operator’s point of view: created tasks receive people who exist and can be assigned there; others appear as follow-up.
- Public documentation updates and in-app accepted-column cues are in scope; no new wizard step is required solely for assignees.
- Extra operator consent, if any, is limited to looking up destination members and assigning them; broader directory search or group-management consent is out of scope. If that consent is missing, the operator sees a blocked preview (when addresses are present), not silent follow-up for everyone.
