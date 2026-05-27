# Implementation Plan: Refine Import Guidance

**Branch**: `006-refine-import-guidance` | **Date**: 2026-05-27 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/006-refine-import-guidance/spec.md`

## Summary

Refine the existing five-step import workflow so it reads as a clearer,
friendlier Planner import journey without changing the underlying workflow
semantics. The implementation concentrates on `Home.razor` copy and step-state
presentation, adds compact introduction guidance for CSV fields and manual
follow-up expectations, aligns the light-theme border token with the intended
Fluent-like neutral value, and updates focused web and architecture tests to
lock the new wording and workflow-state cues in place.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK 10.0.100 via `global.json`)  
**Primary Dependencies**: Blazor Interactive Server, MudBlazor, existing
workflow/presenter services in `ImportToPlanner.Web`, xUnit, bUnit  
**Storage**: N/A for this feature beyond the existing runtime configuration; no
new storage behaviour is introduced  
**Testing**: xUnit unit and architecture tests in `ImportToPlanner.Tests`, bUnit
web component tests in `ImportToPlanner.Web.Tests`  
**Target Platform**: ASP.NET Core Blazor web app running on Linux-hosted server
in desktop and mobile browsers  
**Project Type**: Layered Blazor web application  
**Performance Goals**: Preserve current import-page responsiveness and avoid any
new remote calls or extra workflow round-trips; keep first-render complexity in
the existing page-only range  
**Constraints**: Preserve existing validation, preview, confirmation, and
execution semantics; keep wording in UK English; do not add a navigation bar or
new workflow abstraction unless needed to support future navigation structure;
keep user-facing failures graceful and concise  
**Scale/Scope**: One main page component, one theme token file, one existing
workflow state presentation surface, and focused test updates across the two test
projects

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below.*

- **Dependency Direction Gate**: Pass. Planned changes remain in the Web layer
  and tests. No Domain or Application dependency changes are needed.
- **Core Policy Neutrality Gate**: Pass. The feature changes presentation text,
  view composition, and theme tokens only; no provider-specific policy is pushed
  inward.
- **Boundary Explicitness Gate**: Pass. Workflow services and presenter models
  stay unchanged unless a small Web-owned presentation helper is introduced. Any
  new helper remains on the UI side of the boundary.
- **Replaceability Gate**: Pass. MudBlazor remains an outer-layer UI choice; the
  plan refines usage rather than coupling inner layers to UI concerns.
- **Architecture Evidence Gate**: Pass. Implementation must provide focused bUnit
  evidence for updated copy and step rendering, plus architecture test evidence
  that guidance logic still avoids brittle status-message string scanning.
- **Policy Alignment Gate (Non-Constitutional)**: Pass. The plan preserves UX
  consistency across preview and confirmation, keeps wording in UK English,
  maintains responsive behaviour, and keeps the introduction concise rather than
  turning the page into long-form documentation.

## Project Structure

### Documentation (this feature)

```text
specs/006-refine-import-guidance/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── import-guidance-ui-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Web/
│   ├── Components/
│   │   └── Pages/
│   │       └── Home.razor                    ← introduction copy, step titles, action labels, state styling hooks
│   └── Themes/
│       └── ImportToPlannerTheme.cs          ← light-theme border token alignment

tests/
├── ImportToPlanner.Web.Tests/
│   ├── HomePageSmokeTests.cs                ← workflow structure and top-of-page content expectations
│   └── HomePageWorkflowTests.cs             ← step progression and user-safe wording checks
└── ImportToPlanner.Tests/
    └── ArchitectureComplianceTests.cs       ← retain non-brittle guidance/markup guardrails
```

**Structure Decision**: Keep the existing layered solution and implement the
feature entirely in the Web presentation layer plus focused test coverage. This
is the smallest change that satisfies the spec while preserving clean
architecture boundaries and leaving future navigation support as a UI-only
concern.

## Complexity Tracking

No constitution violations are planned. The work remains inside existing Web and
test surfaces and does not require new projects, new runtime services, or new
cross-layer abstractions.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Keep the five-card workflow and strengthen progression visibility with
   clearer current/completed/upcoming state treatment rather than adding a new
   navigation control.
2. Use concise action-oriented step titles with explicit Planner context,
   anchored by the clarified wording set.
3. Expand the top introduction with compact CSV guidance that names the required
   field, all accepted fields, and one concrete manual-follow-up example.
4. Keep manual-follow-up messaging concise by naming goals as the default
   example rather than enumerating every limitation.
5. Align the light-theme border token to `#d1d1d1` and keep the rest of the
   existing palette unchanged.
6. Verify the change through focused Home page bUnit tests and architecture
   checks rather than broad solution-wide behavioural changes.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md),
and [contracts/import-guidance-ui-contract.md](contracts/import-guidance-ui-contract.md).

Key design outcomes:

- The page keeps five step cards in the existing order, but each card now has a
  clearer presentational state contract: current, completed, or upcoming.
- The introduction becomes a compact expectation-setting summary rather than a
  documentation block, with one required-field cue, one accepted-fields list,
  and one manual-follow-up example.
- Action labels and card headings are normalised around sentence case and
  action-oriented wording that better fits future step-navigation reuse.
- Theme alignment is limited to the shared light-theme border token so the UI
  feels more Fluent-like without triggering a broader redesign.
- Existing bUnit and architecture test surfaces are sufficient to verify the
  feature without introducing a new UI test harness.

### Architecture impact statement

- **Dependency direction**: Unchanged. The work remains in `ImportToPlanner.Web`
  and test projects only.
- **Boundary changes**: None required across Application/Domain boundaries.
  Any extracted helper remains Web-owned and presentation-specific.
- **Adapter responsibilities**: Web continues to own all user-facing wording,
  step rendering, and responsive layout decisions. Theme token ownership stays
  in the Web theme adapter.

### Post-design Constitution Check

- **Dependency Direction Gate**: Pass. No inward dependency changes are planned.
- **Core Policy Neutrality Gate**: Pass. Business policy and planner contracts
  remain untouched.
- **Boundary Explicitness Gate**: Pass. Presentation changes stay in Web, with
  no user-facing prose introduced into Application responses.
- **Replaceability Gate**: Pass. MudBlazor usage remains an outer-layer detail.
- **Architecture Evidence Gate**: Pass. The quickstart defines focused test and
  manual verification evidence for workflow copy, state styling, and architecture
  guardrails.
- **Policy Alignment Gate**: Pass. The design preserves responsive behaviour,
  graceful copy, UK English wording, and existing safety semantics around preview
  and confirmation.
