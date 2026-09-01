# Research: Import Wizard Stepper Layout

**Feature**: 010-wizard-stepper-layout  
**Date**: 2026-09-01  
**Status**: Complete — all unknowns resolved

## Summary

Home today is a single-column `MudContainer` with a header `MudPaper` (title or gate copy + theme + identity) and five always-visible `MudPaper` step cards. Step state already exists in `Home.StepPresentation.razor.cs` (`IsStepComplete`, `IsStepLocked`, `ActiveStep`, summaries). Spec 002 explicitly deferred collapsible steps and rejected `MudStepper` in favour of a scrolling card stack. This feature reverses that presentation choice without changing workflow policy.

MudBlazor 9.9.0 includes `MudStepper` / `MudStep` (`Vertical`, `NonLinear`, `@bind-ActiveIndex`, `Completed`, `Disabled`, `OnPreviewInteraction`). Official examples place each step’s body inside `MudStep` ChildContent, which yields a single column. The approved mock-up needs a left step list, a centre pane, and a right rail, so the stepper is used as **navigation**, not as the sole content host.

No Technical Context item remains NEEDS CLARIFICATION.

---

## Decision 1: MudStepper as left navigation, forms in a sibling pane

**Decision**: Render a vertical `MudStepper` with five `MudStep` instances for titles, completion, lock, and compact `SecondaryText` summaries. Keep each step’s form (autocomplete, upload, preview grids, confirm + report) in a **sibling** centre pane switched on a Home-owned `viewedStep` (1–5). Do not rely on MudStepper ChildContent to build the three-column layout.

**Rationale**: Vertical `MudStepper` with bodies inside each `MudStep` stacks label-plus-content in one column, which recreates the long page. Empty (or near-empty) step bodies plus a sibling pane match issue #117’s left / centre / right model while still using the official stepper for ARIA and completion ticks.

**Alternatives considered**:
- Five `MudPaper` cards (status quo): Rejected — this is the problem.
- All forms inside `MudStep` ChildContent: Rejected for the target desktop layout; acceptable only as a last-resort fallback if stepper-as-nav cannot hide empty bodies cleanly.
- Custom `MudNavLink` list instead of `MudStepper`: Rejected — issue #117 and the spec require a stepper control from the existing library.
- Tailwind sidebar wizard: Rejected in #117.

---

## Decision 2: Viewed-step index is new UI state (not coordinator state)

**Decision**: Add `viewedStep` (integer 1–5) on the Home component. Map `MudStepper` `@bind-ActiveIndex` to `viewedStep - 1`. Initialise and auto-advance `viewedStep` to the existing computed `ActiveStep` (first incomplete unlocked step) when the user completes work, or to 5 when all steps are complete so the execution report stays on screen. Clicking an unlocked step updates `viewedStep` only. Do not add this field to `WorkflowCoordinationState` or `ImportWorkflowCoordinator`.

**Rationale**: Today every step is visible, so `ActiveStep` is only a highlight. A single pane requires an explicit “which form is showing” value that can differ from “next incomplete step” when the user goes back.

**Alternatives considered**:
- Drive the pane solely from `ActiveStep`: Rejected — users could not reopen location/plan/CSV after preview.
- Persist viewed step in session storage: Out of scope; page lifetime is enough.

---

## Decision 3: Non-linear stepper with lock enforcement

**Decision**: Set `NonLinear="true"` so users can return to earlier unlocked steps. Set each `MudStep.Disabled` from `IsStepLocked`. Use `OnPreviewInteraction` (or equivalent cancel) to prevent moving to a locked step. Hide built-in Next / Previous / Reset (`ShowResetButton="false"` and empty or suppressed `ActionContent`) so the only primary actions remain **Preview import** and **Confirm import** on step 4.

**Amendment (2026-09-01)**: Dogfooding showed that splitting preview and confirm across stepper panes felt wrong. Step 4 is now **Preview and confirm** (preview + confirm import); step 5 is **Report** only. Gating is unchanged.

**Rationale**: Existing lock rules (`IsStepLocked`) already encode prerequisites. Linear mode would fight “go back and change location”. Default stepper footer would duplicate and confuse those labels.

**Alternatives considered**:
- Linear stepper: Rejected — changing a completed setup step is required (stale-preview path).
- Keep stepper Next as a synonym for Preview: Rejected — step 4/5 already have named buttons.

---

## Decision 4: Header stays on Home, not MainLayout

**Decision**: Keep theme, email, tenant name, Sign out, Sign in, and commercial Profile in the existing full-width header `MudPaper` on Home. Do not introduce `MudAppBar` in `MainLayout.razor` for this feature. Gate headings (login / paused access / CSV to Planner Import) stay in that header.

**Rationale**: `MainLayout` has no app bar today. Moving chrome would risk duplicating identity on `/profile` or breaking tests that look at Home markup. Spec allows an app bar only if cleaner; keeping the header is lower risk.

**Alternatives considered**:
- Shared header component reused by Profile: Optional later; not MVP.
- `MudAppBar` in MainLayout: Deferred; would restyle Profile by side effect (out of scope).

---

## Decision 5: Commercial gates hide the entire wizard

**Decision**: Render `MudStepper`, working pane, and summary rail only in the existing `else` branch that today shows the five cards (not `showCommercialLoginGate`, not `showCommercialDeletedAccountGate`). Global workflow alerts (first-sign-in success, status, tenant mismatch, admin consent, unsupported account) stay **above** the stepper inside that same allowed-to-import branch.

**Rationale**: Matches FR-010/011 and current tests that assert login gate copy and absence of “Select Planner location”.

**Alternatives considered**:
- Disabled stepper behind the gate: Rejected — spec forbids showing the stepper.

---

## Decision 6: Phase 2 collapsible setup and summary rail

**Decision**: After Phase 1, wrap steps 1–3 forms in `MudExpansionPanels`. Default `IsExpanded` to true only when `viewedStep` equals that step; completed setup panels collapse when the user is on preview or confirm. Summary rail is a `MudItem` (`md` ~3) with `MudPaper` + `MudStack` + `MudChip`, `Class` using MudBlazor position utilities (`sticky` / `top-*`) if they exist; isolated CSS only if utilities cannot pin the rail. Include Open in Planner when `preview.Preview.PlanId` or `executionResult.PlanId` is present (same URLs as today).

**Rationale**: Skill maps “expandable panel” to `MudExpansionPanel` and “page section” to `MudPaper`. Sticky context replaces information lost when inactive forms are hidden.

**Alternatives considered**:
- Accordion for all five steps instead of stepper: Rejected — stepper is the progress affordance.
- Summary inside the header: Rejected — header is identity/theme; spec wants a right rail on wide layouts.

---

## Decision 7: Phase 3 report polish stays in HomeExecutionReport

**Decision**: Keep `MudTabs`. Put created / reused-or-skipped / manual / error **counts** as `MudChip` or compact `MudPaper` stats at the top of the Summary tab before existing tables. Optional compact counts on the preview step above existing grids. Manual action type as `MudChip` colour by type. Do not change view-model fields.

**Rationale**: Spec P3 is scanability, not new meaning. Report component already owns this UI.

---

## Decision 8: Phase 4 optional narrow layout

**Decision**: Treat as optional. Prefer `MudGrid` stacking (stepper full width, then pane, then summary) before a `MudDrawer`. If a drawer is used, bind `@bind-Open` and keep header chrome outside the drawer. Use `MudHidden` for breakpoint show/hide rather than new media-query CSS where possible.

**Rationale**: Spec P4 is optional if stacking remains usable. Skill prefers `MudHidden` over custom breakpoints.

---

## Decision 9: Tests assert components and copy, not `.step-card`

**Decision**: Replace `.step-card` / `--current` / `--completed` / `--upcoming` CSS queries with `FindComponent<MudStepper>()`, `FindComponents<MudStep>()`, disabled/completed parameters, and existing copy assertions. Add assertions that commercial gates do not render `MudStepper`. Keep `HomePageIdentityContextTests` and `ProfilePageTests` behaviour. Prefer not to depend on undocumented MudBlazor internals beyond public parameters and visible text.

**Rationale**: Smoke tests currently require five `.step-card` nodes. That class is deleted with the card stack.

**Alternatives considered**:
- Playwright scroll-length check: Not required; repo has no Playwright suite. Manual Aspire check is in quickstart.

---

## Decision 10: Minimal CSS and no coordinator changes

**Decision**: Delete `.step-card*` rules. Keep `.step-intro` guidance width if still needed. Do not edit `ImportWorkflowCoordinator`, commercial use cases, Graph, or `/profile`. Retain `MudAutocomplete`, `MudFileUpload`, `MudDataGrid`, `MudSimpleTable`.

**Rationale**: Constitution II/III and spec out of scope. MudBlazor skill: custom CSS last.

---

## Dependencies and integrations (best-practice notes)

| Topic | Practice |
|-------|----------|
| MudBlazor composition | Component → `MudGrid`/`MudStack`/`MudPaper` → utility classes → theme → isolated CSS |
| Stepper index | Two-way bind `ActiveIndex`; use `Completed`/`Disabled` parameters (not undocumented `CompletedState`) |
| Alerts | One `MudAlert` per condition; first-sign-in stays global above wizard |
| UK English | No new US spelling in strings |
| Format | `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` |
| Aspire | `aspire resource web rebuild` after Web changes if AppHost is running |
