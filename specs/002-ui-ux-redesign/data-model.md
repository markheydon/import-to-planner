# Data Model: UI/UX Redesign — Stepped Import Workflow

**Feature**: 002-ui-ux-redesign  
**Date**: 2026-05-11

## Overview

This is a pure front-end UI feature. No new domain or application-layer entities are introduced. All existing domain models (`PlannerContainer`, `PlannerPlan`, `ImportPlanPreview`, `ImportExecutionResult`, `ImportTaskPlanItem`, `ImportValidationError`, `ManualAction`) remain unchanged.

The data model for this feature is limited to **UI view-state** that lives within the `Home.razor` component. No new shared models, DTOs, or services are required.

---

## UI Step State Model

The stepped layout requires the component to track derived state for each step. This is expressed as computed C# properties (not a new class), derived from existing state fields already present in `Home.razor`.

### Step definitions

| Step index | Name | Active when (derived condition) |
| ---------- | ---- | ------------------------------- |
| 0 | Select Container | Always active on page load |
| 1 | Select Plan | `selectedContainer is not null`¹ |
| 2 | Upload CSV & Options | `selectedPlan is not null`¹ |
| 3 | Validate & Preview | `!string.IsNullOrWhiteSpace(csvContent)` |
| 4 | Confirm & Import | `preview is not null && !isPreviewStale` |

¹ Non-null state is set **only** via the `SelectedOptionsChanged` callback in the searchable selector component. No implicit first-option pre-selection occurs on page load or after a list refresh (FR-013, FR-014).

### Selector State Model (FR-013 / FR-014 / FR-015)

Step 1 and Step 2 use `FluentAutocomplete<T>` instead of `FluentSelect`, introducing the following state considerations:

| Field | Type | Initialised to | Set by |
| ----- | ---- | -------------- | ------ |
| `selectedContainer` | `PlannerContainer?` | `null` | `SelectedOptionsChanged` callback on explicit user pick; cleared to `null` on container-list refresh |
| `selectedPlan` | `PlannerPlan?` | `null` | `SelectedOptionsChanged` callback on explicit user pick; cleared to `null` on container change or plan-list refresh |

No new C# fields are added; these are the existing fields already present in `Home.razor`. The change is in **how** they are set: from `@bind-Value` on `FluentSelect` to `SelectedOptionsChanged` on `FluentAutocomplete<T>`. This ensures:

- The fields remain `null` on initial page load (placeholder state visible).
- The fields remain `null` after a list refresh (user must re-select).
- Setting the field to the first item in the list does not happen automatically.

See Research Decision 10 for the component-selection rationale and v4 API validation requirements.

### Step visual state derivation

```text
StepState (enum, internal to Home.razor or inline conditional):
  Locked     — step index > current reachable step
  Active     — step is the currently reachable step with no input yet completed
  Complete   — step's required input is satisfied (e.g. selectedContainer is set)
  Error      — step has validation errors (applies to step index 3 when parseErrors > 0)
```

Visual state is computed inline in the Razor markup using C# conditional expressions against existing state variables. No separate view-model class is introduced (keeping implementation simple per FR spec).

---

## Component Interface Preservation

### HomeExecutionReport

The external `[Parameter]` surface is unchanged:

```csharp
[Parameter]
public ImportExecutionResult? ExecutionResult { get; set; }
```

The internal rendering is refactored to use `FluentTabs`. The three tab categories map to existing properties on `ImportExecutionResult`:

| Tab | Source property | Badge condition |
| --- | --------------- | --------------- |
| Summary | `ExecutionResult.Created` + `ExecutionResult.ReusedOrSkipped` | — |
| Manual Actions | `ExecutionResult.ManualActions` | `ManualActions.Count > 0` |
| Errors | `ExecutionResult.Errors` | `Errors.Count > 0` |

---

## Validation Rules Unchanged

All validation logic remains in the `Application` layer (`ICsvImportParser`, `IImportPlannerOrchestrator`). The UI renders validation results from `ImportValidationError` and `ImportExecutionResult` exactly as today; only the visual presentation and placement change.

---

## No New Persistence or API Contracts

This feature has no storage, API, or Graph contract impact. See `contracts/` — no contracts file is created for this feature because it exposes no external interfaces.
