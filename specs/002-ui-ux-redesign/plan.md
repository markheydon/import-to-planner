# Implementation Plan: UI/UX Redesign — Stepped Import Workflow

**Branch**: `002-ui-ux-redesign` | **Date**: 2026-05-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-ui-ux-redesign/spec.md`

## Summary

Redesign the single `Home.razor` import page to present the CSV-to-Planner import workflow as a five-step vertical wizard-like layout using Fluent UI Blazor components. Each step card is always visible on the page but becomes interactive only when its prerequisites are met. The execution results step uses `FluentTabs` to categorise summary, manual actions, and errors. No back-end, domain, or application-layer changes are required; all changes are confined to the Blazor web front-end project.

Implementation preparation for this feature includes Fluent UI Blazor MCP server enablement and v5 AI skill adoption so coding guidance comes from official component patterns instead of ad-hoc CSS experimentation. Container and plan selector controls are replaced with searchable components (`FluentAutocomplete<T>`) to satisfy FR-013 (unselected placeholder), FR-014 (explicit-selection-only progression), and FR-015 (searchable filtering for large datasets).

Implementation scope remains on the currently installed Fluent UI package line (v4.14.1) for this feature branch. v5 guidance assets are used for risk reduction and migration readiness, not as an in-scope package upgrade.

See [research.md](research.md) for all technical decisions and rationale.

## Technical Context

**Language/Version**: C# 13 / .NET 9 (see `global.json`)  
**Primary Dependencies**: `Microsoft.FluentUI.AspNetCore.Components` 4.14.1, `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.1 (both already installed; no new packages required)  
**AI Tooling Dependencies**: `Microsoft.FluentUI.AspNetCore.McpServer` (global .NET tool, `5.0.0-rc.2-26098.1`), repository skill `fluentui-blazor-usage` in `.github/skills/fluentui-blazor-usage/`  
**Storage**: N/A — pure UI change  
**Testing**: xUnit + bUnit (existing test infrastructure in `tests/ImportToPlanner.Web.Tests/`)  
**Target Platform**: Blazor Server, desktop browser at ≥ 1024 px  
**Project Type**: Web application (Blazor Server)  
**Performance Goals**: No perceptible additional render delay vs. current flat layout  
**Constraints**: Fluent UI Blazor must remain the sole UI library; implementation must compile against current installed Fluent UI version (v4.14.1) unless package upgrade scope is approved; custom CSS must be minimal and token-driven; raw HTML/CSS workarounds are last resort and require documented rationale based on MCP/doc guidance  
**Scale/Scope**: Single import page, single user at a time, desktop primary

### Versioning Position

- Current implementation target: Fluent UI Blazor v4.14.1 (installed and production-aligned in this repository).
- v5 status: prerelease track; available for guidance and migration planning via MCP and AI skills.
- Decision for this feature: do not introduce prerelease package migration scope while completing the UI redesign acceptance criteria.

## Constitution Check

*Gate status checked pre-design (Phase 0) and re-checked post-design (Phase 1).*

- **Code Quality Gate**: ✅ All changes confined to `ImportToPlanner.Web` project. No cross-layer coupling introduced. Existing application/domain/infrastructure boundaries are untouched. Home.razor component logic remains in the Razor file; no new services introduced.
- **Testing Gate**: ✅ Existing `HomePageWorkflowTests.cs` and `HomePageSmokeTests.cs` (bUnit) must pass without modification to test logic. New tests required for: step activation sequence, stale-preview warning placement, FluentTabs content in execution report, selector placeholder/initial state (FR-013), explicit first-option progression regression (FR-014), and searchable filtering behaviour (FR-015). Test coverage for step-state transitions uses in-memory gateway mode.
- **UX Consistency Gate**: ✅ All existing user-facing flows are preserved. UK English copy retained. Authentication redirect logic unchanged. Accessibility: Fluent UI components carry built-in ARIA semantics; custom step containers add `role="region"` and `aria-label` attributes.
- **Performance Gate**: ✅ No additional async operations or service calls introduced. Step visibility is computed from existing state variables (no new O(n) or remote-call patterns). FluentCard `MinimalStyle="true"` reduces CSS footprint for repeated step containers.
- **Operations Gate**: ✅ Existing logging and error handling in Home.razor is unchanged. Status messages continue to be surfaced in-step. Dry-run safety behaviour is preserved.
- **Runtime Mode Compatibility Gate**: ✅ UI is fully mode-agnostic. Step activation logic derives from existing state variables that behave identically in both in-memory and Graph modes. Authentication redirect in `OnInitializedAsync` is unchanged.
- **Graph Contract Volatility Gate**: ✅ No Graph-facing changes in this feature.
- **Scope Boundary Gate**: ✅ Single-tenant scope unchanged. No multi-tenant logic introduced.
- **CI and AppHost Gate**: ✅ No project structure, build target, or AppHost changes. Solution-level build is unaffected.
- **Agent Delegation Gate**: ✅ All Blazor component coding, CSS, and test work MUST be delegated to the **C# Expert** agent (`AGENTS.md`).
- **CSS Discipline Gate**: ✅ Known FluentSelect first-option auto-selection bug is resolved by replacing FluentSelect with `FluentAutocomplete<T>` (a Fluent-first solution, not a CSS workaround). Any remaining CSS-fallback requirements are handled by a guidance-first workflow (MCP tools and `fluentui-blazor-usage` skill) before any CSS fallback is accepted.
- **Fluent-First Gate**: ✅ Prefer Fluent component capabilities, parameters, and composition patterns over hand-authored HTML/CSS; fallback requires explicit known-issue rationale per constitution §VI.
- **MCP/Skill Enablement Gate**: ✅ Workspace MCP config (`.vscode/mcp.json`) includes `fluent-ui-blazor` server (`fluentui-mcp`) and repository contains `fluentui-blazor-usage` skill assets to reduce v4/v5 pattern drift during implementation.

## Project Structure

### Documentation (this feature)

```text
specs/002-ui-ux-redesign/
├── plan.md              ← This file
├── research.md          ← Phase 0 decisions
├── data-model.md        ← Phase 1 UI state model
├── quickstart.md        ← Phase 1 verification guide
└── tasks.md             ← Phase 2 output (not yet created)
```

No `contracts/` directory is created — this feature has no external API or interface contracts.

### Source Code Changes

```text
src/ImportToPlanner.Web/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor                    ← MODIFY: full markup redesign (vertical stepper)
│   │   ├── Home.razor.css                ← CREATE: scoped CSS for step connector + card variants
│   │   └── HomeExecutionReport.razor     ← MODIFY: refactor to use FluentTabs
│   └── Layout/
│       ├── MainLayout.razor              ← No changes required
│       └── MainLayout.razor.css          ← No changes required

tests/ImportToPlanner.Web.Tests/
├── HomePageSmokeTests.cs                 ← MODIFY: update step-aware smoke assertions
└── HomePageWorkflowTests.cs              ← MODIFY: add step activation + stale-preview tests
```

## Design

### Component Layout

Each step is rendered as a `FluentCard` with a consistent two-zone layout:

```
┌─────────────────────────────────────────────────────────┐
│  [Badge: step number]  Step Title          [Status icon] │  ← Step header row
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Step content (selects, file upload, grids, etc.)        │  ← Content area
│                                                          │
└─────────────────────────────────────────────────────────┘
          │
          │  connector line (CSS ::after pseudo-element)
          │
┌─────────────────────────────────────────────────────────┐
│  [Badge: step number]  Next Step Title     [Status icon] │
│  ...                                                     │
```

**Step visual states** (applied via CSS classes `step--active`, `step--complete`, `step--locked`):

| State | Card | Badge | Status icon | Controls |
| ----- | ---- | ----- | ----------- | -------- |
| Active | Default `FluentCard` + accent left border | `Appearance.Accent` | `ChevronRight20Filled` | Enabled |
| Complete | `FluentCard MinimalStyle="true"` | `Appearance.Neutral` | `CheckmarkCircle20Filled` (accent tint) | Compact summary only |
| Locked | `FluentCard MinimalStyle="true"` | `Appearance.Neutral` (muted) | `LockClosed20Regular` | Disabled |

**Five steps:**

| # | Title | Unlocks when |
| - | ----- | ------------ |
| 1 | Select Container | Always active |
| 2 | Select Plan | `selectedContainer != null`¹ |
| 3 | Upload CSV | `selectedPlan != null`¹ |
| 4 | Validate & Preview | `csvContent` not empty |
| 5 | Confirm & Import | `preview != null && !isPreviewStale` |

¹ Non-null state is set **only** via an explicit user selection event in the searchable selector (`SelectedOptionsChanged` callback). Components must not auto-select or pre-populate these values on load or list refresh. See FR-013, FR-014, and Research Decision 10.

### Selector Strategy (FR-013 / FR-014 / FR-015)

Container (Step 1) and plan (Step 2) selectors are replaced with `FluentAutocomplete<T>` to satisfy all three selector requirements:

| Requirement | Implementation |
| ----------- | -------------- |
| FR-013 — Unselected placeholder | `SelectedOptions` initialised empty; `Placeholder` text set. Component shows placeholder until user explicitly picks an item. |
| FR-014 — Explicit selection only | Progression driven by `SelectedOptionsChanged` callback only; no `@bind-Value` that could trigger on render. |
| FR-015 — Searchable filtering | `FluentAutocomplete` built-in filter-as-you-type over the bound options collection. |

Selector state in `Home.razor @code { }`:
- `selectedContainer` (`PlannerContainer?`) — set in `SelectedOptionsChanged` callback; cleared on container list refresh.
- `selectedPlan` (`PlannerPlan?`) — set in `SelectedOptionsChanged` callback; cleared on container change or plan list refresh.

See Research Decision 10 for component-choice rationale, v4 API constraints, and alternatives rejected.

### Step 5 — Execution Report with FluentTabs

`HomeExecutionReport.razor` is refactored to use `FluentTabs`:

```razor
<FluentTabs>
    <FluentTab Label="Summary">
        <!-- Created grid + Reused/Skipped grid -->
    </FluentTab>
    <FluentTab>
        <Header>
            Manual Actions
            @if (ExecutionResult.ManualActions.Count > 0)
            {
                <FluentBadge Appearance="Appearance.Accent" Circular="true">
                    @ExecutionResult.ManualActions.Count
                </FluentBadge>
            }
        </Header>
        <!-- ManualAction data grid -->
    </FluentTab>
    <FluentTab>
        <Header>
            Errors
            @if (ExecutionResult.Errors.Count > 0)
            {
                <FluentBadge Appearance="Appearance.Accent" BackgroundColor="var(--error)" Circular="true">
                    @ExecutionResult.Errors.Count
                </FluentBadge>
            }
        </Header>
        <!-- Errors data grid or success message -->
    </FluentTab>
</FluentTabs>
```

### Scoped CSS (Home.razor.css)

Minimal additions:

```css
/* Vertical connector between step cards */
.step-list {
    position: relative;
}
.step-connector {
    position: relative;
    padding-left: 0;
}
.step-connector + .step-connector::before {
    content: '';
    display: block;
    width: 2px;
    height: 1.5rem;
    background-color: var(--neutral-stroke-rest);
    margin: 0 auto 0 1.75rem; /* aligns with badge centre */
}

/* Active step accent border */
.step--active fluent-card::part(control) {
    border-left: 3px solid var(--accent-fill-rest);
}

/* Locked step muted opacity */
.step--locked {
    opacity: 0.6;
}
```

> All colour values use Fluent design tokens for automatic light/dark mode support.

## Complexity Tracking

No constitution violations. No unjustified complexity.

## Implementation Notes for the C# Expert Agent

- All Razor and CSS work MUST be delegated to the C# Expert agent.
- Existing `@code { }` block logic in `Home.razor` is preserved for all import orchestration, validation, preview, and execution state management. Targeted additions to the `@code { }` block **are permitted** for selector state management required by FR-013/FR-014/FR-015: specifically, replacing FluentSelect event binding with `FluentAutocomplete<T>` `SelectedOptionsChanged` callbacks and ensuring `selectedContainer`/`selectedPlan` are only set via explicit user selection events. All other redesign work touches **markup only** plus scoped CSS.
- The `canValidate`, `canExecute`, and `IsCurrentSelectionInSyncWithRequest()` members are the source of truth for step enablement and must not be duplicated.
- `HomeExecutionReport.razor` keeps the same `[Parameter]` interface.
- Test changes: use bUnit's `IRenderedComponent<T>` to assert CSS classes on step containers and tab rendering in the execution report.
- Use `FluentIcon` with the `Value` parameter for named icons from the Icons package (e.g. `new Icons.Filled.Size20.CheckmarkCircle()`).
- Prefer `@if` conditional class binding over JavaScript toggle for step state classes.

