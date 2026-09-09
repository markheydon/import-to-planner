# Implementation Plan: Detect CSV Delimiter and UTF-8 BOM

**Branch**: `012-csv-delimiter-bom` | **Date**: 2026-09-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/012-csv-delimiter-bom/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Excel locale CSV (semicolon separators) and Excel “CSV UTF-8” (leading BOM) currently parse as a missing `Task Name` heading. Extend the existing Infrastructure CSV adapter to strip a leading BOM, choose comma or semicolon from an unambiguous header, and fail the file with a clear `File`-level error when the separator is mixed or unsupported. Keep CsvHelper for quoted-field parsing, preserve the 10 MB upload limit and ignore-extra-columns behaviour, update public CSV/troubleshooting docs, and cover the cases with parser unit tests. No delimiter picker and no `.xlsx` support.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK from `global.json`: 10.0.300, `rollForward: latestFeature`)

**Primary Dependencies**: CsvHelper (already in `ImportToPlanner.Infrastructure.Graph`), existing `ICsvImportParser` / `CsvParseResult`, MudBlazor Home validation grid (display only), public docs under `docs/`

**Storage**: N/A — no new persistence

**Testing**: xUnit v3 unit tests in `ImportToPlanner.Tests` (`CsvImportParserTests`); architecture compliance unchanged; no AppHost tests; no Playwright for this increment

**Target Platform**: ASP.NET Core Blazor app (hosted and self-hosted); same parser in both modes

**Project Type**: Layered Blazor web application

**Performance Goals**: Detection is a single header-line scan on files already capped at 10 MB; no extra network calls

**Constraints**: UK English user-facing messages; constitution dependency rule (CsvHelper stays out of Application/Domain); detection-only (no UI delimiter control); UTF-8 only; mapping (#127) must see already-split headers; workbook import remains #55

**Scale/Scope**: One adapter class, one test class, two public docs pages (FAQ optional), no new projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Dependency Direction**: Pass. CsvHelper and file-format sniffing stay in `ImportToPlanner.Infrastructure.Graph`. Application contract `ICsvImportParser` is unchanged.
- **II. Core Policy Neutrality**: Pass. Separator policy is expressed as repository-owned `ImportValidationError` outcomes, not CsvHelper types leaking inward.
- **III. Boundary Explicitness**: Pass. Coordinator already consumes `CsvParseResult`. No new use-case prose in Application. Web continues to render `Message` in the existing grid.
- **IV. Replaceability**: Pass. CsvHelper remains an outer adapter choice; sniffing is adapter-local so a future parser could reuse the same `ICsvImportParser` contract.
- **V. Traceability**: Pass. Behaviour maps to issue #133 and spec FR-001–FR-012.
- **VI. Testability**: Pass. Parser unit tests can fail without deployment or Graph.
- **VII. Explicit errors**: Pass. Ambiguous/unsupported separators are structured file-level errors, not exceptions or silent comma fallback.
- **VIII. Security**: Pass. Untrusted CSV already parsed in-process; no new secrets; messages must not dump file contents; no workbook/OLE load.
- **IX. Quality evidence**: Pass. Quickstart lists `CsvImportParserTests`, architecture filter, format verify, and docs review.
- **X. Self-hosted viability**: Pass. Same parser registration; no hosted-only dependency.

## Project Structure

### Documentation (this feature)

```text
specs/012-csv-delimiter-bom/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── csv-delimiter-bom-contracts.md
└── tasks.md             # Phase 2 (/speckit-tasks) — not created by /speckit-plan
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Infrastructure.Graph/
│   └── Import/
│       └── CsvImportParser.cs          ← BOM strip, header sniff, explicit Delimiter
├── ImportToPlanner.Application/
│   ├── Abstractions/ICsvImportParser.cs          ← unchanged signature
│   └── Models/CsvParseResult.cs, ImportValidationError.cs
└── ImportToPlanner.Web/
    └── Features/Import/…               ← no delimiter control; existing ParseErrors grid

docs/
├── csv-format.md                       ← locale Excel, BOM, supported separators
└── troubleshooting.md                  ← file-level separator vs missing heading

tests/
└── ImportToPlanner.Tests/
    └── CsvImportParserTests.cs         ← comma, semicolon, BOM, quotes, ambiguous, unsupported
```

**Structure Decision**: Smallest change is adapter-local detection inside the existing Graph-infrastructure parser plus docs and unit tests. Web already blocks preview on `HasErrors` and lists file-level errors (`RowNumber = 0`). Do not add Domain types or a new Application service.

## Complexity Tracking

No constitution violations. No extra projects or cross-layer abstractions.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Quote-aware unquoted comma/semicolon counts on the header; fail closed when mixed; single-column defaults to comma; tab/pipe unsupported.
2. Strip U+FEFF in the parser, not only on upload `StreamReader`.
3. Keep CsvHelper for records with an explicit `Delimiter`; do not use CsvHelper `DetectDelimiter`.
4. File-level `Field = "File"` errors; skip heading validation until a delimiter is chosen.
5. Unchanged `ICsvImportParser` signature; mapping (#127) consumes parser output later.
6. Docs on `/csv-format` and `/troubleshooting`; tests in `CsvImportParserTests` only for this increment.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and [contracts/csv-delimiter-bom-contracts.md](contracts/csv-delimiter-bom-contracts.md).

Key design outcomes:

- Transient `DetectedFieldSeparator` values (comma, semicolon, single-column, ambiguous, unsupported) never persist.
- Parser order: cancel → BOM strip → empty check → sniff → CsvHelper → existing heading/row rules.
- Web UI unchanged except it will display the new file-level messages through the existing grid.
- Public examples stay comma-separated UTF-8 without a BOM.

### Architecture impact statement

- **Dependency direction**: Unchanged. Infrastructure → Application models only.
- **Boundary changes**: None to Application/Domain APIs. Detection is a private step inside `CsvImportParser`.
- **Adapter responsibilities**: Infrastructure owns format sniffing, CsvHelper configuration, and structured validation errors. Web owns 10 MB upload and presentation of `ImportValidationError`. Docs own Excel-locale guidance.
- **Traceability**: #133 / FR-001–FR-012.
- **Testability**: Parser tests without Web or Graph.
- **Error handling**: One file-level error for delimiter failures; coordinator already stops preview.
- **Security trust boundary**: Untrusted CSV text; no workbook parser; no secret logging.
- **Self-hosted**: Same code path as hosted.

### Post-design Constitution Check

All gates remain Pass. No Complexity Tracking entries.

## Implementation notes (for `/speckit-tasks`)

- Add a private header scanner in `CsvImportParser` (or a small internal helper in the same project). Keep it free of user-facing layout concerns.
- Set CsvHelper `Delimiter` after detection; leave `PrepareHeaderForMatch` trim/lower as today so BOM cannot hide in the first header after strip.
- Assert ambiguous errors do **not** contain “Task Name column is required”.
- Update docs with the [end-user-docs](../../.github/skills/end-user-docs/SKILL.md) voice (UK English, non-technical).
- After code changes, run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`.
