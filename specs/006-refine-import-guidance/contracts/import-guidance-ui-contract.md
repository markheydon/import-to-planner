# Import Guidance UI Contract

## Purpose

This contract defines the user-facing structure and wording boundaries for the
home page import workflow after the copy and presentation refinement. It is a UI
contract rather than an API contract because the feature affects the primary
interactive surface exposed to end users.

## Workflow Structure Contract

The page continues to expose exactly five workflow cards in this order:

1. `Select Planner location`
2. `Select plan`
3. `Upload CSV`
4. `Preview import`
5. `Confirm and import`

Rules:

- Visible headings must not include redundant `Step X ·` text.
- Sequence is still communicated through the existing numbered circle treatment
  and card ordering.
- No separate navigation bar or top stepper is introduced in this feature.

## Step State Contract

Each workflow card must render one of three user-visible states:

- `Current`: the next actionable step for the user.
- `Completed`: a step whose prerequisite action has been satisfied.
- `Upcoming`: a later step that is not yet actionable.

Rules:

- `Current` must be visually emphasised.
- `Completed` must show a completion cue such as a tick icon.
- `Upcoming` must appear subdued relative to the current step.
- State cues must remain understandable when cards stack vertically on smaller
  screens.

## Introduction Guidance Contract

The top-of-page guidance must remain compact and include these elements:

- A short overview of the CSV-to-Planner flow.
- A required-field cue naming `Task Name`.
- An accepted-fields cue naming `Task Name`, `Description`, `Priority`,
  `Bucket`, and `Goal`.
- A manual-follow-up note naming one concrete example only, with goals as the
  default example.

Rules:

- The section must set expectations without replacing future public
  documentation.
- Wording must be friendly, concise, and in UK English.
- The manual-follow-up note must not read as a full limitations list.

## Action Label Contract

Primary action labels in the main workflow must use sentence case and plain,
action-oriented wording.

Expected labels:

- `Preview import`
- `Confirm and import`

Rules:

- Labels must describe the user outcome clearly.
- Validation, preview, confirmation, and execution semantics must remain
  unchanged behind the updated labels.

## Theme Token Contract

The light theme must use `#d1d1d1` for the shared neutral border/line token used
for outlined surfaces and dividers touched by this page.

Rules:

- No broader palette refresh is part of this feature.
- Dark-theme border tokens remain unchanged unless needed to prevent regression.

## Test Contract

Implementation must preserve or extend focused checks in:

- `HomePageSmokeTests`
- `HomePageWorkflowTests`
- `ArchitectureComplianceTests`

Required evidence:

- The page still renders five steps.
- The updated headings and action labels appear in rendered markup.
- Step progression semantics remain intact.
- Guidance logic does not rely on brittle status-message string scanning.
