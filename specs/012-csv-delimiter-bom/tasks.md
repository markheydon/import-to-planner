# Tasks: Detect CSV Delimiter and UTF-8 BOM

**Input**: Design documents from `/specs/012-csv-delimiter-bom/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Parser unit tests are required for this increment (plan.md, quickstart.md, and engineering policies). Extend `CsvImportParserTests` only; no Playwright or AppHost tests.

**Organisation**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Parser adapter**: `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- **Application contract**: `src/ImportToPlanner.Application/Abstractions/ICsvImportParser.cs` (unchanged)
- **Unit tests**: `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- **Public docs**: `docs/csv-format.md`, `docs/troubleshooting.md`, optionally `docs/faq.md`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm feature context and contracts before parser changes

- [X] T001 Review parser boundary and detection algorithm in `specs/012-csv-delimiter-bom/contracts/csv-delimiter-bom-contracts.md` against `src/ImportToPlanner.Application/Abstractions/ICsvImportParser.cs`
- [X] T002 [P] Confirm quality gates and traceability checklist in `specs/012-csv-delimiter-bom/quickstart.md` match spec FR-001–FR-012

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared parser pipeline that all user stories depend on — BOM strip, header sniffing, explicit delimiter, and file-level failure paths

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add leading U+FEFF strip before empty-file and header checks in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T004 Add private quote-aware header-line scanner that counts unquoted commas and semicolons in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T005 Refactor `ParseAsync` flow to run cancel → BOM strip → empty check → separator sniff → CsvHelper with explicit `Delimiter` in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T006 Return single file-level `ImportValidationError` (`RowNumber = 0`, `Field = "File"`) for ambiguous headers and skip heading validation in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T007 Return single file-level `ImportValidationError` for unsupported separators (tab, pipe) and skip heading validation in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`
- [X] T008 Default single-column headers (no unquoted comma or semicolon) to comma delimiter in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`

**Checkpoint**: Foundation ready — delimiter detection runs before heading validation; user story tests and docs can proceed

---

## Phase 3: User Story 1 - Upload a semicolon CSV from Excel (Priority: P1) 🎯 MVP

**Goal**: Semicolon-delimited CSV files from UK/EU Excel locales parse with the same column interpretation as equivalent comma-delimited files

**Independent Test**: Upload a valid semicolon-delimited CSV whose first row uses accepted headings and confirm preview treats columns the same as an equivalent comma-delimited file

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation refinements**

- [X] T009 [P] [US1] Add semicolon-delimited parse success test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T010 [P] [US1] Add comma-vs-semicolon equivalence test (same task names and fields) in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T011 [P] [US1] Add semicolon file with extra column and `ignoreExtraColumns: true` success test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T012 [P] [US1] Add single-column header (`Task Name` only) success test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Verify semicolon delimiter path produces identical `CsvTaskRow` values to comma twin and does not emit false missing-heading errors in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`

**Checkpoint**: User Story 1 should be fully functional and independently testable

---

## Phase 4: User Story 2 - Upload a UTF-8 CSV that starts with a BOM (Priority: P1)

**Goal**: Leading UTF-8 BOM does not poison the first heading; BOM and semicolon handling work together

**Independent Test**: Upload a valid comma-delimited CSV whose first character is a UTF-8 BOM and confirm the required heading is recognised and preview proceeds

### Tests for User Story 2

- [X] T014 [P] [US2] Add comma-delimited BOM prefix test using `"\uFEFFTask Name,Description\n..."` in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T015 [P] [US2] Add semicolon-delimited BOM prefix test using `"\uFEFFTask Name;Description\n..."` in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T016 [P] [US2] Add regression test confirming CSV without BOM still parses successfully in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`

### Implementation for User Story 2

- [X] T017 [US2] Confirm BOM strip occurs before header sniffing and `PrepareHeaderForMatch` so first heading matches `Task Name` in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Understand why a file cannot be read (Priority: P2)

**Goal**: Ambiguous or unsupported separators produce clear file-level errors before preview, not false missing-column messages

**Independent Test**: Upload files where both comma and semicolon appear unquoted in the header, and files using tab or pipe separators; confirm file-level errors appear before preview

### Tests for User Story 3

- [X] T018 [P] [US3] Add ambiguous header test (`Task Name,Bucket;Goal`) asserting `Field = "File"` and no "Task Name column is required" message in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T019 [P] [US3] Add tab-separated header unsupported-separator test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T020 [P] [US3] Add pipe-separated header unsupported-separator test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`

### Implementation for User Story 3

- [X] T021 [US3] Finalise UK English ambiguous and unsupported separator messages per `specs/012-csv-delimiter-bom/contracts/csv-delimiter-bom-contracts.md` in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`

**Checkpoint**: File-level delimiter failures are explicit and distinct from heading validation

---

## Phase 6: User Story 4 - Keep quoted descriptions intact (Priority: P2)

**Goal**: Quoted fields containing commas or semicolons remain a single value for both recognised separators

**Independent Test**: Upload comma-delimited and semicolon-delimited files with quoted descriptions containing the other separator and confirm values appear as one field in preview

### Tests for User Story 4

- [X] T022 [P] [US4] Add comma-delimited file with quoted commas inside Description field test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T023 [P] [US4] Add semicolon-delimited file with quoted semicolons inside Description field test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`
- [X] T024 [P] [US4] Add header with quoted comma or semicolon that must not affect delimiter detection test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs`

### Implementation for User Story 4

- [X] T025 [US4] Confirm CsvHelper quote rules preserve field integrity after explicit `Delimiter` is set in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs`

**Checkpoint**: Quoted-field behaviour verified for both comma and semicolon files

---

## Phase 7: User Story 5 - Learn how Excel locale exports should look (Priority: P3)

**Goal**: Public CSV format and troubleshooting guidance describe Excel locale separators, UTF-8 BOM, supported separators, and sample-file encoding

**Independent Test**: Review public CSV format and troubleshooting pages and confirm they describe locale Excel CSV export, BOM, supported separators, and sample-file encoding

### Implementation for User Story 5

- [X] T026 [P] [US5] Update Excel locale semicolon, UTF-8 BOM tolerance, and supported separators in `docs/csv-format.md` (keep examples comma-separated UTF-8 without BOM)
- [X] T027 [P] [US5] Update file-level separator errors versus genuinely missing Task Name heading in `docs/troubleshooting.md`
- [X] T028 [US5] Add one short FAQ entry only if `docs/csv-format.md` and `docs/troubleshooting.md` do not already cover the topic concisely in `docs/faq.md`
- [X] T029 [US5] Verify embedded sample tables in `docs/csv-format.md` remain comma-separated UTF-8 without a BOM

**Checkpoint**: Public docs match parser behaviour and FR-010

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Quality gates, architecture compliance, and validation before merge

- [X] T030 Run parser unit tests with `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvImportParserTests"`
- [X] T031 [P] Run architecture compliance tests with `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"`
- [X] T032 Run format verification with `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- [X] T033 Execute optional manual checks from `specs/012-csv-delimiter-bom/quickstart.md` section 4 (semicolon upload, ambiguous header, comma regression)
- [X] T034 Confirm no delimiter picker was added and `ImportWorkflowCoordinator` still blocks preview on `HasErrors` in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS** all user stories
- **User Stories (Phases 3–7)**: All depend on Foundational completion
  - US1 and US2 (both P1) can proceed in parallel after Phase 2
  - US3 and US4 (both P2) can proceed in parallel after Phase 2
  - US5 (P3) can start after Phase 2; docs can be drafted in parallel with parser tests
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: After Foundational — no dependency on other stories
- **User Story 2 (P1)**: After Foundational — independent; BOM strip is in Foundational, story adds BOM-specific tests
- **User Story 3 (P2)**: After Foundational — independent; ambiguous/unsupported paths are in Foundational, story adds message and assertion tests
- **User Story 4 (P2)**: After Foundational — independent; relies on CsvHelper quote behaviour once delimiter is set
- **User Story 5 (P3)**: After Foundational — independent documentation work; can run in parallel with parser test phases

### Within Each User Story

- Tests MUST be written and FAIL before implementation refinements where applicable
- Foundational parser pipeline MUST complete before story-specific verification
- Docs (US5) can proceed in parallel with parser test phases once contracts are stable

### Parallel Opportunities

- T001 and T002 (Setup) can run in parallel
- After Phase 2, US1–US4 test tasks marked [P] within each story can run in parallel
- T026 and T027 (docs) can run in parallel
- T030 and T031 (automated checks) can run in parallel
- Different user stories can be worked on by different contributors after Foundational completes

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together:
Task T009: "Add semicolon-delimited parse success test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task T010: "Add comma-vs-semicolon equivalence test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task T011: "Add semicolon file with ignoreExtraColumns test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
Task T012: "Add single-column header success test in tests/ImportToPlanner.Tests/CsvImportParserTests.cs"
```

---

## Parallel Example: User Story 5

```bash
# Launch docs updates together:
Task T026: "Update docs/csv-format.md"
Task T027: "Update docs/troubleshooting.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (semicolon parsing and tests)
4. **STOP and VALIDATE**: Run `CsvImportParserTests` for semicolon cases
5. Demo with a semicolon Excel-export CSV if desired

### Incremental Delivery

1. Setup + Foundational → delimiter detection pipeline ready
2. Add User Story 1 → semicolon Excel CSV works (MVP)
3. Add User Story 2 → BOM-tolerant uploads
4. Add User Story 3 → clear file-level errors for bad separators
5. Add User Story 4 → quoted-field confidence
6. Add User Story 5 → public docs aligned
7. Polish → format verify, architecture tests, quickstart validation

### Parallel Team Strategy

With multiple developers after Foundational:

- Developer A: User Stories 1 and 2 (parser tests)
- Developer B: User Stories 3 and 4 (error and quote tests)
- Developer C: User Story 5 (docs)

---

## Notes

- Do not change `ICsvImportParser` signature or add Domain types
- Do not add a delimiter picker to the Web UI
- Do not add `.xlsx` support
- Construct BOM test strings with `"\uFEFF"` so fixtures are not stripped before `ParseAsync`
- Ambiguous/unsupported errors MUST NOT say "Task Name column is required"
- Sample downloads and doc examples stay comma-separated UTF-8 without BOM
- Web upload 10 MB limit and `ignoreExtraColumns` behaviour remain unchanged
