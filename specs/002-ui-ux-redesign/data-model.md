# Data Model: UI/UX Redesign — Stepped Import Workflow (MudBlazor)

**Feature**: 002-ui-ux-redesign  
**Date**: 2026-05-12

## Overview

This is a pure front-end UI feature. No new domain or application-layer entities are
introduced. All existing domain models (`PlannerContainer`, `PlannerPlan`,
`ImportPlanPreview`, `ImportExecutionResult`, `ImportTaskPlanItem`,
`ImportValidationError`, `ManualAction`) remain unchanged.

The data model for this feature is limited to **UI view-state** within the `Home.razor`
component. No new shared models, DTOs, or services are required.

---

## UI Step State Model

The stepped layout requires the component to track derived state for each step.
This is expressed as computed C# properties derived from existing state fields.

### Step definitions

| Step index | Name              | Active when (derived condition)                        |
|------------|-------------------|--------------------------------------------------------|
| 0          | Select Container  | Always active on page load                             |
| 1          | Select Plan       | `selectedContainer is not null`¹                       |
| 2          | Upload CSV & Options | `selectedPlan is not null`¹                         |
| 3          | Validate & Preview | `!string.IsNullOrWhiteSpace(csvContent)`              |
| 4          | Confirm & Import  | `preview is not null && !isPreviewStale`               |

¹ Non-null state is set **only** via the `ValueChanged` callback on `MudAutocomplete<T>`.
No implicit preselection occurs on page load or after a list refresh (FR-013, FR-014).

### Selector State Model (FR-013 / FR-014 / FR-015)

Step 1 and Step 2 use `MudAutocomplete<T>` which introduces the following state
considerations:

| Field              | Type                 | Initialised to | Set by                                                      |
|--------------------|----------------------|----------------|-------------------------------------------------------------|
| `selectedContainer`| `PlannerContainer?`  | `null`         | `ValueChanged` callback on explicit user pick; cleared to `null` on list refresh |
| `selectedPlan`     | `PlannerPlan?`       | `null`         | `ValueChanged` callback on explicit user pick; cleared to `null` on container change or list refresh |

No new C# fields are added beyond what already exists in `Home.razor`. The change is in
**how** they are set: from Fluent UI binding to `MudAutocomplete.ValueChanged`. This
ensures:

- Fields remain `null` on initial page load (placeholder state visible in selector).
- Fields remain `null` after a list refresh (user must re-select explicitly).
- The first item in the list is never silently selected.

### Step visual state derivation

```text
StepState (derived inline from existing fields in Home.razor):
  Locked   — step index is above the currently reachable step
  Active   — step is the currently reachable step (prerequisite satisfied, current step's
              own required input is not yet satisfied)
  Complete — step's required input is satisfied
  Error    — Step 3 (Validate & Preview) when parseErrors.Count > 0
```

Visual state is computed inline in Razor markup via C# conditional expressions against
existing state variables. No separate view-model class is introduced.

---

## Component Interface Preservation

### HomeExecutionReport

The external `[Parameter]` surface is unchanged:

```csharp
[Parameter]
public ImportExecutionResult? ExecutionResult { get; set; }
```

Internal rendering is refactored to use `MudTabs`. The three tab categories map to
existing properties on `ImportExecutionResult`:

| Tab            | Source property                               | Badge condition            |
|----------------|-----------------------------------------------|---------------------------|
| Summary        | `ExecutionResult.Created` + `ReusedOrSkipped` | —                         |
| Manual Actions | `ExecutionResult.ManualActions`               | `ManualActions.Count > 0` |
| Errors         | `ExecutionResult.Errors`                      | `Errors.Count > 0`        |

---

## Files Changed (front-end scope only)

| File | Change type |
|------|-------------|
| `Directory.Packages.props` | Remove Fluent UI packages, add `MudBlazor` |
| `ImportToPlanner.Web.csproj` | Remove Fluent UI refs, add `MudBlazor` ref |
| `Program.cs` | Replace `AddFluentUIComponents()` with `AddMudServices()` |
| `Components/App.razor` | Add MudBlazor CSS link to `<head>` |
| `Components/_Imports.razor` | Replace Fluent UI `@using` with `@using MudBlazor` |
| `Components/Layout/MainLayout.razor` | Full rewrite: MudLayout/MudAppBar shell, replace all Fluent providers |
| `Components/Layout/MainLayout.razor.css` | Remove shell CSS (replaced by MudBlazor layout primitives) |
| `Components/Pages/Home.razor` | Full rewrite: five `MudPaper` step cards, `MudAutocomplete<T>` selectors, `MudFileUpload<IBrowserFile>`, `MudAlert` messages, `MudDataGrid`/`MudSimpleTable` grids |
| `Components/Pages/HomeExecutionReport.razor` | Refactor internals: `MudTabs` with badged tab headers, `MudSimpleTable`/`MudDataGrid<T>` |
| `wwwroot/app.css` | Remove shell + import-grid CSS; retain Blazor error overlay styles only |

---

## Validation Rules Unchanged

All validation logic remains in the `Application` layer (`ICsvImportParser`,
`IImportPlannerOrchestrator`). The UI renders validation results from
`ImportValidationError` and `ImportExecutionResult` exactly as today; only the visual
presentation and placement within the step cards changes.

---

## No New Persistence or API Contracts

This feature has no storage, API, or Graph contract impact. No contracts file is created
for this feature as it exposes no external interfaces.
