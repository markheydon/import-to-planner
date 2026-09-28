---
description: "Task list for CSV column mapping and sample files (issue #127)"
---

# Tasks: CSV Column Mapping and Sample Files

**Input**: Design documents from `/specs/015-csv-column-mapping/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/csv-column-mapping-contracts.md, quickstart.md

**Tests**: Included per plan.md and quickstart.md (Application mapping service + extended parser tests; no Playwright for this increment).

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US5) for story-phase tasks only

## Path Conventions

Repository layout per plan.md: `src/ImportToPlanner.Application/`, `src/ImportToPlanner.Infrastructure.Graph/`, `src/ImportToPlanner.Web/`, `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`, `docs/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Static sample assets and feature folder scaffolding required by FR-001 and FR-002.

- [X] T001 Create sample directory `src/ImportToPlanner.Web/wwwroot/samples/` per plan.md
- [X] T002 [P] Add minimal example CSV `src/ImportToPlanner.Web/wwwroot/samples/import-minimal.csv` (comma-separated UTF-8; header row **Task Name** only; 2+ safe UK English placeholder task rows; no personal data)
- [X] T003 [P] Add full example CSV `src/ImportToPlanner.Web/wwwroot/samples/import-full.csv` (comma-separated UTF-8; canonical headers for all **IsEnabled** catalog fields at release time — Task Name, Description, Priority, Bucket, Goal, Due Date, Assigned To — with example values valid per `docs/csv-format.md`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Application mapping policy, parser contract extension, and coordinator session fields. **No user story work until this phase completes.**

**⚠️ CRITICAL**: Blocks US1–US5.

- [X] T004 Add `ColumnMappingProposalStatus` enum and related Application models in `src/ImportToPlanner.Application/Models/` (`CsvColumnMapping`, `ColumnMappingProposal`, `CsvHeaderPeekResult`, `SavedLayoutMapping`, `ImportColumnFieldDefinition`, mapping conflict records) per data-model.md field rules (`LayoutSignature` from ordered normalised headers; `Assignments` maps; Task Name required on `CsvColumnMapping`)
- [X] T005 Implement `ImportColumnFieldCatalog` in `src/ImportToPlanner.Application/Import/ImportColumnFieldCatalog.cs` as the sole v1 alias source (canonical headers, aliases from data-model.md including Title/Subject/Task/Name/TaskName/Task title → task-name; Notes/Body/Details/Comments → description; Pri/Importance → priority; Bucket Name/BucketName → bucket; Objective/Theme → goal; Due date/DueDate/Deadline → due-date; Assignee/Assignees/Email → assigned-to; **IsRequired** true only for Task Name; **MUST NOT** map Status → Bucket per FR-007)
- [X] T006 Implement header normalisation helpers (trim, invariant case fold, remove spaces and `.`, `-`, `_` for match/signature) in Application used by catalog and mapping service per contracts/csv-column-mapping-contracts.md
- [X] T007 Add `ICsvColumnMappingService` in `src/ImportToPlanner.Application/Abstractions/ICsvColumnMappingService.cs` with `BuildProposal` and `ToConfirmedMapping` signatures per contracts
- [X] T008 Implement `CsvColumnMappingService` in `src/ImportToPlanner.Application/Services/CsvColumnMappingService.cs` (layout signature; saved-layout seed; exact-then-alias assignment; duplicate `FieldId` → `Conflict`; missing Task Name → `NeedsTaskName`; `ToConfirmedMapping` rejects confirmation without Task Name)
- [X] T009 Register `ICsvColumnMappingService` → `CsvColumnMappingService` in `src/ImportToPlanner.Application/DependencyInjection.cs`
- [X] T010 Extend `ICsvImportParser` in `src/ImportToPlanner.Application/Abstractions/ICsvImportParser.cs` with `PeekHeadersAsync` and `ParseAsync(..., CsvColumnMapping columnMapping, bool ignoreExtraColumns)` per contracts (keep existing overload or migrate call sites in same task chain)
- [X] T011 Implement `PeekHeadersAsync` and mapping-aware `ParseAsync` in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs` (reuse BOM/delimiter path from #133; duplicate normalised headers → `RowNumber = 0` file-level errors; read cells via `columnMapping.Assignments`; Task Name required via mapping not literal header; unmapped headers follow `ignoreExtraColumns`; identity mapping on canonical headers MUST match pre-#127 row output)
- [X] T012 Add mapping session fields to `src/ImportToPlanner.Web/Features/Import/Workflows/WorkflowCoordinationState.cs` (`HeaderPeek`, `ColumnMappingProposal`, `ConfirmedColumnMapping`, `IsColumnMappingConfirmed`, `ShowMappingEditor`; invalidate when CSV cleared/replaced per data-model.md)
- [X] T013 [P] Create `tests/ImportToPlanner.Tests/CsvColumnMappingServiceTests.cs` covering normalisation, layout signature (`Task Name` vs `TaskName` same signature), alias proposal (`Title`/`Notes`), `Conflict`, `NeedsTaskName`, and saved-layout merge rules
- [X] T014 [P] Extend `tests/ImportToPlanner.Tests/CsvImportParserTests.cs` for `PeekHeadersAsync`, parse-with-mapping alias headers, `ignoreExtraColumns`, and identity-mapping equivalence with canonical headers

**Checkpoint**: Mapping service and parser compile; unit tests for core rules pass; coordinator state ready for workflow wiring.

---

## Phase 3: User Story 1 — Start from a valid example file (Priority: P1) 🎯 MVP

**Goal**: Download minimal or full sample CSV from the upload step and reach preview without manual column mapping when headers are canonical.

**Independent Test**: Download each sample from upload step, upload unchanged (or row edits only), reach preview with Task Name resolved and no manual mapping step (spec US1).

### Implementation for User Story 1

- [X] T015 [US1] Add MudBlazor download actions on upload step in `src/ImportToPlanner.Web/Features/Import/Pages/Home/` (stable URLs `/samples/import-minimal.csv` and `/samples/import-full.csv`; UK English labels)
- [X] T016 [US1] Wire `ImportWorkflowCoordinator` in `src/ImportToPlanner.Web/Features/Import/Workflows/ImportWorkflowCoordinator.cs` to call `PeekHeadersAsync` on upload, build proposal via `ICsvColumnMappingService`, auto-confirm when `Status = Ready` with identity/canonical headers, and pass `ConfirmedColumnMapping` into `ParseAsync` in `BuildPreviewAsync` (block preview when mapping not confirmed per FR-008)
- [X] T017 [US1] Update `tests/ImportToPlanner.Web.Tests/TestInfrastructure/CsvImportParserSubstitute.cs` and affected Home workflow tests for new `ICsvImportParser` members so sample-upload paths stay green
- [X] T018 [P] [US1] Add unit test in `tests/ImportToPlanner.Tests/CsvImportParserTests.cs` that reads `wwwroot/samples/import-minimal.csv` and `import-full.csv` content (embedded or copied fixture) and parses with identity mapping with zero file-level errors

**Checkpoint**: US1 acceptance scenarios pass in browser for downloaded samples.

---

## Phase 4: User Story 2 — Import files that use familiar column names (Priority: P1)

**Goal**: Auto-map common synonym headers without manual assignment when unambiguous.

**Independent Test**: Upload CSVs with alias-only headers for all present fields; preview shows correct values without manual mapping (spec US2).

### Implementation for User Story 2

- [X] T019 [US2] Ensure `CsvColumnMappingService.BuildProposal` applies alias rules after exact canonical match and treats extra unmapped source columns as ignorable when `ignoreExtraColumns` is true (FR-015) without blocking preview
- [X] T020 [US2] Extend `CsvImportParser` field extraction in `src/ImportToPlanner.Infrastructure.Graph/Import/CsvImportParser.cs` so each enabled `FieldId` from the catalog maps to the correct `CsvTaskRow` property (including Due Date and Assigned To when enabled in catalog)
- [X] T021 [P] [US2] Add `tests/ImportToPlanner.Tests/CsvColumnMappingServiceTests.cs` cases for alias-only header packs (e.g. `Title`, `Notes`, `Pri`) reaching `Ready` with correct `SuggestedAssignments`
- [X] T022 [P] [US2] Add `tests/ImportToPlanner.Tests/CsvImportParserTests.cs` twin-file tests: canonical-header CSV vs alias-header CSV produce equivalent `CsvTaskRow` sets via appropriate mappings

**Checkpoint**: SC-002 alias test pack reaches preview without manual mapping in manual quickstart step 3.

---

## Phase 5: User Story 3 — Fix incomplete or conflicting mappings (Priority: P2)

**Goal**: Mapping editor for missing Task Name or conflicting assignments; preview blocked until resolved and confirmed.

**Independent Test**: Upload files with no Task Name mapping or duplicate target field; complete mapping UI until preview succeeds (spec US3).

### Implementation for User Story 3

- [X] T023 [US3] Implement mapping editor UI on upload step in `src/ImportToPlanner.Web/Features/Import/Pages/Home/` (MudBlazor dialog or panel per mudblazor skill: one control per canonical field, source header dropdown, “Do not import” for optional fields; pre-fill `SuggestedAssignments`; UK English copy)
- [X] T024 [US3] Handle `Conflict` and `NeedsTaskName` in `ImportWorkflowCoordinator` and `Home.WorkflowActions.razor.cs` — force `ShowMappingEditor`, block preview/import until `ToConfirmedMapping` succeeds with Task Name assigned (FR-008, FR-009, FR-010)
- [X] T025 [US3] On mapping confirm, set `ConfirmedColumnMapping`, `IsColumnMappingConfirmed = true`, invalidate stale preview when mapping changes after preview (data-model.md state transitions)
- [X] T026 [P] [US3] Add coordinator-focused tests or extend `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs` to assert preview blocked when proposal is `NeedsTaskName` and allowed after simulated confirm (if pattern exists; otherwise document manual gate in quickstart only)

**Checkpoint**: Quickstart manual steps 4–5 pass (conflict resolution and missing Task Name block).

---

## Phase 6: User Story 4 — Reuse a confirmed mapping for the same layout (Priority: P2)

**Goal**: Browser-local layout memory keyed by normalised header layout, not file name or row data.

**Independent Test**: Confirm mapping once; second file with same headers different name auto-applies mapping (spec US4, FR-012–FR-014).

### Implementation for User Story 4

- [X] T027 [US4] Add `IImportColumnMappingLayoutStore` abstraction in Web and implement `ImportColumnMappingLayoutStore` in `src/ImportToPlanner.Web/Features/Import/Storage/ImportColumnMappingLayoutStore.cs` (`localStorage`, versioned key, `GetAsync`/`SaveAsync`; payload **MUST NOT** contain CSV row values or task data)
- [X] T028 [US4] Register layout store in `src/ImportToPlanner.Web/DependencyInjection.cs` and inject into upload/mapping flow
- [X] T029 [US4] On mapping confirm, persist `SavedLayoutMapping` (`LayoutSignature`, `Assignments` source → `FieldId` or skip, `UpdatedUtc`); on upload peek, load saved layout and seed `BuildProposal` (saved assignments win for existing headers; aliases fill gaps only for unassigned sources per data-model.md rule 5)
- [X] T030 [US4] Show recommended UK English privacy note on first save (mappings stay on this browser only per research.md)
- [X] T031 [P] [US4] Add unit tests for saved-layout merge in `tests/ImportToPlanner.Tests/CsvColumnMappingServiceTests.cs` (second upload same signature applies saved map; updated confirm overwrites)

**Checkpoint**: Quickstart manual step 6 and localStorage inspection step 7 pass.

---

## Phase 7: User Story 5 — See when mapping was automatic (Priority: P3)

**Goal**: Compact summary when auto-mapping succeeds; user can open editor before preview.

**Independent Test**: Fully auto-mapped file shows summary with path to change mappings (spec US5, FR-011).

### Implementation for User Story 5

- [X] T032 [US5] Add mapping summary component/section on upload step in `src/ImportToPlanner.Web/Features/Import/Pages/Home/` when `ColumnMappingProposal.Status = Ready` (concise source → import field list; “Change mapping” opens editor from T023)
- [X] T033 [US5] Allow continue to preview with summary visible without full editor interaction when mapping is complete and user confirms (one-click continue or explicit confirm per product rules in FR-010/FR-011)
- [X] T034 [P] [US5] Extend `tests/ImportToPlanner.Web.Tests/` smoke/workflow tests if feasible to assert mapping summary renders for auto-mapped CSV (optional skip if no bUnit pattern; manual quickstart step 3 still required)

**Checkpoint**: Auto-mapped alias upload shows summary before preview.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, call-site cleanup, architecture compliance, and release gates.

- [X] T035 [P] Update `docs/csv-format.md` with sample links aligned to app paths, synonym/mapping guidance, and remove sole reliance on exact headings (FR-003, SC-005)
- [X] T036 [P] Update `docs/getting-started.md` to reference downloadable samples where appropriate
- [X] T037 Update all remaining `ICsvImportParser.ParseAsync` call sites (coordinator, presenters, tests) to pass `CsvColumnMapping` after confirmation
- [X] T038 Run `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~CsvColumnMapping|FullyQualifiedName~CsvImportParserTests"` and `dotnet test ImportToPlanner.slnx --filter "FullyQualifiedName~ArchitectureComplianceTests"` per quickstart.md
- [X] T039 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` and fix any formatting issues
- [X] T040 Validate quickstart.md manual and static sample smoke checks (`/samples/import-minimal.csv`, `/samples/import-full.csv`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — sample files can be added early.
- **Phase 2 (Foundational)**: Depends on Phase 1 for full-sample column alignment; **blocks all user stories**.
- **Phase 3 (US1)**: Depends on Phase 2 — MVP delivery point.
- **Phase 4 (US2)**: Depends on Phase 2; can overlap US1 after T016 coordinator wiring.
- **Phase 5 (US3)**: Depends on Phase 2 and coordinator peek/proposal from T016.
- **Phase 6 (US4)**: Depends on T025 confirm path from US3.
- **Phase 7 (US5)**: Depends on US2 auto-map path and US3 editor (T023).
- **Phase 8 (Polish)**: After desired user stories complete.

### User Story Dependencies

| Story | Priority | Depends on | Independent test |
| ----- | -------- | ---------- | ---------------- |
| US1 | P1 | Foundational | Sample download → preview without manual mapping |
| US2 | P1 | Foundational | Alias-only headers → correct preview |
| US3 | P2 | Foundational + coordinator | Missing Task Name / conflict → editor → preview |
| US4 | P2 | US3 confirm + store | Second file same layout reapplies mapping |
| US5 | P3 | US2 + US3 UI | Auto-map shows summary + change path |

### Parallel Opportunities

- T002 and T003 (sample CSV files).
- T013 and T014 (test files) after T008/T011 respectively.
- T021 and T022 (US2 tests) in parallel.
- T035 and T036 (docs) in parallel.
- After Phase 2, US1 and US2 implementation tasks touch different concerns (samples/UI vs alias/parser) but share coordinator — sequence T015–T016 before heavy US2 parser work to avoid merge conflicts.

### Parallel Example: User Story 2

```bash
# After T019 completes, run in parallel:
# Task T021 — CsvColumnMappingServiceTests alias pack
# Task T022 — CsvImportParserTests canonical vs alias twins
```

### Parallel Example: Foundational tests

```bash
# After T008 and T011:
# Task T013 — CsvColumnMappingServiceTests.cs
# Task T014 — CsvImportParserTests mapping cases
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. **STOP and VALIDATE** using quickstart sections 1–3 and US1 manual check.
4. Demo downloadable samples → preview.

### Incremental Delivery

1. Foundation → US1 (samples) → US2 (aliases) → US3 (editor) → US4 (memory) → US5 (summary transparency) → Polish.

### Suggested MVP Scope

**User Story 1 only** (Phases 1–3) delivers downloadable samples and canonical-header happy path through the new mapping gate; alias and editor stories follow without rewriting the mapper.

---

## Notes

- Excel import (#55) MUST reuse `ICsvColumnMappingService` and `ImportColumnFieldCatalog` later — no second alias list (FR-016, plan.md).
- Delimiter/BOM (#133) remains in parser only; mapping MUST NOT re-sniff (FR-017).
- Issue #132: `Assignee` / `Email` aliases move to Assigned To via catalog (contracts supersede note).
- All tasks use checklist format with sequential IDs T001–T040.

---

## Phase 9: Convergence

- [X] T041 Auto-confirm column mapping on upload when a matching `SavedLayoutMapping` fully satisfies Task Name with no conflicts, so repeat uploads with the same header layout reach preview without repeating confirmation per US4/AC1, FR-012, and SC-004 (`ImportWorkflowCoordinator.ProcessCsvUploadAsync`) (partial)
- [X] T042 Align alias-only preview gate with FR-011 and SC-002: unambiguous `NeedsConfirmation` proposals (unique Task Name, no conflicts) should not block preview beyond the compact summary, or document and test one-click confirm as the intended SC-002 path (`CsvColumnMappingService` / coordinator / UI) (partial)
- [X] T043 Add troubleshooting entry for “Task Name could not be found” directing users to the mapping UI and downloadable samples per `contracts/csv-column-mapping-contracts.md` (`docs/troubleshooting.md`) (missing)
- [X] T044 Add `CsvColumnMappingServiceTests` case where two source headers compete for Priority (for example `Pri` and `Importance`) and assert `Conflict` per quickstart §1 and FR-009 (partial)
- [X] T045 Add coordinator or `HomePageWorkflowTests` coverage that preview stays blocked when proposal is `NeedsTaskName` and succeeds after simulated mapping confirm per T026, FR-008, and SC-003 (partial)
- [X] T046 Update `docs/troubleshooting.md` CSV validation section to mention synonym headers and the upload-step mapping editor instead of implying only exact headings per FR-003 and SC-005 (partial)

---

## Phase 10: Convergence

- [X] T047 Document that future Excel workbook import (#55) and Planner-export handling will reuse the Application column mapping layer (`ICsvColumnMappingService` / `ImportColumnFieldCatalog`) in public `docs/csv-format.md` (or linked doc), per FR-016 (missing)
- [X] T048 Add automated test asserting saved layout payloads contain only source header keys and field ids (no CSV row or task values), e.g. via `BuildSavedAssignments` or `ImportColumnMappingLayoutStoreStub` after coordinator confirm/upload, per FR-014 and Constitution VI (partial)
- [X] T049 Add `CsvColumnMappingServiceTests` (or catalog test) asserting a `Status` source header does not map to Bucket, per FR-007 and spec edge case (partial)
- [X] T050 Remove or wire up unreachable upload-step **Confirm suggested mapping** UI (`requiresMappingConfirmation` / `ConfirmSuggestedMappingAsync` in `Home.razor`) now that `ProcessCsvUploadAsync` auto-confirms `NeedsConfirmation` proposals (unrequested)
