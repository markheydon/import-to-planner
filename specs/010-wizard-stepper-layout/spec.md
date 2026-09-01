# Feature Specification: Import Wizard Stepper Layout

**Feature Branch**: `010-wizard-stepper-layout`

**Created**: 2026-09-01

**Status**: Draft

**Input**: User description: "Refactor the CSV-to-Planner import wizard UI on the Home page. Keep MudBlazor. Replace five always-expanded MudPaper step cards with a vertical MudStepper, collapsible MudExpansionPanels for setup steps 1-3, and a sticky right summary rail. Keep a persistent header for theme, signed-in email/tenant name, Sign out, and the commercial-only Profile link to /profile. Do not show the stepper during the commercial login gate or deleted-account retention gate. Preserve all existing workflow gating (canValidate, canExecute, preview staleness, commercial gates). Verify both Features:CommercialMode:Enabled true and false. Do not change ImportWorkflowCoordinator, commercial account use cases, or restyle /profile. Phased delivery: (1) stepper + single active pane, (2) collapsible setup + summary rail, (3) execution report polish, (4) optional responsive drawer. See GitHub issue #117 for full context."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Focus on one import step at a time (Priority: P1)

A signed-in user who may import tasks opens the Home page and sees a persistent header (theme, identity, sign out, and Profile when commercial hosting is on) plus a vertical step list. Only the current step’s working area is shown in the main pane. Completed steps show a tick and a short label summary in the step list. Locked later steps stay visible in the list but cannot be opened until earlier requirements are met. The user still completes location → plan → CSV → preview and confirm → report without changing what those steps allow or when they become available.

**Why this priority**: This is the minimum change that removes the long always-expanded card stack and delivers the core usability win. Later polish (collapsible setup, summary rail, report layout, narrow screens) can land on top of this skeleton.

**Independent Test**: Complete a full import with commercial hosting off and with it on (after a valid signed-in session). Confirm only one step’s detailed content is on screen at a time, the header chrome remains, and preview/execute still obey existing validity rules.

**Acceptance Scenarios**:

1. **Given** the user is signed in and allowed to use the import workflow, **When** they open Home, **Then** they see a vertical step list for the five steps (Select Planner location, Select plan, Upload CSV, Preview and confirm, and Report) and only the active step’s detailed content occupies the main working area.
2. **Given** a later step is still locked, **When** the user tries to open it from the step list, **Then** it remains unavailable and the current step’s content stays on screen.
3. **Given** an earlier step is complete, **When** the user views the step list, **Then** that step shows a completed indicator and a compact summary of the choice made (for example location name, plan name, or file name).
4. **Given** the user is on preview and confirm or report, **When** they look at the page, **Then** they are not forced to scroll through the full forms of earlier steps to reach the current action.
5. **Given** a valid preview exists and the confirm action is allowed today, **When** the user confirms the import from the preview-and-confirm step, **Then** the import still runs and the existing execution report still appears in the report step.
6. **Given** preview is stale, invalid, or execute is not yet allowed, **When** the user is on the preview-and-confirm step, **Then** the confirm action stays unavailable and the existing stale-preview or blocking guidance still appears.

---

### User Story 2 - Keep identity, theme, and commercial gates outside the wizard (Priority: P1)

The same Home page still hosts theme switching, signed-in email, tenant display name when available, Sign out, Sign in when not authorised, and (when commercial hosting is on) a Profile control that opens the existing profile page. Commercial-only blocking states — signed-out login gate and deleted-account retention — replace the wizard entirely. Self-hosted mode must not show Profile or those commercial gates.

**Why this priority**: The layout refactor must not hide identity or commercial access; those behaviours already exist and are independently valuable. Shipping a stepper that only works in one hosting mode would be a regression.

**Independent Test**: Review Home signed-in and signed-out, with commercial hosting on and off, including a deleted-account retention session. Confirm chrome and gates match today’s rules and that the stepper never appears on the two commercial blocking screens.

**Acceptance Scenarios**:

1. **Given** the user is signed in (commercial hosting on or off), **When** they view Home, **Then** theme, email, tenant name when Graph has resolved it, and Sign out remain visible in a persistent header above the wizard, not inside a step pane.
2. **Given** commercial hosting is on and the user is signed in with an active account, **When** they view Home, **Then** a Profile control is visible and opens the existing profile page.
3. **Given** commercial hosting is off, **When** they view Home, **Then** no Profile control is shown and no commercial login or deleted-account gate is shown.
4. **Given** commercial hosting is on and the user is signed out, **When** they open Home, **Then** they see the existing “Sign in to Import To Planner” gate and do not see the stepper, setup panels, or summary rail.
5. **Given** commercial hosting is on and the account is in deleted-account retention, **When** they open Home, **Then** they see “Access is paused” with retention expiry, Restore account, Open profile, and Sign out, and they do not see the wizard.
6. **Given** a first successful commercial sign-in that today shows an account-created success message, **When** the wizard is shown, **Then** that success message still appears above the wizard, not as a duplicate inside a step.
7. **Given** the user follows Profile or Open profile from Home, **When** they land on the profile page, **Then** existing profile behaviour is unchanged (details, delete, retention, and redirect away when commercial hosting is off).

---

### User Story 3 - Collapse completed setup and keep context in a summary rail (Priority: P2)

Once the stepper skeleton exists, a user who has chosen location, plan, and CSV can collapse those three setup steps by default and still see the current import context in a sticky summary on wide layouts: location, plan, CSV file name, and preview or execution status. When a plan or preview is available, the summary includes the existing “Open in Planner” destination. The user can expand a setup step to review or change it; changing inputs still invalidates preview or execute exactly as today.

**Why this priority**: This is the second delivery slice. It further shortens the page and restores at-a-glance context that a single-pane stepper would otherwise hide. It is independently testable once Story 1 is in place.

**Independent Test**: Progress through setup, then inspect collapse behaviour and the summary contents. Change a completed setup value and confirm preview/execute gating still matches today’s rules.

**Acceptance Scenarios**:

1. **Given** steps 1–3 are complete and the user is on preview and confirm or report, **When** they view the main pane, **Then** the three setup steps are collapsed by default and do not show their full forms.
2. **Given** a setup step is collapsed, **When** the user expands it, **Then** they can review or change the previous choice and the rest of the workflow still respects existing lock and validity rules.
3. **Given** a wide viewport and the wizard is visible, **When** the user has made any import choices, **Then** a sticky summary rail shows the current location, plan, CSV file name (when chosen), and preview or execution status.
4. **Given** a plan or a successful preview is available, **When** the user views the summary, **Then** they can open the plan in Planner from that rail using the same destination the page already offers.
5. **Given** the user changes location, plan, or file after a preview, **When** they view preview and confirm or report, **Then** execute remains blocked until a fresh valid preview exists, as it does today, and the wizard returns focus to the affected setup step when appropriate.

---

### User Story 4 - Scan import results without a long report stack (Priority: P3)

After a successful or partial import, the user can scan outcome totals (created, skipped, manual follow-up, errors) before opening detailed tables. Manual follow-up items remain distinguishable by action type. Preview can optionally show compact counts before the full preview tables. Existing report meaning and data must not change.

**Why this priority**: This is polish on top of a shorter wizard. It is independently valuable for post-import comprehension but is not required to prove the stepper layout.

**Independent Test**: Run a preview and an import that includes mixed outcomes (including manual follow-up if the sample data produces any) and confirm totals appear before detailed tables, with no change to what those outcomes mean.

**Acceptance Scenarios**:

1. **Given** an import has finished, **When** the user opens the report summary, **Then** they see created, skipped, manual, and error counts before any detailed tables.
2. **Given** the report includes manual follow-up items, **When** the user views those items, **Then** action type is visually distinct (for example coloured labels) without changing the underlying classification.
3. **Given** a successful preview, **When** the user is on the preview-and-confirm step, **Then** they may see compact counts of planned actions before the full preview tables; expanding or scrolling still reveals the same tables as today.

---

### User Story 5 - Use the wizard on a narrow screen (Priority: P4)

On small viewports, the user can still complete the import: the step list may become a drawer or a compact stacked/horizontal stepper, and theme, Profile (when commercial), and Sign out remain reachable. Touch targets and keyboard order stay usable. This slice is optional if Stories 1–3 already remain usable when the layout stacks.

**Why this priority**: Desktop is the primary import context; narrow-layout treatment is an optional follow-on so the three-column layout does not block the MVP.

**Independent Test**: Resize or emulate a small viewport, complete or progress the wizard, and confirm header actions remain in reach and steps remain operable.

**Acceptance Scenarios**:

1. **Given** a narrow viewport and the wizard is visible, **When** the user progresses through steps, **Then** they can still identify the current step and complete available actions without horizontal scrolling of the page.
2. **Given** a narrow viewport, **When** the user looks for theme, Sign out, and Profile (commercial only), **Then** those controls remain reachable, wrapping if needed as they do today.
3. **Given** keyboard or assistive-technology use on a narrow layout, **When** the user moves through the page, **Then** step controls, theme, Profile, and Sign out remain in a sensible focus order.

---

### Edge Cases

- Commercial login gate and deleted-account retention must hide the entire wizard (step list, working pane, summary rail), not only the centre pane.
- Unsupported account, missing admin consent, and tenant-mismatch conditions must still block or warn as they do today; the new layout must not hide those messages inside an inactive step.
- First-sign-in commercial success must not be duplicated as both a global and a step-level alert for the same message.
- When the organisation display name cannot be resolved, the header still shows email and does not invent a placeholder name.
- When the user has not yet chosen a location, plan, or file, the summary rail shows empty or “not yet chosen” context rather than stale values from a previous session on the same page load.
- After import, returning to an earlier step to start another import (if the page already allows it) must not leave the summary or stepper stuck on the previous execution state incorrectly.
- Narrow viewports must not trap identity or Profile behind the step list.
- Screen-reader users must be able to determine current, complete, and locked steps without relying on colour alone.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Home MUST present the import workflow as a vertical step list with a single active working pane whenever the user is allowed to import, replacing the current pattern of five always-expanded step cards stacked on one page.
- **FR-002**: The five steps MUST remain the existing sequence with these titles: Select Planner location, Select plan, Upload CSV, Preview and confirm, and Report. Step 4 combines preview generation, preview review, and confirm import; step 5 shows the execution report only.
- **FR-003**: Only the active step’s detailed content MUST occupy the main working area; inactive step forms MUST NOT consume vertical space as full expanded cards.
- **FR-004**: Completed steps MUST show a completion indicator and a compact summary in the step list.
- **FR-005**: Steps MUST remain locked until existing prerequisites are met; users MUST NOT be able to skip ahead of current availability rules (including preview-available and execute-available rules).
- **FR-006**: Users MUST still be able to return to an earlier unlocked step to review or change inputs; doing so MUST preserve existing preview-staleness and execute-blocking behaviour.
- **FR-007**: A persistent header MUST remain above the wizard and MUST include theme selection (auto, light, dark), signed-in email, tenant display name when available, Sign out when signed in, and Sign in when not authorised.
- **FR-008**: When commercial hosting is enabled and the user is signed in with an active account, Home MUST show a Profile control that navigates to the existing profile page.
- **FR-009**: When commercial hosting is disabled, Home MUST NOT show Profile, the commercial login gate, the deleted-account gate, or the first-sign-in account-created alert.
- **FR-010**: When commercial hosting is enabled and the user is signed out, Home MUST show the existing login gate and MUST NOT render the step list, working pane, or summary rail.
- **FR-011**: When commercial hosting is enabled and the account is in deleted-account retention, Home MUST show the existing paused-access experience (including Restore account and Open profile) and MUST NOT render the wizard.
- **FR-012**: First successful commercial sign-in MUST still show the existing account-created success message above the wizard, without duplicating that message inside a step.
- **FR-013**: Existing blocking and warning conditions (including admin consent, unsupported account, tenant mismatch, and stale preview) MUST remain visible and MUST continue to prevent preview or execute when they do today.
- **FR-014**: Setup steps 1–3 MUST be collapsible, collapsed by default once complete when the user is on a later step, and expandable so the user can review or change those choices (Story 3).
- **FR-015**: On wide viewports, Home MUST show a sticky summary of current location, plan, CSV file name, and preview or execution status while the wizard is visible (Story 3).
- **FR-016**: When a plan or preview is available, the summary MUST offer the existing Open in Planner action (Story 3).
- **FR-017**: After import, the execution report MUST present created, skipped, manual, and error counts before detailed tables, without changing what those outcomes mean (Story 4).
- **FR-018**: Preview MAY show compact planned-action counts before full preview tables; those tables MUST still be available in the preview-and-confirm step (Story 4).
- **FR-019**: On narrow viewports, the step list MAY collapse to a drawer or compact stacked layout; theme, Sign out, and Profile (when shown) MUST remain reachable (Story 5, optional).
- **FR-020**: Keyboard and screen-reader users MUST be able to determine step state, move between available steps, and reach theme, Profile, and Sign out.
- **FR-021**: The same success message or informational warning MUST NOT appear twice (once globally and once inside the active step) for a single condition.
- **FR-022**: The import workflow’s availability rules, commercial account create/delete/restore/retention behaviour, and the profile page’s existing screens MUST remain unchanged except where Home layout must continue to host already-specified chrome and gates.
- **FR-023**: This feature MUST be verified with commercial hosting both enabled and disabled.

### Key Entities

- **Import wizard layout**: The Home import experience when the user is allowed to import — header, step list, active working pane, and (from Story 3) summary rail.
- **Workflow step**: One of the five existing import steps, with states complete, active, upcoming/locked, matching existing availability rather than new business rules.
- **Import context summary**: The current location, plan, CSV file, and preview or execution status shown beside the working pane on wide layouts.
- **Commercial access gate**: Home states that replace the wizard (signed-out commercial login; deleted-account retention) rather than sitting inside a step.
- **Identity chrome**: Theme, email, tenant name, Sign out/Sign in, and commercial-only Profile, always outside the step panes when shown.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After completing an import, a reviewer comparing the report step with today’s always-expanded five-card layout finds the page materially shorter — specifically, earlier step forms are not fully expanded below the header — in 100% of reviewed runs.
- **SC-002**: In a screenshot of any in-progress import, a reviewer who does not know the product can correctly identify the current step, completed steps, and locked steps in at least 9 out of 10 samples.
- **SC-003**: Users can still complete location → plan → CSV → preview and confirm → report on the first attempt when inputs are valid, with no extra mandatory screens compared with today.
- **SC-004**: Preview remains blocked until existing preview-ready conditions are met, and confirm remains blocked until existing execute-ready conditions are met, in 100% of automated workflow scenarios covering happy path, stale preview, and commercial blocking states.
- **SC-005**: With commercial hosting off, 100% of reviewed Home sessions show no Profile control and no commercial login or deleted-account gate, while the import wizard remains available after normal sign-in.
- **SC-006**: With commercial hosting on, signed-out and deleted-account sessions never show the wizard; signed-in active-account sessions always show Profile in the header and can still open the existing profile page.
- **SC-007**: Theme switching (auto, light, dark) continues to apply to Home for 100% of manual checks in both hosting modes.
- **SC-008**: Duplicate global and step-level alerts for the same success or information message are absent on the primary import path (including first commercial sign-in).
- **SC-009**: Keyboard-only users can reach theme, Sign out, Profile when shown, and the current step’s primary action without a mouse.
- **SC-010**: On a wide desktop viewport, once Story 3 is delivered, location, plan, file, and preview/execution status remain visible in the summary without scrolling away from the active step.

## Assumptions

- This is a presentation refactor of Home. Import orchestration, preview and execute eligibility, Graph/CSV behaviour, and commercial account use cases stay as they are; Home only changes how those states are shown.
- The existing UI component library remains the presentation toolkit; this feature does not introduce a second styling system or a page-level rewrite of the profile route.
- Desktop (wide) layout is the primary target for Stories 1–3. Story 5 is optional if stacking already keeps the wizard usable on small screens.
- Delivery may land as incremental slices aligned to the user stories (stepper skeleton, then collapsible setup and summary, then report polish, then optional narrow layout).
- **Amendment (2026-09-01, dogfooding)**: Step 4 and step 5 titles and pane responsibilities were refined after stepper testing. Preview and confirm import now share step 4; step 5 is report-only. Primary actions remain **Preview import** and **Confirm import** on step 4. Workflow gating (`canValidate`, `canExecute`, staleness) is unchanged.
- Header chrome may stay in the current title row or move to a dedicated top bar, provided it stays full-width above the wizard and remains reachable.
- End-user help screenshots and copy under public docs may need a later update if the visible layout changes; that documentation refresh is follow-on unless wording on Home itself changes.
- Source of product intent: GitHub issue #117. Related commercial behaviour is already specified in commercial account features; this spec does not reopen those rules.

## Out of Scope

- Changing import planning, execution, or commercial account create/delete/restore/retention logic.
- Restyling the profile page to match the new wizard.
- Migrating the UI to a different visual framework.
- New import steps, new CSV fields, or new Planner capabilities.
- A marketing site or a larger commercial account dashboard.

## Traceability

- GitHub issue: [#117](https://github.com/markheydon/import-to-planner/issues/117)
- Preserves Home identity and commercial gate behaviour specified for commercial user accounts and in-process commercial hosting.
