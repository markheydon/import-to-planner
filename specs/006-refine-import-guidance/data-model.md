# Data Model: Refine Import Guidance

## Scope

This feature does not add domain entities or persistence models. It introduces a
set of Web-owned presentation concepts that shape the home page wording, step
state rendering, and top-of-page guidance.

## Entities

### WorkflowStepPresentation

- Purpose: Represents how one workflow card is described and visually treated on
  the home page.
- Fields:
  - `Order`: fixed sequence position from 1 to 5.
  - `Title`: concise action-oriented step title.
  - `State`: `Current`, `Completed`, or `Upcoming`.
  - `BadgeContent`: step number for active/upcoming steps or completion cue for
    completed steps.
  - `Summary`: optional short completion summary already shown beneath the title.
  - `PrimaryActionLabel`: optional sentence-case action label associated with the
    step.
- Validation rules:
  - The workflow always contains exactly five steps.
  - Titles must not repeat `Step X` in the visible heading text.
  - `State` must be derivable from existing workflow coordination state rather
    than new server or storage data.
  - Completed state must remain distinct from upcoming state on both desktop and
    mobile layouts.

### IntroGuidanceSummary

- Purpose: Holds the compact introduction content displayed above the workflow.
- Fields:
  - `Heading`: concise page-level heading that frames the workflow.
  - `OverviewText`: short explanation of what the import flow does.
  - `RequiredFieldsText`: compact cue naming the minimum required CSV field.
  - `AcceptedFieldsText`: compact cue naming all accepted CSV fields.
  - `ManualFollowUpText`: short expectation-setting note naming one manual
    follow-up example.
- Validation rules:
  - The content must fit comfortably in the existing header region without
    displacing the workflow below the fold more than necessary.
  - The guidance must remain readable without becoming a tutorial or release
    notes block.
  - User-facing wording must use UK English.

### CsvFieldReference

- Purpose: User-facing representation of the supported CSV schema.
- Fields:
  - `RequiredField`: `Task Name`.
  - `AcceptedFields`: `Task Name`, `Description`, `Priority`, `Bucket`, `Goal`.
  - `ManualFollowUpExample`: `Goal`.
- Validation rules:
  - Values must match the current parser contract.
  - The required field must be clearly distinguishable from the broader accepted
    field list.
  - The manual follow-up example must not be phrased as the only possible manual
    action.

### ThemeBorderToken

- Purpose: Shared neutral border/line colour used by outlined surfaces in the
  light theme.
- Fields:
  - `LinesDefault`: `#d1d1d1`.
  - `Divider`: `#d1d1d1`.
- Validation rules:
  - The token change must preserve existing contrast and visual consistency.
  - No unrelated palette tokens should change as part of this feature.

## Relationships Overview

- `WorkflowStepPresentation` uses existing workflow coordination state to map UI
  state into presentational cues.
- `IntroGuidanceSummary` includes a `CsvFieldReference` snapshot so the page can
  describe the current schema accurately.
- `ThemeBorderToken` affects the outlined surfaces that contain
  `WorkflowStepPresentation` and `IntroGuidanceSummary`.

## State Transitions

### Workflow presentation state

1. The page loads with the first step as `Current` and later steps as `Upcoming`.
2. As the user completes each prerequisite selection, earlier steps become
   `Completed` and the next eligible step becomes `Current`.
3. If Planner state invalidates the preview, the current actionable step remains
   visually highlighted while downstream actions return to an upcoming or locked
   state.

### Introduction guidance state

1. The introduction renders immediately on page load.
2. The CSV guidance stays static unless the underlying supported schema changes
   in a later feature.
3. The manual follow-up note remains concise and does not expand dynamically
   based on runtime data.

## Mapping Boundaries

- The Web layer owns all wording, step labels, and visual state mapping.
- Application and Domain continue to own workflow behaviour and planner data,
  but they do not own user-facing copy for this feature.
- Tests consume these presentation concepts indirectly through rendered markup
  and CSS-state expectations.
