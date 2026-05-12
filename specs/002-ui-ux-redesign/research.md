# Research: UI/UX Redesign — Stepped Import Workflow

**Feature**: 002-ui-ux-redesign  
**Date**: 2026-05-11  
**Status**: Complete — all unknowns resolved

## Summary

All technical unknowns have been resolved through inspection of the existing codebase and the official Fluent UI Blazor documentation at https://www.fluentui-blazor.net/.

Additional validation for implementation readiness was completed against the Fluent UI Blazor v5 MCP documentation (`/Mcp/GetStarted`) and AI skills documentation (`/Mcp/AISkills`).

---

## Decision 1: Vertical step layout strategy

**Decision**: Custom vertical stepper built from Fluent UI primitives (`FluentCard`, `FluentBadge`, `FluentIcon`, `FluentStack`), **not** `FluentWizard`.

**Rationale**: `FluentWizard` is designed as a contained widget with a fixed `Height` (default 400 px). Its content area scrolls internally, showing only the active step's content at once. The user requirement is explicitly "down the page" — meaning all steps are visible on the page with different visual states, and the user scrolls through the full vertical flow. The custom approach is the only one that satisfies this requirement while staying within Fluent UI primitives.

**Alternatives considered**:
- `FluentWizard` with `StepperPosition.Top` and `Height="auto"`: Rejected because the component is not designed for open-ended vertical stacking; height auto behaviour is not documented and layout would be unpredictable.
- `FluentWizard` with `StepperPosition.Left`: Rejected for same reasons — places step navigation in a sidebar column, not a top-to-bottom step flow.
- Third-party stepper library: Rejected by NFR-001 (Fluent UI must be the sole UI library).

---

## Decision 2: Visual state differentiation for steps

**Decision**: Three visual states for each step card:

| State | Visual treatment |
| ----- | ---------------- |
| **Locked** | `FluentCard` with `MinimalStyle="true"`, muted header text, `FluentIcon` showing a lock or circle-outline, step number in neutral `FluentBadge`, controls disabled. |
| **Active** | `FluentCard` with default styling, accent-coloured left border via scoped CSS, `FluentIcon` showing a play/chevron-right, step number in `FluentBadge` with `Appearance.Accent`. |
| **Complete** | `FluentCard` with `MinimalStyle="true"`, muted text, `FluentIcon` showing a checkmark-circle (`CheckmarkCircle20Filled`), step number in `FluentBadge` with `Appearance.Neutral`. Optional compact summary of user's selection shown inline. |

**Rationale**: Using `FluentCard` + `FluentBadge` + `FluentIcon` keeps all visual differentiation within the Fluent Design System. The `MinimalStyle` property on `FluentCard` reduces the CSS footprint for the many repeated locked/complete cards.

**Alternatives considered**: Using Fluent accent border tokens directly on `<div>` elements — rejected because it bypasses the Fluent card elevation/shadow semantics that the user wants for "modern looking".

---

## Decision 3: Execution report UI — tabs for result categories

**Decision**: Use `FluentTabs` + `FluentTab` within Step 5 (Confirm & Results) to categorise the execution report into:

| Tab | Content | Badge |
| --- | ------- | ----- |
| Summary | Created items grid + Reused/Skipped items grid | — |
| Manual Actions | `FluentDataGrid` of `ManualAction` items | Count badge if > 0 |
| Errors | `FluentDataGrid` of error strings | Count badge if > 0 |

**Rationale**: The user explicitly suggested tabs for manual actions. `FluentTabs` with custom headers (`Header` parameter accepting `RenderFragment`) supports `FluentBadge` in the tab title, making it easy to draw attention to non-zero counts. This is a significant UX improvement over the current flat list.

**Alternatives considered**: Accordion (FluentAccordion) for each section — rejected because tabs are a better fit for mutually exclusive sections of a completed operation. Flat rendering (current approach) — rejected as it produces a long, undifferentiated list.

---

## Decision 4: Step connector line

**Decision**: A vertical connector line between step cards is implemented using a single scoped CSS pseudo-element on a wrapper `<div>` with class `step-connector`. It uses `--neutral-stroke-rest` Fluent design token for the line colour to ensure automatic light/dark mode adaptation.

**Rationale**: CSS-only approach keeps the Razor markup clean. Using the Fluent design token ensures the line colour follows the theme without hard-coding hex values.

**Alternatives considered**: `FluentDivider` with vertical orientation — rejected because it produces a full-width horizontal rule, not a narrow left-aligned vertical connector. SVG lines — rejected as overly complex for a static layout element.

---

## Decision 5: No new packages required

**Decision**: All required components (`FluentWizard` was considered but rejected; `FluentTabs`, `FluentCard`, `FluentBadge`, `FluentIcon` are all used) are available in the currently installed `Microsoft.FluentUI.AspNetCore.Components` version **4.14.1**. No package updates or additions are required.

**Rationale**: Version 4.14.1 includes all components used in the design. The `FluentIcons` package (`Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.1) is already referenced and provides the icon variants needed (`CheckmarkCircle20Filled`, `LockClosed20Regular`, `ChevronRight20Regular`).

---

## Decision 6: HomeExecutionReport.razor refactoring scope

**Decision**: `HomeExecutionReport.razor` will be refactored in-place to use `FluentTabs` for the three result categories. The component `[Parameter]` interface (`ExecutionResult`) remains unchanged, preserving compatibility with the parent `Home.razor`.

**Rationale**: The component's external API does not need to change; only its internal rendering logic changes. Keeping it as a separate component preserves testability.

---

## Decision 7: Step collapse/expand (completed steps)

**Decision**: Completed steps will show a compact single-line summary (e.g. "Container: My Group (Group)") but will **not** implement an interactive expand/collapse toggle in this iteration.

**Rationale**: Interactive collapsing would require additional state tracking and event handling. The spec marks this as "optionally collapse" (FR-003). For a first iteration, a fixed compact summary is sufficient and keeps complexity low. A follow-up feature can add expand/collapse.

---

## Decision 8: MCP-first implementation guardrail

**Decision**: Configure and use the Fluent UI Blazor MCP server (`fluentui-mcp`) before UI implementation/refinement work.

**Rationale**: Recent implementation attempts encountered FluentSelect dropdown visibility issues that triggered repeated CSS trial-and-error. MCP tools provide component-aware guidance and reduce reliance on speculative CSS changes.

**Alternatives considered**:
- Continue with manual trial-and-error in CSS: Rejected due to repeated regressions and time loss.
- Ignore MCP until after implementation: Rejected because it does not mitigate the known planning risk.

---

## Decision 9: Replace v4-focused skill usage with v5 AI skill pack for guidance

**Decision**: Add and prefer a repository-local `fluentui-blazor-usage` skill pack (`SKILL.md`, `references/SETUP.md`, `references/DATAGRID.md`, `references/THEMING.md`) sourced from the official v5 AI Skills guidance page.

**Rationale**: The v5 skill pack explicitly calls out common v4/v5 API confusion (`FluentNavMenu` vs `FluentNav`, `SelectedOptions` vs `SelectedItems`, `FluentDesignTheme` removal). These are high-value guardrails even while this feature remains implemented against current project constraints.

**Alternatives considered**:
- Keep only the existing v4-focused `fluentui-blazor` skill: Rejected because it does not capture the new MCP-oriented guidance model.
- No project-local skill updates: Rejected because agent behaviour then depends on stale/default assumptions.

---

## Decision 10: Searchable selector component strategy (FR-013 / FR-014 / FR-015)

**Decision**: Replace `FluentSelect` in Step 1 and Step 2 with `FluentAutocomplete<T>`. The component is configured to satisfy all three selector requirements:

- `SelectedOptions` is initialised empty — enforces FR-013 (no first-option auto-selection).
- `SelectedOptionsChanged` is the sole callback driving step progression — enforces FR-014 (explicit selection only).
- Built-in filter-as-you-type behaviour over the bound options collection — satisfies FR-015 (searchable filtering for large datasets).

**Rationale**: `FluentAutocomplete<T>` is the only Fluent UI Blazor v4 component that combines all three properties:
1. An explicit change callback pattern (`SelectedOptionsChanged`) that does not fire on render.
2. Built-in filtered search input over a bound `IQueryable<T>` or `IEnumerable<T>`.
3. No implicit preselection when `SelectedOptions` is initialised as an empty collection.

A plain `FluentSelect` cannot satisfy FR-013/FR-014 without brittle workarounds because it can auto-select the first rendered option when no selected value matches — this is the known bug that drove those requirements.

**Implementation constraints**:
- `FluentAutocomplete<TOption>` parameterised with `PlannerContainer` (Step 1) and `PlannerPlan` (Step 2).
- `NameSelector` extracts the display name for filtering.
- `MaximumSelectedOptions="1"` enforces single selection semantics.
- `SelectedOptions` initial value is `[]` (no pre-selection on load or list refresh).
- `Placeholder` is set to a descriptive prompt (e.g. "Select or search for a container…").
- `SelectedOptionsChanged` callback sets `selectedContainer`/`selectedPlan` and calls `StateHasChanged`.
- `selectedContainer` and `selectedPlan` are cleared to `null` on container-change or list-refresh to ensure FR-014 consistency.

**Validation required at T006**: The C# Expert agent must confirm the exact v4 API signature (`SelectedOptions`, `SelectedOptionsChanged`, `NameSelector`, `MaximumSelectedOptions`) during the foundation phase (T006/T028–T029). Consult the Fluent UI MCP server for v4-accurate guidance.

**Alternatives considered**:
- `FluentCombobox`: Rejected — in v4 it lacks the `SelectedOptionsChanged` pattern that unambiguously separates explicit selection from binding initialisation, and it does not expose a clean single-selection change event.
- `FluentSelect` with null-sentinel option: Rejected — requires adding a dummy null option to the bound collection and brittle `@bind-Value` initialisation; violates the Fluent-first principle and adds maintenance debt.
- `FluentSelect` with explicit `@onchange` handler: Rejected — the underlying Fluent select element may still fire a change event on first render when the initial value is unset, perpetuating the FR-013 bug.

---

## Codebase Facts Confirmed

| Fact | Value |
| ---- | ----- |
| FluentUI version | 4.14.1 |
| Icons package version | 4.14.1 |
| App is single-page | Yes — `Home.razor` at `/` is the only user-facing page |
| Existing Home.razor lines | ~465 |
| Existing CSS scoped files in Pages | None (Home.razor has no `.razor.css` today) |
| No external API contract | Confirmed — all changes are front-end only |
| Both runtime modes (Graph + InMemory) | UI is mode-agnostic; mode-specific logic in `OnInitializedAsync` is unchanged |
| Test projects | `ImportToPlanner.Web.Tests` uses bUnit for component tests |
