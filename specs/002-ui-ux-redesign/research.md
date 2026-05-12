# Research: UI/UX Redesign — Stepped Import Workflow (MudBlazor)

**Feature**: 002-ui-ux-redesign  
**Date**: 2026-05-12  
**Status**: Complete — all unknowns resolved

## Summary

All technical unknowns have been resolved through inspection of the existing codebase and
the repository-local MudBlazor skill (`.github/skills/mudblazor/`).

This replanning supersedes the 2026-05-11 research which was Fluent UI-specific. All
component decisions now reference MudBlazor. No attempt has been made to map Fluent UI
components one-for-one; each decision selects the MudBlazor component that is most
appropriate for the UI need.

---

## Decision 1: Vertical step layout strategy

**Decision**: Custom vertical stepper built from MudBlazor layout primitives:
`MudStack` (vertical, outer container), one `MudPaper` per step, with `MudAvatar` for
the step indicator and `MudText` for headings. No wizard widget or accordion component.

**Rationale**: The user requirement is a vertically scrolling "steps down the page"
layout, not a contained wizard widget. `MudPaper` cards give each step a clean, elevated
surface; the visual state (locked / active / complete) is expressed entirely through
MudBlazor properties (`Elevation`, `Color`, `Disabled`) and a single scoped CSS utility
class for the active-step accent border — the only custom CSS in this feature, used as a
last resort per the MudBlazor skill decision order. `MudAvatar` for the step circle is
intent-driven: `Color.Default` (locked), `Color.Primary` (active), `Color.Success`
(complete, with a checkmark icon replacing the number).

**Alternatives considered**:
- `MudExpansionPanels` for collapsible steps: Rejected — the requirement is a static
  vertical sequence, not a collapsed/expanded accordion. Collapsible behaviour is
  explicitly deferred to a follow-up iteration (FR-003).
- A third-party Blazor stepper: Rejected — MudBlazor primitives are sufficient and
  keeping a single UI library is the correct scope boundary.

---

## Decision 2: Step visual state differentiation

**Decision**: Three visual states rendered via MudBlazor component properties:

| State    | `MudPaper` Elevation | `MudAvatar` Color | Avatar content | Content state |
|----------|----------------------|-------------------|----------------|---------------|
| Locked   | 0                    | `Color.Default`   | Step number    | Controls disabled, muted title text |
| Active   | 4                    | `Color.Primary`   | Step number    | Controls enabled, left-accent border via `Home.razor.css` |
| Complete | 1                    | `Color.Success`   | Checkmark icon | Compact summary text, inputs hidden |

The active-step left-accent border is the only custom CSS required, applied via a scoped
`Home.razor.css` `.step-active` class using `var(--mud-palette-primary)` so it follows
the MudBlazor theme automatically.

**Alternatives considered**:
- Using `MudCard` instead of `MudPaper`: `MudCard` imposes its own header/content/action
  slot structure that adds unnecessary nesting for a step card. `MudPaper` with
  `Class="pa-4"` is cleaner.
- Applying `Opacity` style to locked steps: Partial opacity on the whole card obscures
  content too aggressively. Disabling individual controls and using `mud-text-disabled` on
  the heading produces a more readable locked state.

---

## Decision 3: Execution report UI — tabbed categories

**Decision**: `MudTabs` + `MudTabPanel` inside Step 5 (Confirm & Import) to categorise
the execution report. Tab headers include `MudBadge` for non-zero count indicators:

| Tab            | Source property              | Badge condition          |
|----------------|------------------------------|-------------------------|
| Summary        | `Created` + `ReusedOrSkipped`| — (no badge)            |
| Manual Actions | `ManualActions`              | Count when `> 0`        |
| Errors         | `Errors`                     | Count when `> 0`        |

Each tab title is a `RenderFragment` wrapping a `MudBadge` around a `MudText` label —
the standard MudBlazor approach for badged tab headings.

**Alternatives considered**:
- `MudExpansionPanels` for each result section: Rejected — tabs are the correct
  affordance for mutually exclusive result categories. Accordions require active user
  effort to discover errors.
- Flat rendering (current approach): Rejected — produces an undifferentiated long list
  that grows with each category and makes errors easy to miss.

---

## Decision 4: Container and plan selectors (searchable, no preselection)

**Decision**: `MudAutocomplete<T>` for Step 1 (container) and Step 2 (plan).

Configuration:
- `SearchFunc` is `Func<string, CancellationToken, Task<IEnumerable<T>>>` returning
  via `Task.FromResult(...)` for synchronous in-memory filtering.
- `ValueChanged` callback is the sole driver of step progression. `Value` starts as
  `null` and is cleared to `null` on container change or list refresh.
- `ToStringFunc` extracts the display string for rendering and filtering.
- `Variant="Variant.Outlined"` for consistent input styling with the overall form.
- `Placeholder` is set to a descriptive prompt (e.g., "Search or select a container…").

This satisfies all three selector requirements:
- FR-013: No preselection — `Value` is `null` on initialisation.
- FR-014: Explicit selection only — `ValueChanged` is the sole callback; nothing fires on render.
- FR-015: Searchable — `SearchFunc` filters the in-memory list as the user types.

**Alternatives considered**:
- `MudSelect<T>`: Rejected — does not provide search-as-you-type for large lists
  without additional manual filtering wiring. Can preselect the first item when the
  bound collection has a default value.
- Custom text input + dropdown: Rejected — MudBlazor has a first-class component;
  custom solutions violate the skill's decision-order principle.

---

## Decision 5: File upload (CSV)

**Decision**: `MudFileUpload<IBrowserFile>` with `Accept=".csv,text/csv"` and
`MaximumFileCount="1"`. The `OnFilesChanged` callback receives the selected file;
the file name is displayed in a `MudText` element below the upload button.

A `MudButton` inside the `<ButtonTemplate>` acts as the visible upload trigger,
styled with `Variant.Outlined`.

**Alternatives considered**:
- Raw `<InputFile>` element: Rejected — `MudFileUpload<T>` is the MudBlazor-native
  equivalent and integrates with the component decision order.

---

## Decision 6: Inline alerts and messages

**Decision**: `MudAlert` for all inline contextual messages.

Severity mapping:
- `Severity.Info` — instructional messages, "no containers found" prompt.
- `Severity.Warning` — no plans for container; stale-preview warning.
- `Severity.Error` — validation errors present.
- `Severity.Success` — execution complete with link to plan in Planner.

`MudAlert` renders inline inside the relevant step card, keeping contextual messaging
co-located with the step it relates to.

**Alternatives considered**:
- `ISnackbar` for persistent state messages (stale preview, no plans found): Rejected —
  snackbars are ephemeral and unsuitable for conditions that must remain visible until
  resolved. `ISnackbar` is reserved for transient operation outcomes.

---

## Decision 7: Preview and report tables

**Decision**: Use the lightest MudBlazor table component appropriate to each use:

| Location              | Component       | Rationale |
|-----------------------|-----------------|-----------|
| Bucket actions (preview) | `MudSimpleTable` | Small static list (1–10 rows), read-only, no interaction needed |
| Task actions (preview)   | `MudDataGrid<T>` with `Dense="true"` | Potentially large list; benefits from density and column width |
| Validation errors (Step 4) | `MudDataGrid<T>` with `Dense="true"` | Potentially many rows; consistent with task actions presentation |
| Summary tab (execution report) | `MudSimpleTable` | Short read-only created/reused counts |
| Manual Actions tab    | `MudDataGrid<T>` | Multiple columns; benefits from column alignment |
| Errors tab            | `MudDataGrid<T>` | Consistent with manual actions rendering |

**Alternatives considered**:
- `MudDataGrid<T>` for everything: Over-engineered for 1–10 row static lists.
- `MudTable<T>` throughout: Heavier than `MudSimpleTable` for fully static read-only tables.

---

## Decision 8: MainLayout shell

**Decision**: Replace the custom `app-shell` div + `app-header` div layout with the
standard MudBlazor shell:

- `MudLayout` — outer shell
- `MudAppBar Elevation="1"` — top bar with app title, user identity, sign-in/sign-out
- `MudMainContent Class="pa-4"` — page content area

Providers in `MainLayout.razor`:
```razor
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

The `app.css` global stylesheet retains only Blazor reconnect/error overlay styles.
All shell layout CSS in `MainLayout.razor.css` and `.import-grid` utility classes in
`app.css` are removed — MudBlazor utility classes and `MudStack` replace them.

**Alternatives considered**:
- Keep the existing custom header div: Rejected — it requires maintaining custom CSS
  that MudBlazor handles natively via `MudLayout`/`MudAppBar`/`MudMainContent`.

---

## Decision 9: Busy state / loading indicator

**Decision**: A `MudProgressLinear Indeterminate="true"` bar positioned immediately below
the `MudAppBar` (or at the top of the main content area) is shown when `isBusy` is true.
Individual action buttons remain disabled during busy state (existing behaviour preserved).

**Rationale**: A top-of-page linear progress bar follows the Material Design convention
for page-level async operations and gives clear global feedback without obscuring step
content. Existing button-disable logic provides secondary confirmation.

---

## Decision 10: Package changes required

**Decision**: The following changes are required:

**`Directory.Packages.props`**:
- Remove `Microsoft.FluentUI.AspNetCore.Components` 4.14.1
- Remove `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.1
- Add `MudBlazor` (latest stable compatible with .NET 10)

**`ImportToPlanner.Web.csproj`**:
- Remove `<PackageReference Include="Microsoft.FluentUI.AspNetCore.Components" />`
- Remove `<PackageReference Include="Microsoft.FluentUI.AspNetCore.Components.Icons" />`
- Add `<PackageReference Include="MudBlazor" />`

**`_Imports.razor`**:
- Replace `@using Microsoft.FluentUI.AspNetCore.Components` with `@using MudBlazor`

**`Program.cs`**:
- Remove `using Microsoft.FluentUI.AspNetCore.Components;`
- Replace `builder.Services.AddFluentUIComponents();` with `builder.Services.AddMudServices();`

**`App.razor`**:
- Add `<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />` to `<head>`
  (MudBlazor requires the CSS link to be explicitly present in the host page).

---

## Decision 11: HomeExecutionReport refactoring scope

**Decision**: `HomeExecutionReport.razor` is refactored in-place to use `MudTabs` as
described in Decision 3. The external `[Parameter]` surface
(`ImportExecutionResult? ExecutionResult`) remains unchanged, preserving compatibility
with `Home.razor` and all existing tests.

---

## Decision 12: Step collapse/expand (deferred)

**Decision**: Completed steps show a compact single-line summary (e.g., "Container: My
Group (Group)") rendered as `MudText Typo="Typo.body2"` inside the step `MudPaper`, but
do not implement interactive expand/collapse. FR-003 defers this explicitly. No state
complexity or additional components are required for the first iteration.
