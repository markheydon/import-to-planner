# Implementation Plan: Download Execution Report as CSV

**Branch**: `016-download-execution-report` | **Date**: 2026-09-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/016-download-execution-report/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

After import execution, operators need a session-local CSV artefact of the report (created, reused or skipped, failed, and manual follow-up) with source row numbers and user-safe reasons. Extend `ImportExecutionUseCase` to emit structured per-row outcomes, map them in `ImportExecutionPresenter` into an export projection, serialize with a new Application `ExecutionReportCsvExporter`, and add **Download report** on `HomeExecutionReport` via in-memory download (JS interop). Update `docs/import-workflow.md` and `docs/faq.md`. Unit tests cover filename, headers, mixed outcomes, and quoting.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK from `global.json`: 10.0.300, `rollForward: latestFeature`)

**Primary Dependencies**: Existing import execution stack (`ImportExecutionUseCase`, `ImportExecutionResult`, `ImportExecutionPresenter`, `HomeExecutionReport`), MudBlazor report UI, `IJSRuntime` / small `wwwroot` download helper, public docs under `docs/`

**Storage**: N/A — CSV generated on demand in memory; same session boundary as `WorkflowCoordinationState.ExecutionReport` (FR-009)

**Testing**: xUnit v3 in `ImportToPlanner.Tests` (CSV exporter + execution row population); `ImportToPlanner.Web.Tests` for download control visibility; architecture compliance filter; `dotnet format` gate per AGENTS.md

**Target Platform**: ASP.NET Core Blazor app (hosted and self-hosted); in-memory mode sufficient for automated coverage

**Project Type**: Layered Blazor web application (Clean Architecture)

**Performance Goals**: Export completes in sub-second time for typical imports (≤ thousands of rows); no extra Graph calls

**Constraints**: UK English headings and labels; no tenant secrets in CSV; no CsvHelper dependency in Application (manual RFC 4180 escaping); credits summary stays on-screen only; constitution dependency direction

**Scale/Scope**: Application models + exporter, execution use case touch, presenter + one Razor component, optional JS snippet, two doc pages, tests — no new projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Dependency Direction**: Pass. CSV serialization and structured outcomes in Application; MudBlazor and JS interop in Web only.
- **II. Core Policy Neutrality**: Pass. Outcome kinds are Application enums; user-safe prose applied in presenter before export.
- **III. Boundary Explicitness**: Pass. Execution use case returns structured `ImportSourceRowOutcome`; presenter maps to export rows; no UI strings in use case loop.
- **IV. Replaceability**: Pass. Download mechanism is Web adapter; export contract is plain CSV text.
- **V. Traceability**: Pass. Maps to issue #135 and spec FR-001–FR-013.
- **VI. Testability**: Pass. Exporter and row-building covered by unit tests without browser deployment.
- **VII. Explicit Errors**: Pass. Download disabled without report; export uses existing failure messages, not raw exceptions.
- **VIII. Security**: Pass. FR-010 exclusions encoded in contract; no new endpoints exposing data.
- **IX. Quality Evidence**: Pass. Quickstart lists tests, format verify, docs review.
- **X. Self-Hosted Viability**: Pass. Same Web component; no hosted-only dependency.

## Project Structure

### Documentation (this feature)

```text
specs/016-download-execution-report/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── execution-report-csv-contracts.md
└── tasks.md             # Phase 2 (/speckit-tasks) — not created by /speckit-plan
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Application/
│   ├── Models/
│   │   ├── ImportExecutionResult.cs          ← SourceRowOutcomes, ImportSourceRowOutcome
│   │   └── ImportSourceRowOutcome.cs         ← new
│   └── Services/
│       ├── ImportExecutionUseCase.cs         ← populate structured row outcomes
│       └── ExecutionReportCsvExporter.cs     ← new: UTF-8 CSV + filename
└── ImportToPlanner.Web/
    ├── Features/Import/
    │   ├── Presenters/ImportExecutionPresenter.cs   ← export row projection
    │   └── Pages/Home/HomeExecutionReport.razor     ← Download report + handler
    └── wwwroot/js/                                 ← optional download helper

docs/
├── import-workflow.md                        ← CSV download mention
└── faq.md                                    ← retry/share via download

tests/
├── ImportToPlanner.Tests/
│   ├── ExecutionReportCsvExporterTests.cs    ← new
│   └── ImportExecutionUseCaseTests.cs        ← extend for row outcomes
└── ImportToPlanner.Web.Tests/
    └── HomePageWorkflowTests.cs or dedicated ← download button visibility
```

**Structure Decision**: Smallest vertical slice: structured outcomes at execution time, presenter-owned user-safe strings, Application CSV writer, Web-only download trigger. Existing summary tabs can keep string lists until a later UI refactor.

## Complexity Tracking

No constitution violations. No extra packages or cross-layer leaks.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Add `ImportSourceRowOutcome` on `ImportExecutionResult`; build during execution loop.
2. Single CSV with `Record type` column; manual rows after source rows.
3. Comma UTF-8 without BOM; RFC 4180 quoting.
4. Filename `import-execution-report-{yyyyMMdd-HHmmss}Z.csv`.
5. Browser download via in-memory bytes + JS interop; no server file store.
6. Presenter-mapped reasons; omit credit columns from CSV.
7. Unit tests in `ImportToPlanner.Tests` for SC-005.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and [contracts/execution-report-csv-contracts.md](contracts/execution-report-csv-contracts.md).

Key design outcomes:

- Eight-column CSV contract with fixed UK English headers.
- Export projection decouples UI view model from serializer input.
- Failure rows gain row numbers at execution time where task actions are known.
- Public docs updated in same increment as UI (FR-011).

### Constitution Check (post-design)

All gates remain **Pass**; presenter retains user-facing wording; Application holds outcome enums and CSV structure only.

---

## Phase 2: Tasks

Not created by `/speckit-plan`. Run `/speckit-tasks` to generate `tasks.md`.
