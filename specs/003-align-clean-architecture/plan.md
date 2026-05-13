# Implementation Plan: Clean Architecture Alignment

**Branch**: `003-align-clean-architecture` | **Date**: 2026-05-13 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/003-align-clean-architecture/spec.md`

## Summary

Refactor the import workflow so that CSV parsing implementation, provider-specific error
language, presentation wording, and page-level workflow coordination all move to the
correct architectural boundaries. The implementation splits the current orchestrator into
separate import planning and import execution use cases with explicit request, response,
and output-boundary contracts; removes Graph/API residue from core models; introduces
outer-layer presenters and page workflow collaborators; and adds measurable architecture
evidence plus regression coverage for both planner runtime modes.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK 10.0.100 via `global.json`)  
**Primary Dependencies**: Blazor Interactive Server, MudBlazor, Microsoft Graph SDK,
Microsoft Identity Web, CsvHelper, xUnit, bUnit  
**Storage**: N/A; planner state is external and this feature adds no repository storage  
**Testing**: xUnit unit and integration-style tests in `ImportToPlanner.Tests`, bUnit
component and workflow tests in `ImportToPlanner.Web.Tests`  
**Target Platform**: Blazor Server web application on Linux-hosted .NET runtime with
desktop and mobile browser support preserved for the primary workflow  
**Project Type**: Layered web application (`Web`, `Infrastructure`, `Application`,
`Domain`)  
**Performance Goals**: Preserve current preview and execution responsiveness; avoid new
avoidable remote calls in planning/execution flows; keep workflow coordination changes
neutral to perceived UI latency  
**Constraints**: Preserve validation, preview, confirmation, and execution semantics;
maintain UK English wording; keep Graph and MudBlazor replaceable outer-layer concerns;
verify planner behaviour parity in both `PlannerGateway:UseGraph` modes when affected  
**Scale/Scope**: Refactor across Application contracts/services/models, Domain plan model,
Infrastructure CSV and planner adapters, Web presenters/workflow collaborators, and the
corresponding unit plus bUnit test suites

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below.*

- **Dependency Direction Gate**: Planned changes preserve `Web/Infrastructure ->
  Application -> Domain`. CSV parsing implementation moves out of Application into an
  outer adapter, and presenter/workflow collaborators stay in Web.
- **Inner-Layer Purity Gate**: Application and Domain will use repository-owned request,
  response, and error concepts only. Graph-branded exception names, UI wording, and API
  residue are removed from inner layers.
- **Boundary Contract Gate**: Planning and execution each gain explicit request,
  response, and output-boundary contracts. Web presenters own wording and view-state
  shaping. Infrastructure adapters translate provider-specific failures into neutral
  application outcomes.
- **Replaceability Gate**: Framework and SDK choices remain outer-layer details. The
  plan does not treat Graph, CsvHelper, or MudBlazor as architectural invariants.
- **Architecture Evidence Gate**: Implementation must provide dependency-direction
  checks, forbidden-reference checks for Domain/Application, and focused tests proving
  that use cases emit structured data rather than presentation strings.
- **Policy Alignment Gate (Non-Constitutional)**: The plan captures the repository
  requirements from `docs-internal/engineering-policies.md`, `tests/README.md`, and
  `AGENTS.md`: smallest-practical regression tests first, runtime-mode parity checks when
  planner behaviour changes, UK English wording, responsiveness safeguards, and C# Expert
  delegation for code/test work.

## Project Structure

### Documentation (this feature)

```text
specs/003-align-clean-architecture/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── import-use-case-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Application/
│   ├── Abstractions/
│   │   ├── ICsvImportParser.cs
│   │   ├── IImportPlannerOrchestrator.cs                     ← to split/retire
│   │   ├── IImportPlanningUseCase.cs                         ← new
│   │   ├── IImportExecutionUseCase.cs                        ← new
│   │   ├── IImportPlanningOutputBoundary.cs                  ← new
│   │   └── IImportExecutionOutputBoundary.cs                 ← new
│   ├── Models/
│   │   ├── ImportRequest.cs                                  ← replace with planning/execution contracts
│   │   ├── ImportPlanPreview.cs                              ← neutral planning response shape
│   │   ├── ImportExecutionResult.cs                          ← neutral execution response shape
│   │   └── [supporting outcome/error models]                 ← new/updated
│   ├── Services/
│   │   ├── ImportPlannerOrchestrator.cs                      ← split/retire
│   │   ├── ImportPlanningUseCase.cs                          ← new
│   │   └── ImportExecutionUseCase.cs                         ← new
│   └── Exceptions/
│       └── PlannerGraphExceptions.cs                         ← replace with neutral failure taxonomy
├── ImportToPlanner.Domain/
│   └── PlannerPlan.cs                                        ← remove provider residue
├── ImportToPlanner.Infrastructure.Graph/
│   ├── CsvImportParser.cs                                    ← move implementation here or equivalent adapter namespace
│   ├── GraphPlannerGateway.cs                                ← map provider failures at adapter boundary
│   └── InMemoryPlannerGateway.cs                             ← keep parity with neutral contracts
└── ImportToPlanner.Web/
    ├── Components/Pages/Home.razor                           ← reduce to composition and bindings
    ├── Presenters/                                           ← new presenter implementations
    └── Workflows/                                            ← new stepped workflow coordinator/state collaborator(s)

tests/
├── ImportToPlanner.Tests/
│   ├── CsvImportParserTests.cs                               ← move/update for infrastructure parser
│   ├── ImportPlannerOrchestratorTests.cs                     ← replace with planning/execution use-case tests
│   └── GraphPlannerGatewayTests.cs                           ← extend only if adapter mapping changes
└── ImportToPlanner.Web.Tests/
    ├── HomePageWorkflowTests.cs                              ← update for workflow collaborator wiring
    └── TestInfrastructure/HomePageTestContext.cs             ← parameterise runtime mode coverage
```

**Structure Decision**: Keep the existing four-project architecture and refactor within
it. No new top-level project is required because the missing seams are boundary and
ownership issues, not packaging gaps.

## Complexity Tracking

No constitution gate violations are planned. No complexity override is required.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. CSV parsing remains an Application boundary but its `CsvHelper` implementation moves
   to Infrastructure.
2. The current orchestrator is replaced by separate planning and execution use cases.
3. Output-boundary interfaces are required for both use cases, with Web-owned presenters.
4. Inner-layer Graph-branded exceptions are replaced by neutral failure categories and
   outcome data.
5. `PlannerPlan` is reduced to business concepts only; provider metadata stays in
   adapters.
6. Workflow coordination moves out of `Home.razor` into dedicated Web collaborators.
7. Architecture evidence combines static dependency/reference checks with focused tests.
8. Planner runtime-mode parity must be covered whenever planner-facing behaviour changes.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and
[contracts/import-use-case-contracts.md](contracts/import-use-case-contracts.md).

Key design outcomes:

- Planning and execution use cases own business decisions only; presenters transform
  structured results into UI-facing wording.
- The Web layer owns workflow progression, stale-preview handling, and authentication-
  adjacent interaction flow without reintroducing business logic into Razor markup.
- Neutral failure and outcome models give both runtime modes a shared contract while
  leaving provider-specific translation in adapters.
- Test strategy is split into smallest-practical use-case tests first, then targeted Web
  workflow tests, then any required adapter/runtime-mode verification.

### Post-design Constitution Check

- **Dependency Direction Gate**: Pass. All new boundaries keep dependencies pointing
  inwards, and CSV/provider/presenter concerns remain in outer layers.
- **Inner-Layer Purity Gate**: Pass. The design removes provider names, transport
  residue, and UI wording from Domain/Application.
- **Boundary Contract Gate**: Pass. Planning and execution each have explicit request,
  response, and output-boundary ownership documented in the contracts artefact.
- **Replaceability Gate**: Pass. No framework or SDK is elevated into an architectural
  requirement.
- **Architecture Evidence Gate**: Pass. The quickstart and research artefacts define the
  evidence expected for dependency checks, forbidden references, boundary leakage, and
  runtime-mode verification.
- **Policy Alignment Gate**: Pass. The design preserves workflow semantics, UK English
  wording, runtime-mode checks, and repository delegation rules.
