# Tasks: Commercial User Accounts

**Input**: Design documents from `/specs/008-commercial-user-accounts/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/commercial-account-contracts.md`  
**Tests**: Included and required because the feature specification defines mandatory user-scenario testing, the plan requires xUnit and bUnit coverage, and the quickstart includes explicit verification for commercial and self-hosted flows.  
**Agent delegation**: All coding, architecture, and test implementation tasks should follow the repository delegation rules in `AGENTS.md`; C# implementation work belongs with the C# Expert agent.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1-US4]**: User story label from `spec.md`; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Wire the deployment and configuration entry points that expose commercial mode and the new storage tables.

- [X] T001 Add the non-secret commercial-mode Aspire parameter and forward `Features__CommercialMode__Enabled` to the web project in `src/ImportToPlanner.AppHost/AppHost.cs`
- [X] T002 [P] Pass `Parameters__enableCommercialMode` through the staging deployment environment in `.github/workflows/deploy-staging.yml`
- [X] T003 [P] Add commercial account and audit table configuration defaults in `src/ImportToPlanner.Web/appsettings.json` and `src/ImportToPlanner.Web/appsettings.Development.json`
- [X] T004 [P] Update the feature verification steps for commercial-mode configuration and staging rollout in `specs/008-commercial-user-accounts/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared contracts, configuration seams, and test infrastructure that every user story depends on.

**⚠️ CRITICAL**: Complete T005-T012 before any user story work begins.

- [X] T005 [P] Create the commercial-mode and session-identity configuration contracts in `src/ImportToPlanner.Web/CommercialModeOptions.cs` and `src/ImportToPlanner.Application/Models/SessionIdentityContext.cs`
- [X] T006 [P] Create the repository-owned account, access-decision, and audit models in `src/ImportToPlanner.Application/Models/CommercialAccount.cs`, `src/ImportToPlanner.Application/Models/CommercialAccessDecision.cs`, and `src/ImportToPlanner.Application/Models/AccountAuditEvent.cs`
- [X] T007 [P] Create the application storage and lifecycle abstractions in `src/ImportToPlanner.Application/Abstractions/ICommercialAccountStore.cs`, `src/ImportToPlanner.Application/Abstractions/ICommercialAuditStore.cs`, `src/ImportToPlanner.Application/Abstractions/ICommercialAccessUseCase.cs`, and `src/ImportToPlanner.Application/Abstractions/ICommercialProfileUseCase.cs`
- [X] T008 Register the foundational commercial account services and options in `src/ImportToPlanner.Application/DependencyInjection.cs` and `src/ImportToPlanner.Web/Program.cs`
- [X] T009 [P] Add startup validation and infrastructure table-client wiring for the commercial tables in `src/ImportToPlanner.Web/StartupConfigurationValidator.cs` and `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs`
- [X] T010 [P] Add reusable commercial account and audit test doubles in `tests/ImportToPlanner.Tests/TestDoubles/CommercialAccountStoreStub.cs`, `tests/ImportToPlanner.Tests/TestDoubles/CommercialAuditStoreStub.cs`, and `tests/ImportToPlanner.Web.Tests/TestInfrastructure/CommercialAccountStoreStub.cs`
- [X] T011 [P] Extend architecture regression coverage for provider-neutral commercial account boundaries in `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`
- [X] T012 [P] Extend the web test host for commercial-mode configuration and account-store overrides in `tests/ImportToPlanner.Web.Tests/TestInfrastructure/HomePageTestContext.cs`

**Checkpoint**: Commercial mode can be configured explicitly, Application owns only neutral contracts and models, and both test projects can host the new account lifecycle seams without reworking the foundation.

---

## Phase 3: User Story 1 - First Commercial Sign-In Creates an Account (Priority: P1) 🎯 MVP

**Goal**: Show a clear commercial login gate, create the minimal account record on first successful sign-in, and allow returning commercial users straight through.

**Independent Test**: Open the commercial deployment while signed out, confirm the sign-in message explains account creation, sign in with a Microsoft 365 work account, and verify that a first-time user gets an account and a returning user reaches the main app without seeing the first-login explanation again.

### Tests for User Story 1

- [X] T013 [P] [US1] Add first-sign-in and returning-user access-resolution coverage in `tests/ImportToPlanner.Tests/CommercialAccessUseCaseTests.cs`
- [X] T014 [P] [US1] Add Azure Table persistence coverage for account creation and sign-in audit writes in `tests/ImportToPlanner.Tests/CommercialAccountTableStoreTests.cs`
- [X] T015 [P] [US1] Add login-gate component coverage for unsigned, first-time, and returning commercial users in `tests/ImportToPlanner.Web.Tests/HomePageCommercialAccessTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implement first-sign-in access resolution and account creation in `src/ImportToPlanner.Application/Services/CommercialAccessUseCase.cs`
- [X] T017 [US1] Register the commercial access use case in `src/ImportToPlanner.Application/DependencyInjection.cs` and `src/ImportToPlanner.Web/Program.cs`
- [X] T018 [P] [US1] Implement Azure Table adapters for commercial accounts and audit events in `src/ImportToPlanner.Infrastructure.Graph/CommercialAccounts/TableCommercialAccountStore.cs` and `src/ImportToPlanner.Infrastructure.Graph/CommercialAccounts/TableCommercialAuditStore.cs`
- [X] T019 [US1] Register the commercial account and audit adapters in `src/ImportToPlanner.Infrastructure.Graph/DependencyInjection.cs`
- [X] T020 [US1] Implement authenticated session identity resolution for tenant and user identifiers in `src/ImportToPlanner.Web/ClaimsSessionIdentityContextAccessor.cs` and `src/ImportToPlanner.Web/DependencyInjection.cs`
- [X] T021 [P] [US1] Add the commercial login gate and first-sign-in explanatory content in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T022 [US1] Wire commercial-mode access decisions into the sign-in and request pipeline in `src/ImportToPlanner.Web/Program.cs` and `src/ImportToPlanner.Web/DependencyInjection.cs`

**Checkpoint**: A commercial user who is signed out sees the explanatory login gate, a first successful sign-in creates the minimal account and audit record, and a returning commercial user reaches the main workflow directly.

---

## Phase 4: User Story 2 - Signed-In User Can See Their Identity Context (Priority: P2)

**Goal**: Show the signed-in email address and tenant name clearly on the main screen and expose a profile link without reorganising the existing navigation.

**Independent Test**: Sign in to the commercial deployment and confirm the top-right area shows the email address, shows the tenant name when available, and exposes a profile link from the main screen.

### Tests for User Story 2

- [X] T023 [P] [US2] Add UI coverage for signed-in email, tenant-name display, and profile-link visibility in `tests/ImportToPlanner.Web.Tests/HomePageIdentityContextTests.cs`
- [X] T024 [P] [US2] Add claims-to-session-identity coverage for email and tenant-name extraction in `tests/ImportToPlanner.Web.Tests/ClaimsSessionIdentityContextAccessorTests.cs`

### Implementation for User Story 2

- [X] T025 [US2] Extend session identity extraction for display-only email and tenant-name values in `src/ImportToPlanner.Web/ClaimsSessionIdentityContextAccessor.cs`
- [X] T026 [US2] Add the signed-in identity summary and profile entry point in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T027 [P] [US2] Add tenant-name-aware display mapping in `src/ImportToPlanner.Web/Presenters/SessionIdentityPresenter.cs`
- [X] T028 [US2] Create the routed profile page shell and navigation target in `src/ImportToPlanner.Web/Components/Pages/Profile.razor` and `src/ImportToPlanner.Web/Components/Routes.razor`

**Checkpoint**: A signed-in commercial user can confirm the active work account and tenant context from the main screen and can navigate to a profile route without changing the rest of the page structure.

---

## Phase 5: User Story 3 - User Manages and Deletes Their Account (Priority: P3)

**Goal**: Let a commercial user review their stored account details, delete their account, be blocked during the retention window, restore the account when eligible, and purge expired records on schedule.

**Independent Test**: Sign in to the commercial deployment, open the profile page, confirm only `TenantId`, `UserId`, and created date are shown from persisted data, delete the account, confirm access is blocked during the retention period with a restore option, restore the account, and verify expired deleted accounts can be purged after the retention window.

### Tests for User Story 3

- [X] T029 [P] [US3] Add delete, restore, and blocked-deleted access coverage in `tests/ImportToPlanner.Tests/CommercialAccountLifecycleUseCaseTests.cs`
- [X] T030 [P] [US3] Add retention-sweep coverage for expired accounts and audit records in `tests/ImportToPlanner.Tests/CommercialRetentionSweepTests.cs`
- [X] T031 [P] [US3] Add profile-page component coverage for stored details and delete confirmation in `tests/ImportToPlanner.Web.Tests/ProfilePageTests.cs`
- [X] T032 [P] [US3] Add deleted-account guidance and restore-flow coverage in `tests/ImportToPlanner.Web.Tests/HomePageCommercialRetentionTests.cs`

### Implementation for User Story 3

- [X] T033 [US3] Implement profile retrieval, delete, restore, and purge use cases in `src/ImportToPlanner.Application/Services/GetCommercialProfileUseCase.cs`, `src/ImportToPlanner.Application/Services/DeleteCommercialAccountUseCase.cs`, `src/ImportToPlanner.Application/Services/RestoreCommercialAccountUseCase.cs`, and `src/ImportToPlanner.Application/Services/PurgeExpiredCommercialAccountsUseCase.cs`
- [X] T034 [US3] Register the lifecycle and retention services in `src/ImportToPlanner.Application/DependencyInjection.cs` and `src/ImportToPlanner.Web/Program.cs`
- [X] T035 [US3] Extend the Azure Table account and audit adapters for delete, restore, and retention queries in `src/ImportToPlanner.Infrastructure.Graph/CommercialAccounts/TableCommercialAccountStore.cs` and `src/ImportToPlanner.Infrastructure.Graph/CommercialAccounts/TableCommercialAuditStore.cs`
- [X] T036 [US3] Build the profile page with stored account details and delete confirmation in `src/ImportToPlanner.Web/Components/Pages/Profile.razor` and `src/ImportToPlanner.Web/Components/Pages/Profile.razor.cs`
- [X] T037 [US3] Add deleted-account retention messaging and restore actions to the commercial entry flow in `src/ImportToPlanner.Web/Components/Pages/Home.razor`
- [X] T038 [US3] Add the commercial-only retention sweep hosted service in `src/ImportToPlanner.Web/Services/CommercialAccountRetentionHostedService.cs` and `src/ImportToPlanner.Web/Program.cs`

**Checkpoint**: Commercial users can review their stored account record, delete and restore the same account during retention, and the system has a scheduled path to purge expired deleted accounts and expired audit records.

---

## Phase 6: User Story 4 - Self-Hosted Access Keeps Today’s Behaviour (Priority: P4)

**Goal**: Preserve the current self-hosted automatic Microsoft 365 sign-in path and ensure commercial account persistence is never required when commercial mode is disabled.

**Independent Test**: Run the app with commercial mode disabled and confirm the existing automatic Microsoft 365 sign-in path still governs access, the commercial login gate never appears, and no commercial account is created or required.

### Tests for User Story 4

- [X] T039 [P] [US4] Add self-hosted parity coverage for authentication and home-page gating in `tests/ImportToPlanner.Web.Tests/HostedAuthenticationEventTests.cs` and `tests/ImportToPlanner.Web.Tests/HomePageSmokeTests.cs`
- [X] T040 [P] [US4] Add startup and access-decision coverage for commercial mode disabled in `tests/ImportToPlanner.Web.Tests/StartupValidationTests.cs` and `tests/ImportToPlanner.Tests/CommercialAccessUseCaseTests.cs`

### Implementation for User Story 4

- [X] T041 [US4] Preserve self-hosted automatic sign-in behaviour and commercial gating bypass in `src/ImportToPlanner.Web/Program.cs` and `src/ImportToPlanner.Web/DependencyInjection.cs`
- [X] T042 [US4] Return `SelfHostedBypass` without commercial persistence when the feature is disabled in `src/ImportToPlanner.Application/Services/CommercialAccessUseCase.cs`
- [X] T043 [P] [US4] Capture self-hosted parity verification steps in `specs/008-commercial-user-accounts/quickstart.md`

**Checkpoint**: Self-hosted deployments still use the existing sign-in path, do not show the commercial first-login gate, and do not depend on commercial account storage.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finalise documentation, privacy-safe wording, and end-to-end verification across all stories.

- [X] T044 [P] Update contributor and operator guidance for commercial mode, storage tables, and retention behaviour in `README.md` and `docs-internal/developer-quickstart.md`
- [X] T045 [P] Update internal Microsoft Graph and operational policy guidance for commercial account storage and audit retention in `docs-internal/microsoft-graph-guidelines.md` and `docs-internal/engineering-policies.md`
- [X] T046 [P] Review commercial user-facing wording and diagnostics for UK English and privacy-safe messages in `src/ImportToPlanner.Web/Components/Pages/Home.razor`, `src/ImportToPlanner.Web/Components/Pages/Profile.razor`, and `src/ImportToPlanner.Web/UserFacingFailureDiagnostics.cs`
- [X] T047 Run the full commercial-mode, retention, and self-hosted verification checklist in `specs/008-commercial-user-accounts/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the MVP commercial sign-in path
- **Phase 4 (US2)**: Depends on US1 because the signed-in identity display builds on the commercial session-access seam introduced there
- **Phase 5 (US3)**: Depends on US1 and US2 because profile management, restore flow, and retention messaging build on the account store and profile route introduced earlier
- **Phase 6 (US4)**: Depends on Phase 2 and remains independently testable with commercial mode disabled
- **Phase 7 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is the MVP
- **US2 (P2)**: Depends on US1 commercial-session resolution, then remains independently testable through main-screen identity checks
- **US3 (P3)**: Depends on US1 account persistence and the profile navigation introduced in US2
- **US4 (P4)**: Depends only on the shared commercial-mode configuration seam from Foundational and can be validated with the feature disabled

### Within Each User Story

- Write the story tests first and confirm they fail before implementation
- Create or extend application contracts before wiring DI and outer-layer adapters
- Complete storage and access logic before changing the user-facing commercial flow that consumes it
- Finish the story's independent verification path before expanding to the next priority when working sequentially

### Parallel Opportunities

- T002-T004 can run in parallel during Setup after T001 establishes the AppHost parameter name
- T005-T007 and T009-T012 can run in parallel once Setup is complete because they touch separate contract, validation, and test files
- For US1, T013-T015 can run in parallel, T018 can proceed alongside T016, and T021 can proceed once the access-decision shape is stable
- For US2, T023 and T024 can run in parallel, then T026 and T027 can proceed in parallel before T028 finalises the profile route shell
- For US3, T029-T032 can run in parallel before the lifecycle services and Web flow work are wired together
- For US4, T039, T040, and T043 can run in parallel while T041-T042 are implemented sequentially on the shared auth and access path
- T044-T046 can run in parallel after story work is complete, before T047 captures final verification

---

## Parallel Example: User Story 1

```text
T013 (CommercialAccessUseCaseTests.cs)  ║  T014 (CommercialAccountTableStoreTests.cs)  ║  T015 (HomePageCommercialAccessTests.cs)
then:
T016 (CommercialAccessUseCase.cs)  ║  T018 (table account and audit adapters)
then:
T019 (infrastructure registration)  ║  T020 (session identity accessor)  ║  T021 (Home.razor login gate)
then:
T022 (web pipeline wiring)
```

---

## Parallel Example: User Story 3

```text
T029 (CommercialAccountLifecycleUseCaseTests.cs)  ║  T030 (CommercialRetentionSweepTests.cs)  ║  T031 (ProfilePageTests.cs)  ║  T032 (HomePageCommercialRetentionTests.cs)
then:
T033 (lifecycle use cases)  ║  T035 (table adapter extensions)
then:
T036 (Profile.razor and Profile.razor.cs)  ║  T037 (Home.razor deleted-account flow)
then:
T038 (CommercialAccountRetentionHostedService.cs)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm the commercial login gate, first-sign-in account creation, and returning-user flow before adding display or profile features

### Incremental Delivery

1. Finish Setup + Foundational to expose the configuration, contracts, and test seams
2. Deliver US1 as the commercial account MVP
3. Add US2 to show signed-in identity context and the profile entry point
4. Add US3 to enable profile management, deletion, restoration, and purge behaviour
5. Validate US4 with commercial mode disabled so self-hosted parity remains intact
6. Finish with cross-cutting documentation, wording, and end-to-end verification

### Parallel Team Strategy

1. One engineer completes Setup and Foundational work to stabilise the shared seams
2. After Foundational, one engineer can own US1 application/infrastructure work while another prepares the Web tests and login-gate UI
3. Once US1 is stable, US2 display work and US3 lifecycle test preparation can proceed in parallel
4. A final pass validates US4 parity and completes the cross-cutting documentation and verification tasks

---

## Summary

- **Total tasks**: 47
- **Setup + Foundational**: 12 (T001-T012)
- **US1**: 10 (T013-T022)
- **US2**: 6 (T023-T028)
- **US3**: 10 (T029-T038)
- **US4**: 5 (T039-T043)
- **Polish**: 4 (T044-T047)
- **Parallel [P] tasks**: 28 of 47

Independent test criteria by story:

- **US1**: A signed-out commercial visitor sees the explanatory login gate, a first successful sign-in creates the minimal account, and a returning commercial user reaches the main workflow directly
- **US2**: A signed-in commercial user can identify the active email address, see the tenant name when available, and find the profile link from the main screen
- **US3**: A commercial user can review their stored account details, delete the account, be blocked during retention, restore the same account, and support purge of expired deleted records
- **US4**: A self-hosted deployment keeps the existing automatic Microsoft 365 sign-in path and never requires the commercial account flow

Suggested MVP scope: Complete Phase 1, Phase 2, and Phase 3 (US1) before adding identity display, profile management, or self-hosted parity refinements.

Format validation: Every task uses the required checklist form with checkbox, task ID, optional `[P]`, required story label in user-story phases, and an exact file path.
