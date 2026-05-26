# Tasks: Simplify Graph Runtime Path

**Input**: Design documents from `/specs/005-simplify-graph-path/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/runtime-cleanup-contracts.md`  
**Tests**: Included and required because this feature removes supported runtime paths, changes startup validation, rewires Aspire hosting, and preserves authentication and workflow behaviour.  
**Agent delegation**: All coding, architecture, and test implementation tasks MUST be delegated to the C# Expert agent per `AGENTS.md`.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3]**: User story label from `spec.md`; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Establish the new AppHost and package baseline before behaviour changes begin.

- [X] T001 Add the conventional Aspire AppHost project file in `ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj`
- [X] T002 [P] Add the AppHost entrypoint in `ImportToPlanner.AppHost/Program.cs`
- [X] T003 [P] Add the AppHost project to the solution in `ImportToPlanner.slnx`
- [X] T004 [P] Add required Aspire AppHost package and SDK support in `Directory.Packages.props`
- [X] T005 Capture the simplified local verification flow in `specs/005-simplify-graph-path/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the shared configuration, validation, and test seams required by every user story.

**⚠️ CRITICAL**: Complete all T006-T014 before any user story work begins.

- [X] T006 [P] Create Web-owned authority configuration contracts in `src/ImportToPlanner.Web/TenantAuthorityConfiguration.cs`
- [X] T007 [P] Create shared storage configuration contracts in `src/ImportToPlanner.Web/StorageConfiguration.cs`
- [X] T008 [P] Add startup validation for missing and obsolete configuration keys in `src/ImportToPlanner.Web/Program.cs`
- [X] T009 [P] Remove deployment-mode data from the tenant contract in `src/ImportToPlanner.Application/Models/TenantContext.cs`
- [X] T010 [P] Add shared planner and metadata test doubles in `tests/ImportToPlanner.Tests/TestDoubles/PlannerGatewayStub.cs`
- [X] T011 [P] Add shared planner and metadata test doubles for Web tests in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/PlannerGatewayStub.cs`
- [X] T012 Remove the obsolete deployment-mode model types in `src/ImportToPlanner.Application/Models/DeploymentModeConfiguration.cs`
- [X] T013 Add architecture regression coverage for removed runtime-mode concepts in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`
- [X] T014 Extend Web test hosting for authority-based configuration in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/HomePageTestContext.cs`

**Checkpoint**: The repo has one AppHost entrypoint, explicit Web-owned authority/storage configuration, startup validation, and test-boundary doubles so story work can proceed without reopening the architecture baseline.

---

## Phase 3: User Story 1 - Run One Supported Production Path (Priority: P1) 🎯 MVP

**Goal**: Remove runtime planner and metadata switching so the import workflow always uses the supported Graph and table-backed production path.

**Independent Test**: Run the workflow with both `AzureAd:TenantId=organizations` and a tenant-specific authority and confirm planner actions and tenant metadata handling use the same production registrations without any runtime mode selection.

### Tests for User Story 1

- [X] T015 [P] [US1] Add DI regression coverage for single-path planner and metadata registrations in `tests/ImportToPlanner.Tests/InfrastructureRegistrationTests.cs`
- [X] T016 [P] [US1] Add import planning regression coverage for single-path consent and metadata behaviour in `tests/ImportToPlanner.Tests/ImportPlanningUseCaseTests.cs`
- [X] T017 [P] [US1] Add workflow regression coverage for single supported planner behaviour in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`

### Implementation for User Story 1

- [X] T018 [US1] Register the Graph planner gateway unconditionally in `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs`
- [X] T019 [US1] Register the table-backed tenant metadata store unconditionally in `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs`
- [X] T020 [P] [US1] Remove runtime planner branching from the import planning use case in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T021 [P] [US1] Remove planner-mode UI branching from the import page in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T022 [P] [US1] Replace the root script AppHost resource graph with the conventional AppHost in `ImportToPlanner.AppHost/Program.cs`
- [X] T023 [US1] Delete the obsolete in-memory planner gateway in `src/ImportToPlanner.Infrastructure.Graph/InMemoryPlannerGateway.cs`
- [X] T024 [US1] Delete the obsolete in-memory tenant metadata store in `src/ImportToPlanner.Infrastructure.Graph/TenantMetadata/InMemoryTenantOperationalMetadataStore.cs`
- [X] T025 [US1] Delete the obsolete root Aspire script in `apphost.cs`
- [X] T026 [US1] Delete the obsolete Aspire script configuration file in `aspire.config.json`

**Checkpoint**: The runtime always resolves the Graph planner gateway and table-backed metadata store, and the AppHost resource graph is the single supported orchestration path.

---

## Phase 4: User Story 2 - Keep the Working Authentication Guards While Removing Mode Plumbing (Priority: P2)

**Goal**: Preserve the existing unsupported-account, admin-consent, and tenant-context mismatch protections while removing deployment-mode plumbing from Application and Web consumers.

**Independent Test**: Verify that `organizations` retains the shared-organisations auth guards, a tenant-specific authority remains single-tenant, and all existing guard outcomes stay unchanged without `DeploymentModeConfiguration`.

### Tests for User Story 2

- [X] T027 [P] [US2] Add authority-classification and startup-auth regression tests in `tests/ImportToPlanner.Web.Tests/HostedAuthenticationEventTests.cs`
- [X] T028 [P] [US2] Add tenant-context regression coverage for authority-based behaviour in `tests/ImportToPlanner.Web.Tests/ClaimsTenantContextAccessorTests.cs`
- [X] T029 [P] [US2] Add token-provider regression coverage after deployment-mode removal in `tests/ImportToPlanner.Web.Tests/MicrosoftIdentityAccessTokenProviderTests.cs`

### Implementation for User Story 2

- [X] T030 [P] [US2] Remove deployment-mode synthesis and AzureAd backfilling from startup in `src/ImportToPlanner.Web/Program.cs`
- [X] T031 [P] [US2] Drive OpenID Connect event guards from authority classification in `src/ImportToPlanner.Web/DependencyInjection.cs`
- [X] T032 [P] [US2] Resolve tenant context without deployment-mode dependencies in `src/ImportToPlanner.Web/ClaimsTenantContextAccessor.cs`
- [X] T033 [P] [US2] Remove self-hosted mode branching from delegated token acquisition in `src/ImportToPlanner.Web/MicrosoftIdentityAccessTokenProvider.cs`
- [X] T034 [P] [US2] Remove deployment-mode telemetry dimensions from hosted diagnostics in `src/ImportToPlanner.Web/HostedTelemetryHelper.cs`
- [X] T035 [P] [US2] Remove deployment-mode dependencies from failure diagnostics in `src/ImportToPlanner.Web/UserFacingFailureDiagnostics.cs`
- [X] T036 [US2] Remove deployment-mode dependencies from workflow coordination in `src/ImportToPlanner.Web/Workflows/ImportWorkflowCoordinator.cs`

**Checkpoint**: The working auth protections are preserved, but no Web or Application behaviour depends on `DeploymentMode` or `DeploymentModeConfiguration`.

---

## Phase 5: User Story 3 - Remove Obsolete Configuration and Test Scaffolding (Priority: P3)

**Goal**: Remove obsolete configuration keys, rename storage settings, always persist Data Protection keys to blob storage, and replace deleted runtime-path tests with boundary doubles or explicit startup-validation coverage.

**Independent Test**: Start the app with valid `Storage:*` and `AzureAd:*` settings, then verify that removed keys fail gracefully if reintroduced and that tests no longer rely on deleted in-memory production implementations.

### Tests for User Story 3

- [X] T037 [P] [US3] Add startup-validation coverage for missing `AzureAd:TenantId` and removed keys in `tests/ImportToPlanner.Web.Tests/StartupValidationTests.cs`
- [X] T038 [P] [US3] Update Data Protection persistence coverage for always-on blob storage in `tests/ImportToPlanner.Web.Tests/HostedDataProtectionConfiguratorTests.cs`
- [X] T039 [P] [US3] Replace deleted in-memory production-path tests with boundary-double coverage in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`

### Implementation for User Story 3

- [X] T040 [P] [US3] Always configure blob-backed Data Protection persistence in `src/ImportToPlanner.Web/HostedDataProtectionConfigurator.cs`
- [X] T041 [P] [US3] Rename hosted storage settings to `Storage:*` in `src/ImportToPlanner.Web/appsettings.json`
- [X] T042 [P] [US3] Remove mode flags and hosted-only toggles from development settings in `src/ImportToPlanner.Web/appsettings.Development.json`
- [X] T043 [P] [US3] Consume renamed `Storage:*` settings in infrastructure registration in `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs`
- [X] T044 [P] [US3] Update Web project startup configuration guidance in `README.md`
- [X] T045 [P] [US3] Update contributor run and configuration guidance in `CONTRIBUTING.md`
- [X] T046 [P] [US3] Replace in-memory production test guidance in `tests/README.md`
- [X] T047 [US3] Update Microsoft Graph adapter guidance for boundary doubles in `docs-internal/microsoft-graph-guidelines.md`

**Checkpoint**: The app uses `Storage:*` consistently, Data Protection always persists to blob storage, removed keys fail gracefully, and test/documentation scaffolding no longer describes deleted runtime paths.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalise evidence, delete leftover references, and verify the simplified developer experience end to end.

- [X] T048 [P] Remove leftover plan-reference guidance that assumes the old AppHost flow in `docs-internal/developer-quickstart.md`
- [X] T049 [P] Record final verification evidence for the simplified local flow in `specs/005-simplify-graph-path/quickstart.md`
- [X] T050 [P] Update repository-level testing policy language to remove runtime-mode parity expectations in `docs-internal/engineering-policies.md`
- [X] T051 [P] Update contributor-facing plan references in `.github/copilot-instructions.md`
- [ ] T052 Run the full feature verification checklist in `specs/005-simplify-graph-path/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the MVP by removing the runtime path matrix
- **Phase 4 (US2)**: Depends on US1 because the auth-guard cleanup assumes the single production path and new Web-owned authority seam already exist
- **Phase 5 (US3)**: Depends on US1 and US2 because storage renaming, startup validation, and test/documentation cleanup rely on the final runtime and auth shape being stable
- **Phase 6 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is the MVP
- **US2 (P2)**: Depends on US1 runtime simplification, then remains independently testable through focused auth and tenant-context checks
- **US3 (P3)**: Depends on US1 and US2, then remains independently testable through startup-validation, storage, and documentation checks

### Within Each User Story

- Tests before implementation wherever practical
- Configuration and contract cleanup before UI or workflow integration that consumes it
- DI and adapter rewrites before deletion of obsolete runtime-path files
- Story-specific verification before moving to the next priority

### Parallel Opportunities

- T002-T005 can run in parallel during Setup
- T006-T011 and T013-T014 can run in parallel once Setup is complete
- For US1, T015-T017 can run in parallel, then T020-T022 can run in parallel while T018-T019 are completed sequentially in the shared infrastructure registration file before the deletion tasks
- For US2, T027-T029 can run in parallel, then T030-T035 can run in parallel before T036
- For US3, T037-T039 can run in parallel, then T040-T047 can run in parallel where files do not overlap
- T048-T051 can run in parallel after user-story work is complete

---

## Parallel Execution Examples

### User Story 1

```text
T015 (DI registration tests)  ║  T016 (planning use-case tests)  ║  T017 (workflow tests)
then:
T018 (planner DI)  →  T019 (metadata DI)
in parallel with:
T020 (planning cleanup)  ║  T021 (Home page cleanup)  ║  T022 (AppHost graph)
then:
T023 (delete in-memory planner)  ║  T024 (delete in-memory metadata store)  ║  T025 (delete root AppHost script)  ║  T026 (delete aspire.config.json)
```

### User Story 2

```text
T027 (auth event tests)  ║  T028 (tenant context tests)  ║  T029 (token provider tests)
then:
T030 (startup cleanup)  ║  T031 (OpenID Connect guard cleanup)  ║  T032 (tenant context cleanup)  ║  T033 (token provider cleanup)  ║  T034 (telemetry cleanup)  ║  T035 (failure diagnostics cleanup)
then:
T036 (workflow coordination cleanup)
```

### User Story 3

```text
T037 (startup validation tests)  ║  T038 (Data Protection tests)  ║  T039 (execution boundary-double tests)
then:
T040 (Data Protection implementation)  ║  T041 (appsettings rename)  ║  T042 (dev settings cleanup)  ║  T043 (infrastructure storage config)  ║  T044 (README)  ║  T045 (CONTRIBUTING)  ║  T046 (tests README)  ║  T047 (Graph guidance)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate that the app now has one supported AppHost and one supported runtime path before expanding scope

### Incremental Delivery

1. Setup + Foundational establish the AppHost, startup validation, and boundary-double seams
2. US1 removes the runtime path matrix and delivers the MVP
3. US2 preserves the correct auth protections while removing mode plumbing
4. US3 cleans up storage/configuration/test/documentation scaffolding
5. Polish records final evidence and removes leftover guidance

### Parallel Team Strategy

1. One engineer completes Setup + Foundational to stabilise shared seams
2. After Foundational, US1 DI, use-case, UI, and AppHost work can be split across engineers
3. Once US1 is stable, US2 auth-event, tenant-context, and token-provider work can proceed in parallel
4. US3 storage, startup-validation, and documentation cleanup can then be divided across implementation and docs owners

---

## Summary

- **Total tasks**: 52
- **Setup + Foundational**: 14 (T001-T014)
- **US1**: 12 (T015-T026)
- **US2**: 10 (T027-T036)
- **US3**: 11 (T037-T047)
- **Polish**: 5 (T048-T052)
- **Parallel [P] tasks**: 37 of 52

Independent test criteria by story:

- **US1**: Planner actions and tenant metadata handling always use the supported production path, regardless of whether the configured authority is `organizations` or a specific tenant
- **US2**: Unsupported-account rejection, admin-consent guidance, and tenant-context mismatch handling remain unchanged when authority behaviour is derived directly from `AzureAd:TenantId`
- **US3**: Missing or obsolete configuration fails gracefully, `Storage:*` replaces `HostedStorage:*`, and tests/documentation no longer rely on deleted in-memory production paths

Suggested MVP scope: Complete Phase 1, Phase 2, and Phase 3 (US1) before starting the auth-plumbing cleanup or configuration/test scaffolding cleanup.

Format validation: Every task uses the required checklist form with checkbox, task ID, optional `[P]`, required story label in user-story phases, and an exact file path.
