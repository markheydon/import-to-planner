# Implementation Plan: Import Wizard Stepper Layout

**Branch**: `010-wizard-stepper-layout` | **Date**: 2026-09-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-wizard-stepper-layout/spec.md`, plus GitHub issue [#117](https://github.com/markheydon/import-to-planner/issues/117) for layout inventory, commercial chrome, and phased delivery.

**Note**: Coding, architecture, and tests at implement time are delegated to the C# Expert agent (`AGENTS.md`) using `csharp-async`, `csharp-docs`, `csharp-xunit`, `dotnet-best-practices-repo`, and the `mudblazor` skill for all component and layout work. Public `docs/` screenshot updates are follow-on unless Home copy itself changes.

## Summary

Replace the Home page’s five always-expanded step cards with a persistent header plus a MudBlazor stepper layout: vertical `MudStepper` as step navigation, a single active working pane, collapsible setup panels for steps 1–3, and a sticky import-context rail on wide viewports. Preserve existing `canValidate` / `canExecute`, preview staleness, identity chrome, and commercial gates. Do not change `ImportWorkflowCoordinator`, commercial account use cases, or `/profile` markup. Deliver in slices matching spec stories P1–P4.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK from `global.json`)

**Primary Dependencies**: Blazor Interactive Server, MudBlazor 9.9.0 (`MudStepper`, `MudExpansionPanels`, `MudGrid`, `MudHidden` / `MudDrawer` for optional Phase 4), existing Home presenters and `WorkflowCoordinationState`, xUnit v3, NSubstitute, bUnit

**Storage**: N/A — no new persistence. Existing `WorkflowCoordinationState` remains the source of import selections and results.

**Testing**: bUnit in `ImportToPlanner.Web.Tests` (workflow, smoke, identity, commercial access/retention). Architecture scans unchanged unless Web gains forbidden inner-layer types (it must not). No AppHost tests. No Playwright suite unless a later explicit journey is requested (not required here). `ProfilePageTests` expected unchanged.

**Target Platform**: ASP.NET Core Blazor on Linux-hosted Aspire / ACA; desktop browsers primary; narrow viewports in Phase 4 (optional)

**Project Type**: Layered Blazor web app; this feature is Web presentation only

**Performance Goals**: No extra Graph, CSV, or commercial use-case calls. Layout change must not add a perceptible extra round-trip. Sticky rail and stepper re-renders stay client-side from existing state.

**Constraints**: Inward dependencies only; no MudBlazor or UI types in Application/Domain; UK English; MudBlazor decision order (component → layout primitives → utilities → theme → isolated CSS last); no Tailwind; hide wizard on commercial login and deleted-account gates; verify commercial mode on and off; `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes`

**Scale/Scope**: One page (`Home.razor` + partials + `Home.razor.css` + optional `HomeExecutionReport.razor`); test helper updates; no new projects or routes

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below — still passes.*

- **I. Dependency Direction**: Changes stay in `ImportToPlanner.Web` and `ImportToPlanner.Web.Tests`. No Application or Domain edits.
- **II. Technology-neutral core**: Step titles, gating, and import meaning stay as today. MudBlazor is an outer presentation choice.
- **III. Explicit boundaries**: `ImportWorkflowCoordinator` and presenters unchanged. New viewed-step / panel-expanded flags are Home-owned UI state derived from existing coordination state.
- **IV. Replaceable frameworks**: Spec and contracts describe layout regions (header, step list, working pane, summary). MudBlazor is the current adapter; a later library swap would re-implement the same regions.
- **V. Traceability**: Work maps to spec 010 FRs / stories and issue #117. No opportunistic `/profile` restyle or coordinator rewrite.
- **VI. Testable behaviour**: bUnit must fail if the wizard renders on commercial gates, if Profile appears in self-host, or if preview/execute gating regresses. Inner-layer tests are unaffected.
- **VII. Explicit failures**: Existing alerts (stale preview, tenant mismatch, admin consent, unsupported account, restore failures) stay human-friendly; no raw exception dumps.
- **VIII. Security**: No new trust boundary. Profile remains commercial-only. Sign in/out unchanged. Secrets still out of UI.
- **IX. Quality evidence**: Updated bUnit selectors and new layout assertions; format gate at implement; architecture tests remain green without new inner-layer UI types.
- **X. Self-hosted viability**: Layout must work with commercial mode off (no Profile, no commercial gates, wizard after normal sign-in).
- **Policy alignment (non-constitutional)**: MudBlazor-first UI; xUnit v3 / NSubstitute / Assert; UK English; a11y of stepper and header; no AppHost tests.

No constitution violations requiring Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/010-wizard-stepper-layout/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── wizard-layout-ui-contract.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/ImportToPlanner.Web/Features/Import/Pages/Home/
├── Home.razor                          # header + gates + stepper grid; no five-card stack
├── Home.razor.css                      # drop step-card chrome; sticky rail only if utilities cannot
├── Home.StepPresentation.razor.cs      # viewed-step index; stepper Completed/Disabled mapping
├── Home.ThemeHeader.razor.cs           # unchanged unless header markup moves
├── Home.CommercialAccess.razor.cs      # touch only if gate render moves; access rules unchanged
├── HomeExecutionReport.razor           # Phase 3: summary counts before tables
└── HomeWorkflowStepPresentation.cs     # reuse titles/summaries for stepper SecondaryText

tests/ImportToPlanner.Web.Tests/
├── HomePageSmokeTests.cs               # five steps via MudStepper, not .step-card
├── HomePageWorkflowTests.cs            # current/complete/upcoming without card CSS
├── HomePageIdentityContextTests.cs     # email, tenant, Profile href
├── HomePageCommercialAccessTests.cs    # login gate; no stepper
├── HomePageCommercialRetentionTests.cs # paused access; no stepper; Open profile
├── ProfilePageTests.cs                 # expect no change
└── TestInfrastructure/HomePageTestContext.cs
```

**Structure Decision**: Keep the existing Home feature folder. Do not add Application types. Optional small Web-only child components (`HomeWizardStepper`, `HomeImportSummaryRail`) are allowed if they keep `Home.razor` readable; they must remain presentation-only.

## Complexity Tracking

> None.

## Phase 0 Research

See [research.md](./research.md). All Technical Context items are resolved from Home source, MudBlazor 9 stepper behaviour, issue #117, and the repository MudBlazor skill. No remaining NEEDS CLARIFICATION.

## Phase 1 Design

- [data-model.md](./data-model.md) — presentation state: viewed step, layout mode, summary context.
- [contracts/wizard-layout-ui-contract.md](./contracts/wizard-layout-ui-contract.md) — regions, gates, chrome, alerts, testable markup.
- [quickstart.md](./quickstart.md) — bUnit, format, commercial on/off, optional Aspire visual check.

## Implementation sketch (for `/speckit-tasks`)

1. **Phase 1 (P1)**: Introduce `viewedStep` (1–5) on Home. Bind vertical `MudStepper` / `MudStep` (`Completed`, `Disabled`, `Title`, `SecondaryText` from existing summaries). Render only the viewed step’s form in a sibling pane. Suppress stepper Next/Previous/Reset. Keep header `MudPaper` and commercial/self-host gates. Remove five `MudPaper` step cards and `.step-card` CSS. Update smoke/workflow tests to `MudStepper`/`MudStep`.
2. **Phase 1 chrome (P1)**: Assert wizard (`MudStepper`, working pane, later the rail) is absent on commercial login and deleted-account gates; Profile only when commercial and signed in; self-host unchanged.
3. **Phase 2 (P2)**: `MudExpansionPanels` for steps 1–3 (collapsed when complete and not viewed). Sticky summary rail (`MudPaper` + chips) with location, plan, file, preview/execution status, and Open in Planner when a plan id exists.
4. **Phase 3 (P3)**: Execution report summary as count chips/cards before tables; optional preview counts; manual-action type chips. Do not change report data.
5. **Phase 4 (P4, optional)**: `MudHidden` / `MudDrawer` (or compact stacked stepper) below `md`. Keep theme, Profile, Sign out reachable.
6. `dotnet test` on Web.Tests (and full solution if time). `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes`. If AppHost is running, `aspire resource web rebuild`.

## Architecture impact statement

| Topic | Statement |
|-------|-----------|
| Dependency direction | Web and Web.Tests only. |
| Boundaries | Viewed-step and panel expansion are UI state. Coordinator, `WorkflowCoordinationState`, presenters, Commercial use cases unchanged. |
| Adapters | MudBlazor remains the UI adapter. Autocomplete, file upload, and data grids stay as-is. |
| Traceability | Spec 010 FR-001–023 and issue #117 phases. |
| Testability | bUnit for layout, gates, and existing workflow progression; no production deploy required. |
| Errors | Existing alerts and restore messages; de-duplicate global vs step copies of the same message. |
| Security | Commercial Profile and gates unchanged; no new privileged actions. |
| Self-host | Commercial mode off still shows the wizard after Microsoft 365 sign-in, without Profile or commercial gates. |
