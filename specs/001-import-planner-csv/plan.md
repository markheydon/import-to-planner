# Implementation Plan: CSV To Planner Import Workflow

**Branch**: `[001-spec-reality-alignment]` | **Date**: 2026-05-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-import-planner-csv/spec.md`

**Note**: This plan specifies the implementation approach for the CSV import workflow feature, including architectural decisions, testing strategy, and delivery phases.

## Summary

Implement a Blazor CSV-to-Planner import workflow with safe validation, dry-run preview, explicit confirmation, stale-preview blocking, partial-success execution, manual follow-up actions, and consistent runtime-mode behaviour across in-memory and Graph-backed execution. The plan also establishes a dedicated Blazor UI test host so execution-report rendering tests can be implemented as first-class automated tests.

## Technical Context

**Language/Version**: C# on .NET 10 LTS SDK (`10.0.100`)  
**Primary Dependencies**: `Microsoft.FluentUI.AspNetCore.Components`, `Microsoft.Graph`, `Microsoft.Identity.Web`, `CsvHelper`  
**Storage**: N/A (Planner/Graph is external system of record; no local persistence)  
**Testing**: xUnit + `Microsoft.NET.Test.Sdk` + `coverlet.collector` in `tests/ImportToPlanner.Tests`, plus dedicated Blazor UI component/workflow testing in `tests/ImportToPlanner.Web.Tests` (bUnit-based)  
**Target Platform**: ASP.NET Core Blazor web app with Aspire AppHost support, local development, and Codespaces  
**Project Type**: Layered web application (`Web`, `Application`, `Domain`, `Infrastructure.Graph`, plus `ServiceDefaults`)  
**Performance Goals**: Preview for up to 500 valid rows completes within 10 seconds for 95% of runs  
**Constraints**: Single-tenant scope; Graph beta volatility; no secrets in logs/UI; stale preview blocks execution; transient row failures retry once; CSV upload limited to 10 MB  
**Scale/Scope**: Single-use, operator-driven import sessions with hundreds of rows per import, one plan targeted per execution

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Gate**: PASS. The feature maintains the Domain/Application/Infrastructure/Web architectural split; changes are concentrated in CSV parsing, orchestration, and the Blazor UI flow.
- **Testing Gate**: PASS. The spec and task list cover parser validation, preview/execution gating, stale-preview blocking, retry-once behaviour, runtime-mode parity, reporting, and user-safe error mapping. The remaining gap for UI rendering coverage is now explicitly addressed by prerequisite UI test-host tasks.
- **UX Consistency Gate**: PASS. The feature implements a validation → preview → confirm → execute → report journey using the Fluent UI application shell.
- **Performance Gate**: PASS. The plan requires preview for 500 rows to complete within 10 seconds (p95) and avoids unnecessary repeated remote calls in hot paths.
- **Operations Gate**: PASS. Dry-run MUST stay non-destructive; user-safe error mapping, authentication gating, and manual-action reporting MUST be explicit.
- **Runtime Mode Compatibility Gate**: PASS. Behaviour MUST be validated for both in-memory and Graph modes; mode-entry auth behaviour MUST be documented and testable.
- **Graph Contract Volatility Gate**: PASS. Graph-facing behaviour MUST be implemented behind the gateway abstraction and use user-safe error mapping.
- **Scope Boundary Gate**: PASS. The feature MUST remain single-tenant and MUST NOT introduce multi-tenant behaviour.
- **CI and AppHost Gate**: PASS. The solution MUST maintain AppHost/Aspire build viability, including `ImportToPlanner.ServiceDefaults`.

Design artifacts (`research.md`, `data-model.md`, `contracts/import-workflow-contract.md`, `quickstart.md`, `tasks.md`) are aligned with constitutional gates.

## Project Structure

### Documentation (this feature)

```text
specs/001-import-planner-csv/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
apphost.cs

src/
├── ImportToPlanner.Web/
│   ├── Components/
│   │   ├── Pages/Home.razor
│   │   └── ...
│   ├── Program.cs
│   └── Properties/
├── ImportToPlanner.Application/
│   ├── Abstractions/
│   ├── Exceptions/
│   ├── Models/
│   └── Services/
├── ImportToPlanner.Domain/
├── ImportToPlanner.Infrastructure.Graph/
└── ImportToPlanner.ServiceDefaults/

tests/
├── ImportToPlanner.Tests/
└── ImportToPlanner.Web.Tests/
```

**Structure Decision**: Keep the current layered monorepo structure and implement feature behaviour through `ImportToPlanner.Application` services and `ImportToPlanner.Web` interaction flow, with gateway behaviour provided by `ImportToPlanner.Infrastructure.Graph`, runtime wiring and operational defaults provided by `ImportToPlanner.ServiceDefaults`, and validation split between core behaviour tests in `tests/ImportToPlanner.Tests` and UI rendering/workflow tests in `tests/ImportToPlanner.Web.Tests`.

## Complexity Tracking

No constitution violations require justification for this feature.
