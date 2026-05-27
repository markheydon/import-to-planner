# Feature Specification: Refine Import Guidance

**Feature Branch**: `006-refine-import-guidance`

**Created**: 2026-05-27

**Status**: Draft

**Input**: User description: "We need a very minor tweak of some of the language used in the app to make it a bit more user-friendly. Here's what I'm thinking.

Remove the redundant “Step X” text from each section header and rely on the existing numbered circles to indicate sequence (e.g. change “Step 1 · Select Container” to “Select Container”), keeping the layout clean while preserving clarity; keep the current card-based flow but improve progression visibility by visually distinguishing states (current step highlighted, completed steps with a tick icon, upcoming steps greyed out); simplify and modernise button labels to sentence case and more user-friendly phrasing (e.g. “Preview Import” instead of “VALIDATE AND PREVIEW”, “Confirm & Import” instead of “CONFIRM AND EXECUTE”); slightly refine wording for clarity and tone (e.g. “Create your target plan in Planner first, then select it here”); optionally make step titles more user-friendly rather than internal-facing (e.g. “Select Container” → “Select Planner Location” or “Where should the tasks go?”); keep the overall minimalist layout but prepare for future expansion by ensuring the structure can support navigation later, without adding a navigation bar yet.

We also need to enhance the 'introduction' bit at the top to help manage user expectations a little. So for example there's currently no sample .csv file or even an indication of what field names the user needs in the imported .csv. I'm thinking two would be good, a simple 'required' fields one, plus one with all the accepted fields. Which then brings up another point in that some actions are manual as the API currently doesn't support all actions, so again, it should be clear, in user-friendly language, that while you can include these missing bits they are manual actions after the fact. We don't want a bloated introduction though just the basics. There's another 'feature' coming after this one to introduce a GitHub Pages served public-facing docs site so we can be much more verbose in that content.

Also please fix a very minor mismatch in fluent-like theming we have in place, i.e. the 'Border/lines' is slightly off.

Token	Current value	Fluent 2 spec	Gap
Primary blue	#0f6cbd	#0f6cbd	✅ exact match
Background	#f3f2f1	#f3f2f1	✅ exact match
Background gray	#edebe9	#edebe9	✅ exact match
Font family	Segoe UI, Segoe UI Variable	Segoe UI Variable	✅ correct order
Success	#107c10	#107c10	✅ exact match
Error	#a4262c	#a4262c	✅ exact match
Border/lines	#e1dfdd	#d1d1d1	🟡 close, slightly warm"

## Clarifications

### Session 2026-05-27

- Q: Which step-title style should the workflow use? → A: Use concise action titles with explicit Planner context, for example: "Select Planner location", "Select plan", "Upload CSV", "Preview import", and "Confirm and import".
- Q: How specific should the manual-follow-up guidance be? → A: Mention one concrete example in plain language, such as goals needing manual follow-up in Planner, without listing every limitation.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand the workflow at a glance (Priority: P1)

As a first-time user, I can quickly understand what each step does, where I am in the import flow, and what I need to do next without reading internal or overly technical labels.

**Why this priority**: The core value of this feature is making the import journey easier to follow before any data is uploaded or imported. If the workflow feels unclear, users are more likely to hesitate or make mistakes.

**Independent Test**: Can be fully tested by opening the import page and verifying that step headings, progression states, and action labels make the sequence obvious before any import is performed.

**Acceptance Scenarios**:

1. **Given** a user opens the import page for the first time, **When** they view the workflow cards, **Then** each step heading uses concise action-oriented wording with explicit Planner context and without repeating the step number in the title text.
2. **Given** the workflow shows multiple steps, **When** one step is active and others are either complete or not yet available, **Then** the current step is visually highlighted, completed steps are marked as complete, and upcoming steps are visibly de-emphasised.
3. **Given** a user reaches the preview and import actions, **When** they read the action buttons, **Then** the labels use sentence case and plain-language phrasing that clearly describes the action outcome.

---

### User Story 2 - Know the CSV expectations before uploading (Priority: P2)

As a user preparing a CSV, I can see a concise explanation of the required field and the full list of accepted fields so I know what the app expects before I upload a file.

**Why this priority**: Users need enough guidance to prepare a valid CSV without forcing them into trial and error, but the page still needs to remain compact.

**Independent Test**: Can be fully tested by reviewing the introduction area and confirming it explains the required CSV field, the optional accepted fields, and where users can find fuller documentation later.

**Acceptance Scenarios**:

1. **Given** a user is reading the introduction before uploading a file, **When** they review the guidance, **Then** they can identify the minimum required CSV field needed for a valid import.
2. **Given** a user wants to use additional supported fields, **When** they review the same introduction area, **Then** they can identify the complete accepted field list without leaving the page.
3. **Given** the page introduces CSV guidance, **When** the information is presented, **Then** it remains brief and avoids replacing the planned longer-form documentation site.

---

### User Story 3 - Understand what still needs manual follow-up (Priority: P3)

As a user planning an import, I can understand which imported details may still require manual follow-up in Planner so I am not surprised by post-import work.

**Why this priority**: Clear expectation-setting reduces confusion and support friction, but it is secondary to making the main workflow understandable.

**Independent Test**: Can be fully tested by reviewing the introduction and execution guidance to confirm it explains, in plain language, that some imported details may generate manual follow-up actions after import.

**Acceptance Scenarios**:

1. **Given** a user reads the introduction before uploading a file, **When** they review the import guidance, **Then** they are told that some supported CSV details may still need manual completion in Planner after import, including a brief example such as goals.
2. **Given** a user includes fields that cannot be completed automatically, **When** the app describes the import process, **Then** it sets the expectation that the app will identify those items for manual follow-up rather than implying full automation, while naming only one brief example instead of listing every limitation.
3. **Given** the page is updated for clearer wording, **When** the user reads the page from top to bottom, **Then** the tone remains friendly and concise rather than dense or documentation-heavy.

### Edge Cases

- What happens when a user lands on the page on a smaller screen where step cards stack vertically and the state styling must still clearly indicate current, completed, and upcoming steps?
- How does the page behave when a user returns after partially completing earlier steps and the workflow needs to show both completed and currently active states without ambiguity?
- What happens when the introduction needs to mention accepted CSV fields and manual follow-up guidance in limited space without pushing key actions too far down the page?
- How does the interface distinguish incomplete future steps if the user has not yet selected a Planner location or plan?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST replace step headings that currently repeat the step number in text with concise action-oriented titles while preserving the existing sequential order indicator elsewhere in the layout.
- **FR-002**: The system MUST visually distinguish the current workflow step, completed workflow steps, and upcoming workflow steps using consistent state treatments that remain understandable without additional explanatory text.
- **FR-003**: The system MUST show completed workflow steps with a clear completion indicator and preserve a subdued appearance for steps that are not yet available.
- **FR-004**: Users MUST be able to identify the purpose of each step from its title without relying on internal implementation terms, using titles with explicit Planner context such as "Select Planner location", "Select plan", "Upload CSV", "Preview import", and "Confirm and import".
- **FR-005**: The system MUST update primary workflow action labels to sentence case, user-friendly phrasing, and wording that clearly communicates the outcome of previewing or starting an import.
- **FR-006**: The system MUST refine supporting instructional copy throughout the main import flow so it uses plain language and a friendlier tone while preserving the existing card-based workflow.
- **FR-007**: The system MUST expand the introduction area with brief guidance that explains the minimum required CSV field for a valid import.
- **FR-008**: The system MUST expand the same introduction area with a concise list of all CSV fields the app accepts today.
- **FR-009**: The system MUST explain in the introduction, in user-friendly language, that some imported details may require manual follow-up in Planner because they cannot yet be completed automatically, and it MUST include one brief concrete example such as goals.
- **FR-010**: The system MUST keep the introduction compact enough that the core import workflow remains visible without feeling replaced by long-form documentation.
- **FR-011**: The system MUST preserve the existing card-based structure while organising workflow state and headings in a way that can support future step navigation without introducing a navigation bar in this change.
- **FR-012**: The system MUST align the light-theme border and line colour token with the intended Fluent-like neutral border value used by the design direction.
- **FR-013**: The system MUST keep the rest of the existing visual language, including core colour palette and minimalist layout, unchanged apart from the requested copy, state, and border refinements.

### Key Entities *(include if feature involves data)*

- **Workflow Step Presentation**: The visible representation of a step in the import journey, including its title, state, summary treatment, and action prompts.
- **Introduction Guidance Summary**: The compact guidance area at the top of the page that sets expectations for CSV preparation, supported fields, and manual follow-up work.
- **CSV Field Reference**: The user-facing description of the current CSV schema, including the required field and the additional accepted optional fields.
- **Manual Follow-up Expectation**: The explanation shown before import that some data can be recognised and surfaced for follow-up even when it cannot be completed automatically.
- **Theme Border Token**: The shared neutral line colour used for outlined surfaces and dividers in the light theme.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In usability review, users can correctly identify the current, completed, and upcoming workflow steps from the page state alone without needing a written legend.
- **SC-002**: First-time users can identify the required CSV field and the full accepted field list from the introduction area in under 30 seconds.
- **SC-003**: At least 90% of users in acceptance testing can explain, before importing, that some supported details may still require manual completion in Planner afterwards.
- **SC-004**: Reviewers can confirm that all primary step actions use sentence case and plain-language labels, with no remaining all-caps workflow button labels on the main import journey.
- **SC-005**: Visual review confirms the updated light-theme border and line colour matches the agreed neutral border token across the affected outlined surfaces.

## Assumptions

- The current supported CSV schema remains unchanged for this feature: `Task Name` is required, while `Description`, `Priority`, `Bucket`, and `Goal` remain accepted optional fields.
- The app continues to use the existing card-based step sequence and does not introduce a separate navigation component in this change.
- Step headings will use concise action titles with explicit Planner context rather than question-style or internal system terminology.
- Manual follow-up guidance will stay concise by naming at most one concrete example, with goals as the default example, rather than enumerating all current automation limitations.
- Manual follow-up guidance is limited to concise expectation-setting on the main page because fuller documentation will be delivered in a later feature.
- This feature is limited to wording, presentation-state clarity, and a small theme-token correction rather than broader workflow redesign.
