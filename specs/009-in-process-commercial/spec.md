# Feature Specification: Isolate Commercial Accounts (Single Hosted Process)

**Feature Branch**: `009-in-process-commercial`

**Created**: 2026-08-31

**Status**: Draft

**Input**: User description: "From GitHub issue #99: keep hosted commercial accounts inside a single user-facing deployment. Remove the unused extra commercial process left over from an abandoned split. Isolate commercial account, audit, retention, and commercial tenant-metadata persistence in an outer commercial capability registered only when commercial mode is on. Do not change the user-facing commercial behaviour already shipped by the commercial user accounts feature (login gate, first-sign-in account create, identity chrome, profile delete/restore, 6-month retention then purge, 12-month audit, self-host automatic Microsoft 365 sign-in). Implement as a new feature from current main; do not revive the abandoned process-split work."

## User Scenarios & Testing *(mandatory)*

This feature does not reopen the commercial user-account journeys already shipped. Those remain the behaviour contract. The stories below are the new outcomes: operators run one hosted application; commercial users see no regression; self-host operators stay free of commercial storage and login; commercial account rules stay outside import and planner policy.

### User Story 1 - Hosted Commercial Runs as One Application (Priority: P1)

A hosted commercial operator deploys the commercial version of the app and expects a single user-facing application. Today an unused extra commercial process is still provisioned even though it does no commercial work. After this change, commercial mode deploys only the application users already use, with commercial account storage attached only to that application.

**Why this priority**: The leftover extra process is wasted hosted capacity, a confusing operational surface, and a false signal that commercial accounts live in a separate service. Removing it is the primary operator-visible outcome of this feature.

**Independent Test**: Can be fully tested by enabling commercial mode in a hosted-style deployment, confirming only one user-facing application is created, confirming commercial account storage is attached only to that application, and confirming no unused extra commercial process exists.

**Acceptance Scenarios**:

1. **Given** a hosted deployment with commercial mode enabled, **When** the operator inspects the deployed application graph, **Then** there is exactly one user-facing application and no unused extra commercial process.
2. **Given** a hosted deployment with commercial mode enabled, **When** commercial account, audit, retention, or commercial tenant-metadata storage is required, **Then** that storage is attached only to the user-facing application.
3. **Given** a hosted deployment that previously provisioned an unused extra commercial process, **When** this feature is in place, **Then** that extra process is absent from the solution, deployment topology, and runtime.

---

### User Story 2 - Commercial Users Keep Existing Account Behaviour (Priority: P1)

A commercial user continues to sign in, create an account, see identity on the main screen, manage their profile, delete and restore during retention, and rely on audit and purge behaviour exactly as already shipped. This feature must not change wording or journeys unless a defect in the current commercial path is found and fixed.

**Why this priority**: Isolating commercial accounts and collapsing the unused process has no value if it breaks the commercial service people already use.

**Independent Test**: Can be fully tested by repeating the commercial journeys already specified in the commercial user accounts feature (signed-out gate, first sign-in create, returning user, identity chrome, profile, delete, restore, retention purge, and audit) and confirming equivalent outcomes and UK English wording.

**Acceptance Scenarios**:

1. **Given** a person opens the commercial version while not signed in, **When** the app loads, **Then** they still see the explanatory login gate stating that signing in with Microsoft 365 creates an account.
2. **Given** a person signs in successfully to the commercial version for the first time, **When** authentication completes, **Then** the app still creates the minimal account (Tenant Id, User Id, created date) and grants access to the main experience.
3. **Given** a returning commercial user is already signed in, **When** they open the app, **Then** they skip the first-login explanation and reach the main experience.
4. **Given** a signed-in commercial user is on the main screen, **When** identity information is available, **Then** the screen still shows email and tenant name when available, with a profile link and without reorganising navigation.
5. **Given** a signed-in commercial user opens their profile, **When** they review or delete the account, **Then** they still see stored Tenant Id, User Id, and created date; Delete Account still marks the account deleted, signs them out, and starts the 6-month retention window; restore during the window still reactivates the same account; purge still happens only after expiry.
6. **Given** commercial account lifecycle and sign-in outcomes occur, **When** operators or auditors review records, **Then** create, delete, restore, and sign-in outcome events are still recorded and retained for 12 months.

---

### User Story 3 - Self-Hosted Deployments Stay Free of Commercial Overhead (Priority: P2)

A self-hosting organisation continues to use automatic Microsoft 365 sign-in with no commercial login gate, no commercial account flow, and no requirement to configure commercial account storage.

**Why this priority**: Self-hosted viability is a standing product constraint. Commercial isolation must not make self-host depend on commercial login or commercial persistence.

**Independent Test**: Can be fully tested by running with commercial mode off, confirming the existing automatic Microsoft 365 sign-in path, confirming the commercial login gate never appears, and confirming the deployment starts and runs without commercial account storage being configured.

**Acceptance Scenarios**:

1. **Given** commercial mode is disabled, **When** a user opens the app, **Then** access continues via the existing automatic Microsoft 365 sign-in behaviour and the commercial login gate does not appear.
2. **Given** commercial mode is disabled, **When** the operator inspects the deployment, **Then** no commercial account storage resource is required or provisioned.
3. **Given** commercial mode is disabled, **When** the application starts and users import tasks, **Then** the app does not require commercial persistence at startup or at runtime.

---

### User Story 4 - Commercial Account Rules Stay Outside Import Policy (Priority: P2)

Operators and maintainers need commercial account lifecycle, audit, retention, and commercial tenant-metadata persistence kept in an outer commercial capability so import and planner policy stay free of commercial account types. The user-facing app registers that capability only when commercial mode is on. Shared tenant-context ideas that both self-host and commercial adapters may implement may remain in core policy; commercial account, audit, and profile contracts must not.

**Why this priority**: Mixing commercial accounts into import policy makes self-host harder to reason about and blocks replacing commercial storage later. Isolation is the architectural outcome that makes the single-process hosted model safe.

**Independent Test**: Can be fully tested by reviewing the published capability boundaries and automated compliance checks: commercial account, audit, and profile contracts live only in the outer commercial capability; core import and planner policy contain none of them; commercial mode off does not load commercial persistence; commercial mode on uses the outer capability from the same user-facing application.

**Acceptance Scenarios**:

1. **Given** commercial mode is enabled, **When** the user-facing application starts, **Then** it registers the outer commercial capability in the same application (not as a separate user-facing service) and uses it for account, audit, retention, and commercial tenant-metadata persistence.
2. **Given** reviewers inspect import and planner policy, **When** they look for commercial account, audit, or profile contracts, **Then** those contracts are absent from core policy and present only in the outer commercial capability.
3. **Given** automated architecture compliance checks run, **When** commercial account types are introduced into core import or planner policy, **Then** those checks fail.
4. **Given** the outer commercial capability is used, **When** it performs commercial persistence, **Then** it does not take on planner import execution or planner-provider client responsibilities.

### Edge Cases

- What happens when commercial mode is on but commercial account storage is missing or misconfigured at startup?
- What happens when a commercial persistence or retention operation fails — users still receive a clear, human-friendly failure rather than raw storage diagnostics?
- What happens if someone tries to restore a separate commercial process or remote commercial service as part of this work — that approach stays out of scope unless import work regularly starves the user interface, independent background workers become necessary, or a second consumer of commercial operations appears?
- How does the app ensure self-host never loads commercial persistence “just in case”?
- What happens to existing commercial accounts, audit records, and retention windows already created under the current commercial feature — they remain valid; this feature must not require users to recreate accounts?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A hosted deployment with commercial mode enabled MUST run commercial account operations inside the same user-facing application that people already use; it MUST NOT provision an unused extra commercial process.
- **FR-002**: When commercial mode is enabled, commercial account storage MUST be attached only to that user-facing application.
- **FR-003**: When commercial mode is disabled, the deployment MUST be the user-facing application only, MUST NOT provision commercial account storage, and MUST NOT require commercial persistence at startup or runtime.
- **FR-004**: The unused extra commercial process left from the abandoned split MUST be removed from the solution, deployment topology, and runtime so it cannot be started in either mode.
- **FR-005**: Commercial account lifecycle, audit recording, retention sweep and purge, and commercial tenant-metadata persistence MUST live in an outer commercial capability, not in core import or planner policy.
- **FR-006**: Core import and planner policy MUST remain limited to import and planner rules plus shared tenant-context abstractions that both self-host and commercial adapters may implement.
- **FR-007**: Core import and planner policy MUST NOT contain commercial account, audit, or profile contracts, and MUST NOT contain commercial storage vendor types.
- **FR-008**: The outer commercial capability MUST NOT take on planner import execution or planner access on the user’s behalf.
- **FR-009**: The user-facing application MUST register the outer commercial capability only when commercial mode is enabled; when commercial mode is disabled it MUST NOT initialise commercial persistence.
- **FR-010**: The user-facing application MUST map signed-in identity and session into commercial requests; commercial operations MUST return structured results; user-facing wording MUST remain in the presentation layer (UK English).
- **FR-011**: Sign-in gate, first-time account creation, returning-user skip of the first-login explanation, main-screen identity chrome, profile contents, delete, restore during retention, 6-month retention then purge, and 12-month audit MUST remain equivalent to the already shipped commercial user accounts feature unless a defect in that path is found and corrected.
- **FR-012**: Self-hosted operators MUST never see the commercial login gate and MUST never depend on commercial storage being configured.
- **FR-013**: Commercial account storage and commercial login MUST be used only when commercial mode is on.
- **FR-014**: Operations that can fail (sign-in account access, create, delete, restore, audit write, retention purge, commercial tenant-metadata persistence) MUST return structured failures; public-facing messages MUST be human-friendly and MUST NOT dump raw storage or provider diagnostics.
- **FR-015**: Secrets, credentials, and tokens MUST NOT appear in source, logs, diagnostics presented to users, or the user interface.
- **FR-016**: Automated checks MUST fail if commercial account, audit, or profile contracts leak into core import or planner policy, or if the unused extra commercial process is reintroduced into the commercial-mode topology.
- **FR-017**: Automated checks MUST confirm commercial mode does not create the unused extra commercial process and self-host mode does not create commercial account storage.
- **FR-018**: Existing commercial accounts, audit records, and in-progress retention windows MUST remain valid after isolation; users MUST NOT be required to recreate accounts solely because commercial persistence moved to the outer capability.
- **FR-019**: Documentation for the already shipped commercial user accounts feature MAY be aligned so it no longer describes core-policy-owned commercial stores or a separate commercial process; that feature’s user stories MUST NOT be reopened as new product scope.
- **FR-020**: This feature MUST NOT change planner import execution, MUST NOT split planner work into another process, MUST NOT introduce a separate retention worker, and MUST NOT treat a remote commercial service as required.

### Key Entities

- **Deployment Access Mode**: Whether the running deployment is commercial or self-hosted; this chooses login behaviour and whether commercial persistence is present.
- **Outer Commercial Capability**: The replaceable outer home for commercial account lifecycle, audit, retention, and commercial tenant-metadata persistence, used only when commercial mode is on and hosted in the same user-facing application.
- **App Account**: Unchanged from the commercial user accounts feature: Tenant Id, User Id, created date; uniquely identified by Tenant Id and User Id.
- **Deleted Account Retention**: Unchanged: 6-month window after delete, restore of the same account during the window, purge only after expiry.
- **Account Audit Event**: Unchanged: create, delete, restore, and sign-in outcome events with Tenant Id, User Id, timestamp, and outcome; retained for 12 months.
- **Commercial Tenant Metadata**: Tenant-scoped operational metadata needed for commercial hosted operation; persisted with commercial accounts when commercial mode is on, not required for self-host.
- **Unused Extra Commercial Process**: The leftover companion process from the abandoned split that currently does no commercial work and MUST be removed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested hosted deployments with commercial mode enabled run as a single user-facing application with no unused extra commercial process.
- **SC-002**: 100% of tested commercial-mode deployments attach commercial account storage only to that user-facing application.
- **SC-003**: 100% of tested deployments with commercial mode disabled start and run without commercial account storage and without showing the commercial login gate.
- **SC-004**: 100% of the already shipped commercial user-account journeys (gate, create, returning user, identity chrome, profile, delete, restore, retention purge, audit) produce equivalent outcomes and UK English wording compared with the current commercial path.
- **SC-005**: 100% of tested existing commercial accounts remain usable after isolation without forcing account recreation.
- **SC-006**: Automated compliance checks detect 100% of attempted leaks of commercial account, audit, or profile contracts into core import or planner policy in the test suite.
- **SC-007**: A commercial user who already knows the current commercial flow can complete first sign-in or profile deletion in the same time as today (first sign-in including explanation under 2 minutes; profile deletion under 1 minute) with no extra steps introduced by this isolation.
- **SC-008**: Operators comparing a commercial-mode hosted deploy before and after this feature observe one fewer unused process and no additional user-facing steps for commercial sign-in or account management.

## Assumptions

- The commercial user accounts feature already shipped on the current mainline is the behaviour baseline; this feature preserves that baseline rather than redesigning commercial UX.
- Commercial mode versus self-hosted mode remains a deployment configuration choice available before the app chooses login and persistence behaviour.
- Microsoft 365 remains the identity source for both modes.
- Moving commercial persistence into an outer capability does not require a new account identifier scheme or a data rebuild for existing commercial records.
- A separate commercial process, remote commercial service, or dedicated retention worker remains deferred until there is evidence that import work starves the user interface, independent workers are required, or a second consumer of commercial operations appears.
- Aligning older commercial-account planning artefacts is documentation drift only; it does not reopen those user stories.
- Repository-wide test-tooling changes and agent-tooling changes are out of scope.
- Failures in commercial persistence are shown as clear user or operator messages; detailed diagnostics stay in operator channels.

## Out of Scope

- Changing planner import execution or splitting planner work into another process.
- Introducing a dedicated background retention worker or remote commercial service as a requirement.
- Repository-wide migration of test conventions.
- Spec Kit or agent-tooling changes.
- Rewriting or cherry-picking the abandoned process-split implementation as the delivery of this feature.
- Reopening commercial user-account user stories as new product scope.
