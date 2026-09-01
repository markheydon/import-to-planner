# Data Model: Import Wizard Stepper Layout

## Scope

This feature does not add Domain entities, Application request/response types, or persistence. All new fields are **Web-owned presentation state** on the Home page, derived from existing `WorkflowCoordinationState` plus a viewed-step index.

Existing workflow entities (container, plan, CSV content, planning preview, execution report, commercial access flags) are unchanged. See specs 001, 008, and 009 for those models.

## Entities

### WizardLayoutMode

- Purpose: Which Home chrome is visible.
- Values:
  - `ImportWizard` — user may import; header + stepper + working pane (+ summary rail from Story 3).
  - `CommercialLoginGate` — signed-out commercial; header only (login copy + Sign in).
  - `CommercialDeletedAccountGate` — retention; header + paused-access actions; no wizard.
- Derivation: existing `showCommercialLoginGate` / `showCommercialDeletedAccountGate`. No new commercial rules.

### ViewedWorkflowStep

- Purpose: Which step’s detailed form occupies the working pane.
- Fields:
  - `Index`: 1–5, matching the existing step order.
  - `Title`: step titles (Select Planner location, Select plan, Upload CSV, Preview and confirm, Report).
  - `State`: `Current`, `Completed`, or `Upcoming` from existing `GetWorkflowStepState` (still based on completeness and the computed next incomplete step, not solely on `Index`).
  - `Locked`: existing `IsStepLocked`.
  - `Summary`: existing `GetStepSummary` when complete.
- Validation:
  - User cannot set `Index` to a locked step.
  - When the next incomplete step advances, `Index` follows unless the user is inspecting an earlier unlocked step (implementer: auto-advance on completion of the viewed step; do not yank the user away while they edit an earlier step).
  - When all steps are complete, `Index` remains 5 so the report stays visible.

### ImportContextSummary

- Purpose: Sticky rail contents (Story 3).
- Fields:
  - `LocationLabel` — formatted container or empty/not chosen.
  - `PlanLabel` — formatted plan or empty/not chosen.
  - `CsvFileName` — selected file name or the existing “no file” wording.
  - `PreviewStatus` — none / ready / stale / validation errors (derived from preview, `isPreviewStale`, parse errors).
  - `ExecutionStatus` — none / succeeded / succeeded with warnings (derived from execution report).
  - `PlannerUrl` — optional; same plan URLs already used on preview and report.
- Validation: never show a previous page-load’s values; empty until the current session has a choice. Do not invent a tenant or plan name.

### SetupPanelExpansion

- Purpose: Expansion state for steps 1–3 (Story 3).
- Fields:
  - `StepIndex`: 1, 2, or 3.
  - `IsExpanded`: true when `viewedStep == StepIndex`; false by default when that step is complete and `viewedStep` is 4 or 5.
- Validation: expanding a panel to change inputs must still run existing change handlers (stale preview, lock of later steps).

### IdentityChrome

- Purpose: Persistent header controls (unchanged meaning).
- Fields: theme mode, email, tenant display name (optional), Sign in / Sign out, Profile visibility (`CommercialModeOptions.Enabled` and authorised).
- Validation: Profile hidden when commercial mode is off. Email shown without a fake tenant name when the name is missing.

## State transitions

```text
[Load Home]
    → CommercialLoginGate | CommercialDeletedAccountGate | ImportWizard

ImportWizard:
    viewedStep := ActiveStep ?? 5
    user selects unlocked step N → viewedStep := N
    user completes viewed setup step → viewedStep := ActiveStep (stays on step 4 until import runs)
    user confirms import on step 4 → viewedStep := 5 (report)
    locked step click → no change

Preview generated → step 4 shows preview; confirm enabled when canExecute
Execute → steps 4 and 5 complete; viewedStep advances to 5
Change location/plan/CSV on step 4 or 5 → return viewedStep to affected setup step; existing stale flags; execute remains unavailable until fresh preview
```

## Relationships

- `ViewedWorkflowStep` **reads** `WorkflowCoordinationState` (selections, preview, execution, busy, stale).
- `ImportContextSummary` **reads** the same state; it does not write coordinator fields.
- Commercial gates **suppress** viewed-step UI entirely.

## Persistence

None. State lives for the Blazor circuit / page instance, as today.
