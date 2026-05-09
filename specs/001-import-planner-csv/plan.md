# Implementation Plan: CSV To Planner Import Workflow

**Branch**: `[001-import-planner-csv]` | **Date**: 2026-05-09 | **Spec**: [/specs/001-import-planner-csv/spec.md](../001-import-planner-csv/spec.md)
**Input**: Feature specification from `/specs/001-import-planner-csv/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Deliver a safe CSV-to-Planner import workflow in the existing Blazor + Fluent UI web app,
with explicit validation, dry-run preview, explicit confirmation, and execution reporting.
The implementation will preserve layered architecture boundaries, keep runtime mode parity
between in-memory and Graph gateways, and enforce clarified execution rules: name-only
match detection, skip-on-match idempotency, partial success processing, single retry for
transient Graph row failures, and stale-preview execution blocking.

## Technical Context

**Language/Version**: C# on .NET 10 LTS SDK (`10.0.100`)  
**Primary Dependencies**: `Microsoft.FluentUI.AspNetCore.Components`, `Microsoft.Graph`, `Microsoft.Identity.Web`, `CsvHelper`  
**Storage**: N/A (no local persistence; Planner/Graph is external system of record)  
**Testing**: xUnit + `Microsoft.NET.Test.Sdk` + `coverlet.collector` in `tests/ImportToPlanner.Tests`  
**Target Platform**: ASP.NET Core Blazor web app (server-rendered interactive UI), local + Codespaces development
**Project Type**: Layered web application (`Web`, `Application`, `Domain`, `Infrastructure.Graph`)  
**Performance Goals**: Preview for up to 500 valid rows completes within 10 seconds for 95% of runs  
**Constraints**: Single-tenant scope; Graph beta volatility; no secrets in logs/UI; stale preview blocks execution; transient row failures retry once  
**Scale/Scope**: Single-use/operator-driven import sessions, hundreds of rows per import, one plan targeted per execution

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Gate**: PASS. Changes stay in Application orchestration and Web flow
  composition, preserving existing Domain/Application/Infrastructure/Web boundaries.
- **Testing Gate**: PASS. Plan includes unit and integration coverage for parser,
  orchestrator preview/execution logic, stale preview protection, retry policy, and
  mode parity.
- **UX Consistency Gate**: PASS. Existing Fluent UI flow in `Home.razor` keeps the same
  journey ordering (validate/preview -> confirm/execute -> execution report).
- **Performance Gate**: PASS. Preview timing target from spec is carried into validation,
  with explicit measurement on expected row volume.
- **Operations Gate**: PASS. Dry-run remains non-destructive; user-safe error handling,
  transient retry boundary, and explicit partial-result reporting are preserved.
- **Runtime Mode Compatibility Gate**: PASS. Behavioural checks will execute in both
  in-memory and Graph modes for impacted pathways.
- **Graph Contract Volatility Gate**: PASS. Graph-facing changes require compatibility
  notes and constrained retry semantics for transient failures.
- **Scope Boundary Gate**: PASS. Feature remains single-tenant with no multi-tenant
  expansion.
- **CI and AppHost Gate**: PASS. Solution and `apphost.cs` build/test paths remain part
  of acceptance validation.

Post-design re-check: PASS. Design artifacts (research, data model, contracts,
quickstart) remain aligned with all constitutional gates.

## Project Structure

### Documentation (this feature)

```text
specs/001-import-planner-csv/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
apphost.cs

src/
├── ImportToPlanner.Web/
│   ├── Components/
│   │   ├── Pages/Home.razor
│   │   └── ...
│   └── Program.cs
├── ImportToPlanner.Application/
│   ├── Abstractions/
│   ├── Models/
│   └── Services/
├── ImportToPlanner.Domain/
└── ImportToPlanner.Infrastructure.Graph/

tests/
└── ImportToPlanner.Tests/
```

**Structure Decision**: Keep the current layered monorepo structure and implement feature
behaviour through `ImportToPlanner.Application` services and `ImportToPlanner.Web`
interaction flow, with gateway behaviour provided by `ImportToPlanner.Infrastructure.Graph`
and validated via `tests/ImportToPlanner.Tests`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
