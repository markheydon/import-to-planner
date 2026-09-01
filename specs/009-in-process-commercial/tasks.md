---

description: "Task list for feature implementation"
---

# Tasks: Isolate Commercial Accounts (Single Hosted Process)

**Input**: Design documents from `/specs/009-in-process-commercial/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Architecture compliance, registration, and retargeting of existing commercial/self-host tests are required by spec FR-016–017 and contracts §6. No new TDD-first unit-test phases unless a defect is found in the commercial path.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/` at repository root (`ImportToPlanner.Commercial`, `ImportToPlanner.Web`, `ImportToPlanner.AppHost`, `ImportToPlanner.Application`, `ImportToPlanner.Infrastructure.Graph`)
- **Tests**: `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Commercial class library shell and solution wiring so later moves have a valid target project.

- [X] T001 Create `src/ImportToPlanner.Commercial/ImportToPlanner.Commercial.csproj` with `ProjectReference` to Application and Domain, and package references `Aspire.Azure.Data.Tables` and `Azure.Data.Tables` per plan.md
- [X] T002 [P] Add `ImportToPlanner.Commercial` to `ImportToPlanner.slnx` and remove the `ImportToPlanner.ApiService.Commercial` project entry
- [X] T003 [P] Scaffold `src/ImportToPlanner.Commercial/DependencyInjection.cs` with `AddCommercialStorageClients` and `AddCommercial` extension method signatures per `specs/009-in-process-commercial/contracts/commercial-module-contracts.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Move commercial persistence, use cases, and DI registration into `ImportToPlanner.Commercial` so Web can compose them in-process. **No user story work should begin until this phase is complete.**

**⚠️ CRITICAL**: Application and Graph still own commercial types today; partial sources already exist under `src/ImportToPlanner.Commercial/Features/` but lack a `.csproj` and are not wired.

- [X] T004 Consolidate commercial models (`CommercialAccount`, `AccountAuditEvent`, `CommercialAccessDecision`, lifecycle result types) into `src/ImportToPlanner.Commercial/` per data-model.md — merge existing `Features/CommercialAccess/Models/` and `Features/CommercialProfile/Models/` with types still in `src/ImportToPlanner.Application/Models/`
- [X] T005 [P] Move commercial store abstractions and use-case interfaces from `src/ImportToPlanner.Application/Abstractions/` (`ICommercialAccountStore`, `ICommercialAuditStore`, `ICommercialAccessUseCase`, `ICommercialProfileUseCase`) into `src/ImportToPlanner.Commercial/`
- [X] T006 [P] Move commercial use cases from `src/ImportToPlanner.Application/Services/` (`CommercialAccessUseCase`, `GetCommercialProfileUseCase`, `DeleteCommercialAccountUseCase`, `RestoreCommercialAccountUseCase`, `PurgeExpiredCommercialAccountsUseCase`) into `src/ImportToPlanner.Commercial/` — align with or replace existing `Features/CommercialAccess/Services/` and `Features/CommercialProfile/Services/`
- [X] T007 Move `TableCommercialAccountStore` and `TableCommercialAuditStore` from `src/ImportToPlanner.Infrastructure.Graph/CommercialAccounts/Storage/` into `src/ImportToPlanner.Commercial/` table adapter folder (e.g. `Accounts/` or `Common/Storage/`)
- [X] T008 Move `TableTenantOperationalMetadataStore` from `src/ImportToPlanner.Infrastructure.Graph/TenantMetadata/` into `src/ImportToPlanner.Commercial/TenantMetadata/` and register it as `ITenantOperationalMetadataStore`
- [X] T009 Implement `AddCommercialStorageClients` in `src/ImportToPlanner.Commercial/DependencyInjection.cs`: call `builder.AddAzureTableServiceClient(connectionName: "tables")` and register keyed `TableClient` instances via `GetTableClient` using `Storage:CommercialAccountsTable`, `Storage:CommercialAuditTable`, and `Storage:TenantMetadataTable`
- [X] T010 Implement `AddCommercial` in `src/ImportToPlanner.Commercial/DependencyInjection.cs`: register table-backed stores, commercial use cases, and table-backed `ITenantOperationalMetadataStore` — no `new TableServiceClient(...)` on runtime paths
- [X] T011 Add `ProjectReference` to `ImportToPlanner.Commercial` in `src/ImportToPlanner.Web/ImportToPlanner.Web.csproj`
- [X] T012 Remove duplicate `SessionIdentityContext` from `src/ImportToPlanner.Commercial/Common/Models/` — keep the canonical `SessionIdentityContext` in `src/ImportToPlanner.Application/Models/SessionIdentityContext.cs` only per data-model.md

**Checkpoint**: Commercial library compiles with all account, audit, lifecycle, and tenant-metadata adapters; Web can reference it.

---

## Phase 3: User Story 1 - Hosted Commercial Runs as One Application (Priority: P1) 🎯 MVP (topology)

**Goal**: Hosted commercial mode deploys a single user-facing `web` application with `tables` attached only to `web`; the unused `commercialapiservice` process is gone from solution, AppHost, and runtime.

**Independent Test**: Enable commercial mode in a hosted-style deployment (or inspect static artefacts), confirm exactly one user-facing application, `tables` referenced from `web` only, and no `commercialapiservice` / `ApiService.Commercial` in solution or AppHost sources.

### Implementation for User Story 1

- [X] T013 [US1] Delete the entire `src/ImportToPlanner.ApiService.Commercial/` project directory
- [X] T014 [US1] Remove `commercialapiservice` provisioning, `minCommercialApiServiceReplicas`, and `web.WithReference(commercialApiService)` / `WaitFor` from `src/ImportToPlanner.AppHost/AppHost.cs` — keep `tables` `WithReference` / `WaitFor` on `web` only when commercial mode is enabled
- [X] T015 [US1] Remove `ImportToPlanner.ApiService.Commercial` project reference from `src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj`
- [X] T016 [P] [US1] Add static topology source-scan tests in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` asserting `AppHost.cs`, `ImportToPlanner.AppHost.csproj`, and `ImportToPlanner.slnx` contain no `commercialapiservice` or `ApiService.Commercial`

**Checkpoint**: Solution builds without the API host; AppHost models one `web` container with optional `tables` only.

---

## Phase 4: User Story 2 - Commercial Users Keep Existing Account Behaviour (Priority: P1)

**Goal**: All shipped commercial journeys (login gate, first sign-in create, returning user, identity chrome, profile, delete, restore, retention purge, audit) behave equivalently with UK English wording unchanged.

**Independent Test**: Repeat commercial user-account journeys from spec 008 against commercial mode enabled; outcomes and wording match current `main`.

### Implementation for User Story 2

- [X] T017 [US2] Update `src/ImportToPlanner.Web/Program.cs` to call `AddCommercialStorageClients` and `AddCommercial` only when `Features:CommercialMode:Enabled` is true; remove unconditional `AddInfrastructureStorageClients` table registration from the Graph path
- [X] T018 [US2] Register `CommercialAccountRetentionHostedService` in `src/ImportToPlanner.Web/Program.cs` only when commercial mode (and the existing retention-sweep flag) is enabled — remove unconditional `AddHostedService<CommercialAccountRetentionHostedService>()`
- [X] T019 [P] [US2] Update `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.CommercialAccess.razor.cs` to resolve Commercial use-case interfaces from `ImportToPlanner.Commercial` namespaces
- [X] T020 [P] [US2] Update `src/ImportToPlanner.Web/Features/CommercialAccounts/Pages/Profile.razor.cs` and `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.Initialization.razor.cs` to use Commercial contracts without changing UK English presentation text
- [X] T021 [US2] Relocate or reference `CommercialAccountRetentionHostedService` so Web registers the host-owned service while Commercial supplies purge use-case dependencies — update `src/ImportToPlanner.Web/Features/CommercialAccounts/CommercialAccountRetentionHostedService.cs` if namespace/DI changes are required
- [X] T022 [P] [US2] Retarget `tests/ImportToPlanner.Tests/CommercialAccessUseCaseTests.cs`, `CommercialAccountLifecycleUseCaseTests.cs`, `CommercialAccountTableStoreTests.cs`, and `CommercialRetentionSweepTests.cs` to `ImportToPlanner.Commercial` types and namespaces
- [X] T023 [P] [US2] Retarget `tests/ImportToPlanner.Web.Tests/HomePageCommercialAccessTests.cs`, `HomePageCommercialRetentionTests.cs`, and `ProfilePageTests.cs` stubs in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/` to Commercial contracts
- [X] T024 [US2] Confirm `src/ImportToPlanner.Web/Infrastructure/StartupConfigurationValidator.cs` validates commercial storage configuration keys only when commercial mode is enabled

**Checkpoint**: Commercial-mode manual or automated journeys pass; no regression in gate, profile, delete/restore, retention, or audit behaviour.

---

## Phase 5: User Story 3 - Self-Hosted Deployments Stay Free of Commercial Overhead (Priority: P2)

**Goal**: With commercial mode off, automatic Microsoft 365 sign-in works, the commercial login gate never appears, and no `tables` connection or commercial persistence is required at startup or runtime.

**Independent Test**: Run with `Features:CommercialMode:Enabled` = false; app starts without `tables`, no commercial gate, import workflow works.

### Implementation for User Story 3

- [X] T025 [US3] Remove commercial mode branching, `AddInfrastructureStorageClients`, no-op commercial stores, and table-backed tenant metadata from `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs` — Graph registers CSV, planner gateway, and `SelfHostTenantOperationalMetadataStore` only
- [X] T026 [US3] Remove commercial use-case registrations (`ICommercialAccessUseCase`, profile/delete/restore/purge) from `src/ImportToPlanner.Application/DependencyInjection.cs`
- [X] T027 [P] [US3] Remove `Azure.Data.Tables` and `Aspire.Azure.Data.Tables` package references from `src/ImportToPlanner.Infrastructure.Graph/ImportToPlanner.Infrastructure.Graph.csproj` once no Graph source files use them
- [X] T028 [US3] Update `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs` to assert self-host registration has no `TableServiceClient`, no commercial stores, and resolves `SelfHostTenantOperationalMetadataStore` for `ITenantOperationalMetadataStore`
- [X] T029 [P] [US3] Update `tests/ImportToPlanner.Web.Tests/StartupValidationTests.cs` and `HomePageSmokeTests.cs` to confirm self-host starts without commercial storage configuration

**Checkpoint**: Self-host composition has zero commercial DI surface; AppHost omits `tables` when commercial mode is false (already true on `main` — verify unchanged).

---

## Phase 6: User Story 4 - Commercial Account Rules Stay Outside Import Policy (Priority: P2)

**Goal**: Commercial account, audit, and profile contracts live only in `ImportToPlanner.Commercial`; Application/Domain contain import/planner policy plus shared tenant/session abstractions only; automated checks fail on boundary leaks.

**Independent Test**: Run architecture compliance and registration tests; confirm commercial types absent from Application/Domain, Commercial does not reference Graph/Kiota, and commercial mode on composes Commercial from `web` only.

### Implementation for User Story 4

- [X] T030 [US4] Delete commercial models and abstractions from `src/ImportToPlanner.Application/Models/` and `src/ImportToPlanner.Application/Abstractions/` (`CommercialAccount`, `AccountAuditEvent`, `CommercialAccessDecision`, `CommercialAccountRestoreResult`, `ICommercialAccountStore`, `ICommercialAuditStore`, `ICommercialAccessUseCase`, `ICommercialProfileUseCase`)
- [X] T031 [US4] Delete `src/ImportToPlanner.Infrastructure.Graph/CommercialAccounts/` (no-op and table storage adapters) after confirming adapters live in Commercial
- [X] T032 [US4] Invert `Application_ContainsCommercialAccountBoundaryContracts` and `CommercialAccountContracts_AreProviderNeutral` in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` — tests MUST fail when commercial account/audit/profile contracts appear in Application or Domain
- [X] T033 [P] [US4] Add architecture test in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` failing when `ImportToPlanner.Commercial` references `ImportToPlanner.Infrastructure.Graph`, Microsoft.Graph, Kiota, or MudBlazor
- [X] T034 [P] [US4] Add architecture test in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` failing when Graph `DependencyInjection.cs` registers commercial table stores after migration
- [X] T035 [P] [US4] Add architecture test in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs` failing on `new TableServiceClient` or `new BlobServiceClient` in Commercial or Graph runtime adapter source (test doubles outside host builder exempt per contracts §6)
- [X] T036 [US4] Add commercial-mode registration test in `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs` confirming `AddCommercialStorageClients` yields a DI `TableServiceClient` and Commercial stores/use cases — not Graph
- [X] T037 [US4] Verify `src/ImportToPlanner.Commercial/ImportToPlanner.Commercial.csproj` has no `ProjectReference` to `ImportToPlanner.Infrastructure.Graph` and no Graph/Kiota package references

**Checkpoint**: Architecture scans encode FR-016–017; inner layers are import-centric with shared `ITenantOperationalMetadataStore` and `SessionIdentityContext` only.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation drift, validation gates, and operator evidence.

- [X] T038 [P] Align `specs/008-commercial-user-accounts/plan.md` to describe Commercial-owned stores and no `commercialapiservice` per FR-019
- [X] T039 [P] Align `specs/008-commercial-user-accounts/contracts/commercial-account-contracts.md` so store ownership and topology match this feature — do not reopen 008 user stories
- [X] T040 Run `dotnet test ImportToPlanner.slnx --no-restore` from repository root and fix any failures
- [X] T041 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` from repository root
- [X] T042 Validate static checks and test expectations documented in `specs/009-in-process-commercial/quickstart.md` (solution topology, registration, architecture scans)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational — can overlap late Foundational tasks (T011) but requires T001–T003 complete
- **User Story 2 (Phase 4)**: Depends on Foundational (T004–T012) and benefits from US1 AppHost cleanup (T013–T015) for end-to-end hosted validation
- **User Story 3 (Phase 5)**: Depends on Foundational and US2 Web composition (T017–T018) so Graph/Application stripping does not break commercial mode
- **User Story 4 (Phase 6)**: Depends on Foundational moves (T004–T010); Application/Graph deletion tasks (T030–T031) MUST follow US2/US3 presenter and registration updates
- **Polish (Phase 7)**: Depends on all desired user story phases complete

### User Story Dependencies

- **User Story 1 (P1)**: Independent topology outcome after Foundational — no dependency on US2–US4 for static artefact checks
- **User Story 2 (P1)**: Depends on Foundational Commercial library; independent of US3; integrates with US1 for hosted deploy validation
- **User Story 3 (P2)**: Depends on US2 Web gating — strip Graph/Application commercial code only after Commercial composition exists
- **User Story 4 (P2)**: Depends on US2/US3 registration changes before deleting Application commercial types

### Within Each User Story

- AppHost/solution deletions (US1) before operator staging check
- Web `Program.cs` composition (US2) before removing Graph no-op stores (US3)
- Retarget behaviour tests (US2) before deleting Application types (US4)
- Architecture test inversions (US4) after source moves complete

### Parallel Opportunities

- **Phase 1**: T002 and T003 in parallel after T001
- **Phase 2**: T005, T006, T007 in parallel after T004; T033–T035 in parallel within US4
- **Phase 3**: T016 parallel with T013–T015 once paths are known
- **Phase 4**: T019, T020, T022, T023 in parallel
- **Phase 5**: T027 and T029 in parallel
- **Phase 6**: T033, T034, T035 in parallel
- **Phase 7**: T038 and T039 in parallel

---

## Parallel Example: User Story 2

```bash
# After T017–T018 (Program.cs gating), launch presenter and test retargeting together:
Task T019: "Update Home.CommercialAccess.razor.cs to resolve Commercial use-case interfaces"
Task T020: "Update Profile.razor.cs and Home.Initialization.razor.cs to use Commercial contracts"
Task T022: "Retarget CommercialAccessUseCaseTests and lifecycle/table/retention tests"
Task T023: "Retarget HomePageCommercialAccessTests, retention tests, and ProfilePageTests stubs"
```

---

## Parallel Example: User Story 4

```bash
# After T030–T031 (source deletion), launch architecture extensions together:
Task T033: "Commercial must not reference Graph/Kiota/MudBlazor"
Task T034: "Graph must not register commercial table stores"
Task T035: "No hand-built TableServiceClient/BlobServiceClient in runtime adapters"
```

---

## Implementation Strategy

### MVP First (User Story 1 topology + User Story 2 behaviour)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: User Story 1 — single-process hosted topology
4. Complete Phase 4: User Story 2 — preserve commercial journeys
5. **STOP and VALIDATE**: `dotnet test`, commercial quickstart journeys, static topology checks
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Commercial library ready
2. US1 → One `web` app, no stub API container
3. US2 → Commercial users see no regression
4. US3 → Self-host stays lightweight
5. US4 → Architecture enforcement locked in
6. Polish → 008 doc drift + format gate

### Parallel Team Strategy

With multiple developers after Foundational completes:

- Developer A: US1 AppHost/solution deletion + topology tests
- Developer B: US2 Web composition + bUnit retargeting
- Developer C: US3 Graph/Application stripping + registration tests
- Developer D: US4 architecture test inversions (after A–C merge)

---

## Notes

- Partial `src/ImportToPlanner.Commercial/Features/` sources exist without a `.csproj`; Phase 1–2 consolidate them rather than duplicating logic
- `SessionIdentityContext` stays in Application; Commercial use cases accept it as input per data-model.md
- Do not add `Aspire.Hosting.Testing` AppHost harnesses — use static source scans per research.md §5
- Test stack: xUnit v3, NSubstitute, built-in Assert — do not port #72 Moq patterns
- If AppHost is running during implement, run `aspire resource web rebuild` after Web/Commercial changes
- [P] tasks = different files, no incomplete-task dependencies
- [Story] label maps task to spec.md user story for traceability
- Commit after each task or logical group; stop at any checkpoint to validate story independently
