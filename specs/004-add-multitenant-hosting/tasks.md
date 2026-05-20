# Tasks: Hosted Multi-Tenant Support

**Input**: Design documents from `/specs/004-add-multitenant-hosting/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/hosted-tenant-contracts.md`  
**Tests**: Included and required because this feature changes authentication, consent handling, metadata persistence, deployment wiring, and planner-facing workflow behaviour.  
**Agent delegation**: All coding, architecture, and test implementation tasks MUST be delegated to the C# Expert agent per `AGENTS.md`.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1/US2/US3]**: User story label from `spec.md`; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Establish the hosted feature baseline, package ownership, and verification scaffolding before code changes begin.

- [X] T001 Record hosted/self-hosted verification checkpoints and evidence commands in `specs/004-add-multitenant-hosting/quickstart.md`
- [X] T002 [P] Add Azure Storage, data-protection, and hosted support package versions in `Directory.Packages.props`
- [X] T003 [P] Add hosted metadata storage package references in `src/ImportToPlanner.Infrastructure.Graph/ImportToPlanner.Infrastructure.Graph.csproj`
- [X] T004 [P] Add hosted authentication and data-protection package references in `src/ImportToPlanner.Web/ImportToPlanner.Web.csproj`
- [X] T005 Capture hosted rollout guardrails and evidence expectations in `docs-internal/aspire-production-readiness.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the shared contracts, configuration seams, and compliance checks that every user story depends on.

**⚠️ CRITICAL**: Complete all T006-T014 before any user story work starts.

- [X] T006 [P] Create deployment-mode configuration contracts in `src/ImportToPlanner.Application/Models/DeploymentModeConfiguration.cs`
- [X] T007 [P] Create supported account type and tenant context contracts in `src/ImportToPlanner.Application/Models/TenantContext.cs`
- [X] T008 [P] Create tenant-scoped operational metadata contracts in `src/ImportToPlanner.Application/Models/TenantOperationalMetadata.cs`
- [X] T009 [P] Create consent resolution contracts in `src/ImportToPlanner.Application/Models/ConsentResolution.cs`
- [X] T010 [P] Add the current-tenant context accessor abstraction in `src/ImportToPlanner.Application/Abstractions/ICurrentTenantContextAccessor.cs`
- [X] T011 [P] Add the tenant operational metadata store abstraction in `src/ImportToPlanner.Application/Abstractions/ITenantOperationalMetadataStore.cs`
- [X] T012 Add tenant and identity boundary regression checks in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`
- [X] T013 [P] Bind deployment-mode configuration and shared auth options in `src/ImportToPlanner.Web/Program.cs`
- [X] T014 [P] Extend web-test hosting support for deployment-mode scenarios in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/HomePageTestContext.cs`

**Checkpoint**: Shared tenant, consent, metadata, and deployment-mode seams are available so hosted and self-hosted story work can proceed without reopening the architecture baseline.

---

## Phase 3: User Story 1 - Use Hosted Deployment Across Organisations (Priority: P1) 🎯 MVP

**Goal**: Allow supported work or school users from multiple Microsoft 365 tenants to sign in to one hosted deployment, remain isolated by tenant context, and continue using delegated Graph authority through the existing import workflow.

**Independent Test**: Sign into one hosted deployment with users from at least two supported customer tenants, verify both can reach validation, preview, confirmation, and reporting, and confirm tenant metadata and workflow context never cross tenant boundaries.

### Tests for User Story 1

- [X] T015 [P] [US1] Add hosted sign-in and tenant-isolation workflow coverage in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T016 [P] [US1] Add tenant metadata partitioning regression coverage in `tests/ImportToPlanner.Tests/TenantOperationalMetadataStoreTests.cs`
- [X] T017 [US1] Add unsupported-account and tenant-mismatch presentation coverage in `tests/ImportToPlanner.Web.Tests/ImportPresenterTests.cs`

### Implementation for User Story 1

- [X] T018 [P] [US1] Implement claim-based tenant context extraction and unsupported-account rejection in `src/ImportToPlanner.Web/DependencyInjection.cs`
- [X] T019 [P] [US1] Implement the Azure Table tenant metadata store in `src/ImportToPlanner.Infrastructure.Graph/TenantMetadata/TableTenantOperationalMetadataStore.cs`
- [X] T020 [P] [US1] Register hosted tenant metadata adapters and mappings in `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs`
- [X] T021 [US1] Enforce active tenant-context safety throughout the workflow coordinator in `src/ImportToPlanner.Web/Workflows/ImportWorkflowCoordinator.cs`
- [X] T022 [US1] Surface hosted sign-in, unsupported-account, and tenant-mismatch states in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T023 [P] [US1] Preserve signed-in-user delegated token acquisition for hosted mode in `src/ImportToPlanner.Web/MicrosoftIdentityAccessTokenProvider.cs`
- [X] T024 [US1] Keep planner gateway operations bound to the current delegated tenant session in `src/ImportToPlanner.Infrastructure.Graph/GraphPlannerGateway.cs`

**Checkpoint**: One hosted deployment safely supports multiple work-or-school tenants, rejects unsupported accounts before workflow entry, and keeps metadata and Graph access scoped to the active signed-in tenant.

---

## Phase 4: User Story 2 - Resolve Consent and Privacy Expectations Clearly (Priority: P2)

**Goal**: Give hosted users and administrators a clear consent path, explicit failure guidance, and trustworthy documentation about accessed data and retained tenant-scoped metadata.

**Independent Test**: Attempt first-time hosted use from a tenant without the required permissions, verify the app either completes delegated consent or shows a clear administrator path, and confirm the documentation explains accessed data and retained metadata accurately.

### Tests for User Story 2

- [X] T025 [P] [US2] Add consent-guidance presenter coverage for user-consent and admin-consent-required paths in `tests/ImportToPlanner.Web.Tests/ImportPresenterTests.cs`
- [X] T026 [P] [US2] Add consent resolution and retention-scope regression coverage in `tests/ImportToPlanner.Tests/ConsentResolutionTests.cs`

### Implementation for User Story 2

- [X] T027 [P] [US2] Implement consent resolution and workflow gating in `src/ImportToPlanner.Application/Services/ImportPlanningUseCase.cs`
- [X] T028 [P] [US2] Map token-acquisition failures and administrator consent paths into repository contracts in `src/ImportToPlanner.Web/DependencyInjection.cs`
- [X] T029 [P] [US2] Persist tenant-scoped consent state and support diagnostics in `src/ImportToPlanner.Infrastructure.Graph/TenantMetadata/TableTenantOperationalMetadataStore.cs`
- [X] T030 [US2] Render UK-English consent guidance and administrator next steps in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T031 [US2] Document hosted consent, accessed data, and retained metadata guidance in `README.md`
- [X] T032 [US2] Add operator-facing hosted privacy and consent guidance in `docs/README.md`

**Checkpoint**: Hosted tenants receive a clear delegated-consent or administrator-consent experience, and both users and operators can understand the hosted data-handling boundary without ambiguity.

---

## Phase 5: User Story 3 - Preserve Self-Hosted Operation and Hosted Readiness (Priority: P3)

**Goal**: Keep single-tenant self-hosted behaviour intact while adding the hosted deployment, observability, and AppHost wiring needed to operate the service safely.

**Independent Test**: Configure one self-hosted single-tenant deployment and one hosted shared deployment, verify both preserve validation, preview, confirmation, and reporting semantics, and confirm hosted diagnostics identify tenant-safe failure context without leaking secrets.

### Tests for User Story 3

- [X] T033 [P] [US3] Add deployment-mode workflow parity coverage for hosted and self-hosted runs in `tests/ImportToPlanner.Web.Tests/HomePageWorkflowTests.cs`
- [X] T034 [P] [US3] Add planner runtime-mode parity coverage for deployment-aware execution flows in `tests/ImportToPlanner.Tests/ImportExecutionUseCaseTests.cs`
- [X] T035 [P] [US3] Add hosted telemetry and privacy-safe diagnostics regression coverage in `tests/ImportToPlanner.Tests/HostedTelemetryTests.cs`

### Implementation for User Story 3

- [X] T036 [P] [US3] Add deployment-mode-aware authentication and hosted data-protection configuration in `src/ImportToPlanner.Web/Program.cs`
- [X] T037 [P] [US3] Extend the AppHost with hosted storage and local emulation resources in `apphost.cs`
- [X] T038 [P] [US3] Enrich hosted telemetry with deployment mode, tenant key, consent status, and failure category in `src/ImportToPlanner.ServiceDefaults/Extensions.cs`
- [X] T039 [US3] Preserve self-hosted single-tenant configuration defaults in `src/ImportToPlanner.Web/appsettings.json`
- [X] T040 [US3] Add hosted/self-hosted operational configuration examples in `src/ImportToPlanner.Web/appsettings.Development.json`
- [X] T041 [US3] Document hosted and self-hosted deployment setup differences in `docs/README.md`
- [X] T042 [US3] Capture deployment-mode verification steps and expected evidence in `specs/004-add-multitenant-hosting/quickstart.md`

**Checkpoint**: Self-hosted operators keep the existing single-tenant behaviour, hosted deployments gain their required AppHost and telemetry support, and both modes preserve the existing workflow semantics.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalise cross-cutting documentation, architecture evidence, and rollout notes after the user stories are complete.

- [X] T043 [P] Update Microsoft Graph hosted guidance and compatibility notes in `docs-internal/microsoft-graph-guidelines.md`
- [X] T044 [P] Record final hosted rollout constraints and support notes in `docs-internal/aspire-production-readiness.md`
- [X] T045 [P] Record focused test, runtime-mode, and architecture evidence outcomes in `specs/004-add-multitenant-hosting/quickstart.md`
- [X] T046 [P] Update contributor-facing deployment summary links in `README.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the hosted multi-tenant MVP
- **Phase 4 (US2)**: Depends on US1 because consent guidance and retained-metadata behaviour build on the hosted tenant-context and metadata seams introduced there
- **Phase 5 (US3)**: Depends on US1 and US2 because deployment parity, AppHost wiring, and hosted diagnostics rely on the hosted auth, consent, and metadata paths already being stable
- **Phase 6 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is the MVP
- **US2 (P2)**: Depends on US1 hosted tenant-context and metadata support, then remains independently testable through focused consent and documentation checks
- **US3 (P3)**: Depends on US1 and US2, then remains independently testable through deployment-mode parity and hosted observability checks

### Within Each User Story

- Tests before implementation wherever practical
- Contracts and configuration seams before workflow or adapter rewrites
- Adapter and presenter changes before Razor/UI integration that consumes them
- Story-specific verification before moving to the next priority

### Parallel Opportunities

- T002, T003, and T004 can run in parallel during Setup
- T006-T011, T013, and T014 can run in parallel once Setup is complete
- For US1, T015 and T016 can run in parallel, then T018-T020 and T023 can run in parallel before the final workflow/UI integration tasks
- For US2, T025 and T026 can run in parallel, then T027-T029 can run in parallel before UI and documentation updates
- For US3, T033-T035 can run in parallel, then T036-T038 can run in parallel before configuration and documentation tasks
- T043-T046 can run in parallel after implementation is complete

---

## Parallel Execution Examples

### User Story 1

```text
T015 (hosted workflow tests)  ║  T016 (tenant metadata store tests)
then:
T018 (tenant context extraction)  ║  T019 (Table metadata store)  ║  T020 (Infrastructure registration)  ║  T023 (delegated token provider)
then:
T021 (workflow tenant safety)  →  T022 (Home page hosted states)  →  T024 (Graph gateway tenant binding)
```

### User Story 2

```text
T025 (consent presenter tests)  ║  T026 (consent resolution tests)
then:
T027 (planning use-case consent gating)  ║  T028 (web consent mapping)  ║  T029 (metadata retention updates)
then:
T030 (Home page guidance)  ║  T031 (README guidance)  ║  T032 (docs guidance)
```

### User Story 3

```text
T033 (deployment-mode web tests)  ║  T034 (runtime-mode parity tests)  ║  T035 (telemetry tests)
then:
T036 (web auth/data protection)  ║  T037 (AppHost resources)  ║  T038 (telemetry enrichment)
then:
T039 (self-hosted defaults)  ║  T040 (development examples)  ║  T041 (deployment docs)  ║  T042 (verification steps)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate hosted multi-tenant sign-in, tenant isolation, and delegated Graph behaviour before expanding scope

### Incremental Delivery

1. Setup + Foundational establish the deployment-mode, tenant, consent, and metadata seams
2. US1 delivers the hosted multi-tenant MVP
3. US2 adds clear consent handling and privacy guidance
4. US3 preserves self-hosted parity and completes hosted operational readiness
5. Polish records final evidence and rollout notes

### Parallel Team Strategy

1. One engineer completes Setup + Foundational to stabilise shared seams
2. After Foundational, hosted auth, metadata storage, and workflow work for US1 can be split across separate engineers
3. Once US1 is stable, consent handling, UI wording, and documentation in US2 can run in parallel
4. AppHost, telemetry, and deployment-parity work in US3 can run alongside final documentation and evidence capture

---

## Summary

- **Total tasks**: 46
- **Setup + Foundational**: 14 (T001-T014)
- **US1**: 10 (T015-T024)
- **US2**: 8 (T025-T032)
- **US3**: 10 (T033-T042)
- **Polish**: 4 (T043-T046)
- **Parallel [P] tasks**: 32 of 46

Independent test criteria by story:

- **US1**: One hosted deployment safely supports multiple supported customer tenants, rejects unsupported accounts, and keeps tenant metadata and delegated Graph access isolated to the active tenant context
- **US2**: Hosted consent either completes through delegated consent or produces a clear administrator path, and the documentation explains accessed data and retained tenant-scoped metadata accurately
- **US3**: Hosted and self-hosted deployments both preserve validation, preview, confirmation, and reporting semantics while hosted diagnostics remain tenant-safe and actionable

Suggested MVP scope: Complete Phase 1, Phase 2, and Phase 3 (US1) before starting consent/privacy refinements or hosted operational readiness work.

Format validation target: Every task uses the required checklist form with checkbox, task ID, optional `[P]`, required story label in user-story phases, and an exact file path.
