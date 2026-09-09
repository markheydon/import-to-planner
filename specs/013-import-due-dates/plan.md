# Implementation Plan: Import Due Dates From CSV

**Branch**: `013-import-due-dates` | **Date**: 2026-09-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/013-import-due-dates/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Accept optional CSV heading **Due Date**, parse ISO and UK day-first calendar dates in the existing Infrastructure parser, show the date on preview, and set Graph `dueDateTime` only when creating a task. Invalid non-empty values fail the row like Priority. Name-matched existing tasks stay skip-only. Inner models use `DateOnly?`; the Graph adapter maps to 10:00 UTC on that date. Update public CSV docs and Home accepted-fields copy. No aliases, start dates, or `.xlsx`.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK from `global.json`: 10.0.300, `rollForward: latestFeature`)

**Primary Dependencies**: Existing `ICsvImportParser` / `CsvTaskRow`, `IPlannerGateway.CreateTaskAsync`, Microsoft Graph `plannerTask.dueDateTime` (Infrastructure only), MudBlazor preview grid, public docs under `docs/`

**Storage**: N/A — no new persistence

**Testing**: xUnit v3 in `ImportToPlanner.Tests` (`CsvImportParserTests`, `ImportExecutionUseCaseTests`, `ImportPlanningUseCaseTests`, `GraphPlannerGatewayTests`); architecture compliance unchanged; no AppHost tests; no Playwright for this increment

**Target Platform**: ASP.NET Core Blazor app (hosted and self-hosted); same parser and gateway mapping in both modes

**Project Type**: Layered Blazor web application

**Performance Goals**: Per-row `DateOnly.TryParseExact` on files already capped at 10 MB; one Graph property on existing create POST (no extra round trip)

**Constraints**: UK English messages; constitution dependency rule (Graph types stay out of Application/Domain); create-only writes; exact heading `Due Date` (aliases #127); workbook import remains #55

**Scale/Scope**: Parser + Application records + gateway signature + preview column + docs; no new projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Dependency Direction**: Pass. CsvHelper and Graph `DueDateTime` stay in `ImportToPlanner.Infrastructure.Graph`. Application owns `DateOnly?` on repository models and `IPlannerGateway`.
- **II. Core Policy Neutrality**: Pass. Calendar-date policy is `DateOnly` and `ImportValidationError`, not Graph timestamps in inner layers.
- **III. Boundary Explicitness**: Pass. Parser maps CSV text → `CsvTaskRow`. Execution passes `DueDate` into the gateway. Web presenter formats dates for display.
- **IV. Replaceability**: Pass. A future Planner adapter can map `DateOnly` differently; Application does not depend on `dueDateTime`.
- **V. Traceability**: Pass. Behaviour maps to issue #131 and spec FR-001–FR-015.
- **VI. Testability**: Pass. Parser, planning, execution doubles, and Graph request-adapter tests fail without deployment.
- **VII. Explicit errors**: Pass. Invalid dates are structured row-level errors; skips are not silent updates.
- **VIII. Security**: Pass. Untrusted CSV already in-process; no new secrets; no workbook/OLE load; messages must not dump file contents.
- **IX. Quality evidence**: Pass. Quickstart lists parser, execution, Graph, architecture, format, and docs checks.
- **X. Self-hosted viability**: Pass. Same parser and gateway; no hosted-only date service.

## Project Structure

### Documentation (this feature)

```text
specs/013-import-due-dates/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── import-due-dates-contracts.md
└── tasks.md             # Phase 2 (/speckit-tasks) — not created by /speckit-plan
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Application/
│   ├── Models/CsvTaskRow.cs, ImportTaskPlanItem.cs
│   ├── Abstractions/IPlannerGateway.cs          ← CreateTaskAsync + DateOnly?
│   └── Services/ImportExecutionUseCase.cs, ImportFingerprintBuilder.cs,
│       ImportPlanningUseCase.cs
├── ImportToPlanner.Infrastructure.Graph/
│   ├── Import/CsvImportParser.cs                ← heading, parse, errors
│   └── Planner/GraphPlannerGateway.cs           ← dueDateTime on POST
└── ImportToPlanner.Web/
    ├── Features/Import/Pages/Home/Home.razor    ← accepted fields + preview column
    └── Features/Import/Presenters/ImportPlanningPresenter.cs

docs/
├── csv-format.md
└── troubleshooting.md

tests/
├── ImportToPlanner.Tests/
│   ├── CsvImportParserTests.cs
│   ├── Fixtures/with-extra-columns.csv          ← Due Date no longer “extra”
│   ├── ImportExecutionUseCaseTests.cs
│   ├── ImportPlanningUseCaseTests.cs
│   ├── GraphPlannerGatewayTests.cs
│   └── TestDoubles/PlannerGatewayStub.cs
└── ImportToPlanner.Web.Tests/                   ← gateway stub + copy tests if they assert accepted fields
```

**Structure Decision**: Extend the existing CSV → plan → create pipeline. Do not add Domain due-date entities or a new use case. Preview formatting stays in the Web presenter.

## Complexity Tracking

No constitution violations. Gateway arity increases by one parameter; a create-request DTO is deferred as unnecessary for this story.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Exact-format `DateOnly` parse in `CsvImportParser`; UK day-first; two-digit years 2000–2099; times and serials fail the row.
2. Inner type `DateOnly?`; Graph mapping only in the gateway.
3. Create POST uses `dueDateTime` at 10:00 UTC on the calendar date; no update on skip.
4. Preview column + fingerprint include due date; skip rows still display CSV date.
5. `Due Date` becomes a supported heading; extra-column fixture keeps a true extra such as `Owner`.
6. Docs and Home accepted-fields copy; tests across parser, planning, execution, Graph.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and [contracts/import-due-dates-contracts.md](contracts/import-due-dates-contracts.md).

Key design outcomes:

- `CsvTaskRow` / `ImportTaskPlanItem` gain `DueDate`.
- `IPlannerGateway.CreateTaskAsync` gains `DateOnly? dueDate`.
- Parser heading set includes `Due Date`.
- Graph adapter: 10:00Z mapping on create only.
- UI: Due date column; compact accepted fields updated.

### Architecture impact statement

- **Dependency direction**: Unchanged. Infrastructure → Application models; Application does not reference Graph.
- **Boundary changes**: `IPlannerGateway` and `CsvTaskRow` are additive. No Domain Graph fields.
- **Adapter responsibilities**: Parser owns formats and row errors. Graph gateway owns `dueDateTime`. Web owns UK display strings and docs-adjacent Home copy.
- **Traceability**: #131 / FR-001–FR-015.
- **Testability**: Parser and stubbed execution without live Planner; Graph tests use existing request adapter.
- **Error handling**: Row-level `Due Date` errors; coordinator already blocks on `HasErrors`.
- **Security trust boundary**: Untrusted CSV text; create still uses delegated session; no secret logging.
- **Self-hosted**: Same code path as hosted.

### Post-design Constitution Check

All gates remain Pass. No Complexity Tracking entries beyond noting positional gateway parameters.

## Implementation notes (for `/speckit-tasks`)

- Reuse Priority’s validate-then-`continue` loop; add `TryParseDueDate`.
- XML-document the new `CsvTaskRow` parameter and gateway `dueDate` per csharp-docs skill.
- Update every `CreateTaskAsync` implementer and call site in one change.
- Assert Graph POST JSON contains the 10:00Z timestamp, not midnight.
- Update extra-column tests that currently expect `Due Date` as unexpected.
- Docs: [end-user-docs](../../.github/skills/end-user-docs/SKILL.md) voice.
- After code changes, run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`.
