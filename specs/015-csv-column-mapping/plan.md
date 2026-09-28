# Implementation Plan: CSV Column Mapping and Sample Files

**Branch**: `015-csv-column-mapping` | **Date**: 2026-09-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/015-csv-column-mapping/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

First-time and repeat CSV importers need downloadable examples, synonym header support, optional mapping UI, and browser-local layout memory — without duplicating delimiter logic (#133) or Excel work (#55). Add an Application-level column catalog and mapping service, extend `ICsvImportParser` with header peek and mapping-aware parse, ship `wwwroot` sample files, extend the Home upload step (MudBlazor) with downloads/mapping summary/editor, persist confirmed layouts in `localStorage`, and align public CSV docs with the same samples and alias guidance.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK from `global.json`: 10.0.300, `rollForward: latestFeature`)

**Primary Dependencies**: Existing `ICsvImportParser` / `CsvParseResult` / `CsvTaskRow`, CsvHelper (Infrastructure only), MudBlazor Home wizard, `ImportWorkflowCoordinator`, public docs under `docs/`

**Storage**: Browser `localStorage` for saved layout mappings (Web adapter only); no server persistence in v1

**Testing**: xUnit v3 in `ImportToPlanner.Tests` (new Application mapping tests + extended `CsvImportParserTests`); architecture compliance unchanged; no AppHost tests; no Playwright for this increment

**Target Platform**: ASP.NET Core Blazor app (hosted and self-hosted)

**Project Type**: Layered Blazor web application (Clean Architecture)

**Performance Goals**: Mapping proposal is O(headers × fields) on files already capped at 10 MB; no Graph calls added for mapping

**Constraints**: UK English UI and docs; dependency rule (mapping policy in Application, CsvHelper in Infrastructure); block preview until Task Name mapped; never persist row data; Excel (#55) must reuse this layer later

**Scale/Scope**: Application catalog + service, parser interface extension, Web upload UI + layout store, two static sample files, docs updates, unit tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Dependency Direction**: Pass. Catalog and `ICsvColumnMappingService` in Application; `CsvImportParser` changes stay in Infrastructure; `localStorage` in Web only.
- **II. Core Policy Neutrality**: Pass. Aliases and layout rules are Application types; no MudBlazor or CsvHelper in Application/Domain.
- **III. Boundary Explicitness**: Pass. Coordinator consumes `CsvColumnMapping` and peek/proposal results; presenters keep user-facing strings in Web.
- **IV. Replaceability**: Pass. Mapping service is interface-based; parser remains swappable; layout store is a Web port.
- **V. Traceability**: Pass. Maps to issue #127 and spec FR-001–FR-017.
- **VI. Testability**: Pass. Mapping rules and parse-with-mapping covered without browser or Graph.
- **VII. Explicit errors**: Pass. Peek/mapping conflicts surface as structured `ImportValidationError`; no silent fallback when Task Name unmapped.
- **VIII. Security**: Pass. No new secrets; localStorage holds header labels and field ids only; no CSV body in storage.
- **IX. Quality evidence**: Pass. Quickstart lists tests, format verify, manual mapping checks, docs review.
- **X. Self-hosted viability**: Pass. Browser storage works without commercial accounts; samples served from wwwroot.

## Project Structure

### Documentation (this feature)

```text
specs/015-csv-column-mapping/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── csv-column-mapping-contracts.md
└── tasks.md             # Phase 2 (/speckit-tasks) — not created by /speckit-plan
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Application/
│   ├── Abstractions/ICsvColumnMappingService.cs     ← new
│   ├── Import/ImportColumnFieldCatalog.cs           ← canonical fields + aliases
│   ├── Models/CsvColumnMapping.cs, ColumnMappingProposal.cs, …
│   └── Services/CsvColumnMappingService.cs
├── ImportToPlanner.Infrastructure.Graph/
│   └── Import/CsvImportParser.cs                    ← PeekHeadersAsync, parse with mapping
├── ImportToPlanner.Web/
│   ├── Features/Import/Workflows/
│   │   ├── ImportWorkflowCoordinator.cs           ← peek, mapping gate before parse
│   │   └── WorkflowCoordinationState.cs           ← mapping session fields
│   ├── Features/Import/Pages/Home/                  ← samples, summary, mapping UI
│   └── Features/Import/Storage/ImportColumnMappingLayoutStore.cs  ← localStorage
└── ImportToPlanner.Web/wwwroot/samples/
    ├── import-minimal.csv
    └── import-full.csv

docs/
├── csv-format.md                                    ← samples + synonyms
└── getting-started.md                               ← link samples if applicable

tests/
└── ImportToPlanner.Tests/
    ├── CsvColumnMappingServiceTests.cs              ← new
    └── CsvImportParserTests.cs                      ← mapping + peek cases
```

**Structure Decision**: Introduce Application mapping policy and extend the existing parser adapter rather than a parallel Excel-only path. Web owns persistence and MudBlazor mapping UX; coordinator enforces preview gate.

## Complexity Tracking

No constitution violations. No new projects.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Application owns catalog, normalisation, proposals, and layout signatures; Infrastructure owns CSV IO.
2. `PeekHeadersAsync` + mapping-required `ParseAsync` extension on `ICsvImportParser`.
3. Single `ImportColumnFieldCatalog` for aliases (includes Due Date / Assigned To when enabled in parser).
4. Layout memory in browser `localStorage` via Web adapter.
5. Sample CSVs in `wwwroot/samples/` with upload-step download buttons.
6. Mapping UI on upload step; preview blocked until Task Name confirmed.
7. `ignoreExtraColumns` semantics preserved for unmapped headers.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and [contracts/csv-column-mapping-contracts.md](contracts/csv-column-mapping-contracts.md).

Key design outcomes:

- `ColumnMappingProposal.Status` drives whether the full editor is mandatory or a summary suffices.
- `SavedLayoutMapping` keyed by `LayoutSignature` only; merges with aliases for new columns.
- Parser equivalence: identity mapping on canonical headers matches pre-#127 behaviour.
- Issue #132 parser contract superseded for Assignee/Email aliases once catalog ships.

### Architecture impact statement

- **Dependency direction**: Application gains mapping types and service; Infrastructure parser depends on Application models only; Web depends on Application abstractions for mapping, implements storage port locally.
- **Boundary changes**: `ICsvImportParser` signature change (call sites: coordinator + tests). `ICsvColumnMappingService` registered in Application DI.
- **Adapter responsibilities**: Infrastructure reads cells by mapped source headers; Web persists layouts and renders mapping UI; docs mirror sample URLs.
- **Traceability**: #127 / FR-001–FR-017.
- **Testability**: Mapping service and parser mapping paths without UI.
- **Error handling**: Peek failures and mapping conflicts before row validation; Task Name missing blocks preview explicitly.
- **Security trust boundary**: Untrusted CSV headers only; storage excludes row content.
- **Self-hosted**: Same as hosted for samples and localStorage.

### Post-design Constitution Check

All gates remain Pass. No Complexity Tracking entries.

## Implementation notes (for `/speckit-tasks`)

- Register `ICsvColumnMappingService` in `ImportToPlanner.Application` DI; wire parser updates in Infrastructure DI unchanged lifetime.
- Update `ImportWorkflowCoordinator.BuildPreviewAsync` to require `ConfirmedColumnMapping` and pass it to `ParseAsync`.
- On file upload handler: peek → proposal → apply saved layout → set confirmation flags; invalidate preview when mapping or CSV changes.
- Add MudBlazor mapping dialog or inline panel per `mudblazor` skill; avoid custom CSS where components suffice.
- Generate full sample values from `docs/csv-format.md` rules (priority text, due date ISO, assignee placeholder emails).
- After code changes, run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`.
- Document in plan/tasks that Excel (#55) must call the same Application mapping service — no second alias list.
