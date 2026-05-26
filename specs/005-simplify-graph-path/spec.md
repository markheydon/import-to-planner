# Feature Specification: Simplify Graph Runtime Path

**Feature Branch**: `005-simplify-graph-path`  
**Created**: 2026-05-25  
**Status**: Draft  
**Input**: User description: "Remove all scaffolding that existed before Microsoft Graph integration was complete, remove the Application-layer deployment-mode concept that was a clean-architecture boundary violation, and produce a single uniform code path that requires no runtime mode-switching. This spec does not add any new features; it is a targeted cleanup and simplification of what spec 004 introduced."

## Clarifications

### Session 2026-05-25

- Q: How should the app behave if `AzureAd:TenantId` is missing or blank? → A: Fail fast at startup with a clear, human-friendly configuration error experience.
- Q: How should the app behave if removed planner, storage, or deployment-mode keys are still present? → A: Fail fast with a clear, human-friendly configuration error because there is no supported legacy upgrade path.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run One Supported Production Path (Priority: P1)

As an operator or signed-in user, I want the app to run through one supported planner and tenant-metadata path so that the import workflow behaves consistently without hidden runtime switches.

**Why this priority**: Removing the obsolete split path is the core purpose of this feature and reduces the risk of inconsistent behaviour between deployments.

**Independent Test**: Can be fully tested by running the existing import workflow in both single-tenant and multi-tenant sign-in configurations and confirming that planner actions and tenant metadata handling use the same supported production path without any runtime mode selection.

**Acceptance Scenarios**:

1. **Given** the app is configured for normal use, **When** a user signs in and runs the import workflow, **Then** planner actions use the single supported planner gateway path and do not branch between alternate runtime implementations.
2. **Given** the app needs tenant-scoped operational metadata, **When** that metadata is read or written, **Then** the app uses the single supported metadata path and does not switch between alternate storage implementations at runtime.
3. **Given** a deployment is configured for either one tenant or many tenants, **When** the user reaches the working import workflow, **Then** the workflow behaviour remains the same apart from the sign-in authority implied by tenant configuration.

---

### User Story 2 - Keep the Working Authentication Guards While Removing Mode Plumbing (Priority: P2)

As a maintainer, I want the existing multi-tenant authentication protections to remain intact while removing the separate deployment-mode concept so that the app keeps the correct sign-in safeguards without an inner-layer boundary violation.

**Why this priority**: The current authentication protections are already correct and must survive the cleanup, but the mode concept introduced unnecessary architectural coupling.

**Independent Test**: Can be fully tested by verifying that unsupported account rejection, admin-consent redirection, and tenant-context mismatch handling still behave correctly when tenant behaviour is derived directly from the web project's tenant-authority configuration.

**Acceptance Scenarios**:

1. **Given** the web project is configured with a shared organisational authority value, **When** a supported work or school user signs in, **Then** the app applies the existing multi-tenant protections without relying on a separate deployment-mode setting.
2. **Given** the web project is configured with a tenant-specific authority value, **When** a user signs in, **Then** the app behaves as single-tenant without relying on a separate deployment-mode setting.
3. **Given** the existing auth guards encounter an unsupported account type, missing admin consent, or tenant-context mismatch, **When** sign-in is processed, **Then** the current guard outcomes remain unchanged by this cleanup.

---

### User Story 3 - Remove Obsolete Configuration and Test Scaffolding (Priority: P3)

As a maintainer, I want obsolete configuration keys and production-like in-memory scaffolding removed so that the codebase reflects the supported architecture and tests use boundary doubles only where needed.

**Why this priority**: Cleanup is incomplete if deleted production paths remain in configuration or tests, because they continue to imply unsupported operating modes.

**Independent Test**: Can be fully tested by reviewing configuration and automated tests to confirm that obsolete runtime-mode and hosted-storage switches are gone, while tests now use abstraction-boundary doubles or are removed when they only exercised deleted behaviour.

**Acceptance Scenarios**:

1. **Given** application and web configuration are reviewed, **When** obsolete planner, storage, and deployment-mode keys are searched for, **Then** they are no longer present in supported code paths, configuration files, or tests.
2. **Given** automated tests previously depended on deleted in-memory production scaffolding, **When** those tests are updated, **Then** they either use doubles at the abstraction boundary or are removed if they only covered deleted behaviour.
3. **Given** the web project needs tenant-authority configuration, **When** configuration ownership is reviewed, **Then** that value is owned by the web project rather than being forwarded from the AppHost.

### Edge Cases

- What happens when the web project tenant-authority value is set to the shared organisational value used for multi-tenant sign-in?
- What happens when the tenant-authority value is set to a specific tenant instead of the shared organisational value?
- What happens when configuration still contains obsolete planner, storage, or deployment-mode switches after the cleanup?
- How does the app behave if the required web-project tenant-authority value is missing or blank at startup?
- What happens to tests that only validated behaviour unique to the deleted in-memory production paths?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST remove alternate runtime implementations for planner access and tenant operational metadata so that supported production behaviour runs through one uniform path.
- **FR-002**: The system MUST treat the Microsoft Graph-backed planner path as the only supported production planner path.
- **FR-003**: The system MUST treat the existing tenant-scoped persistent metadata path as the only supported production metadata path.
- **FR-004**: The system MUST remove planner, storage, and deployment-mode configuration switches that exist only to select between obsolete runtime paths.
- **FR-004a**: If removed planner, storage, or deployment-mode keys are still present in runtime configuration, the system MUST stop startup and present a clear, human-friendly configuration error experience rather than ignoring the obsolete values.
- **FR-005**: The system MUST remove the inner-layer deployment-mode concept and any related inner-layer types whose sole purpose is to communicate deployment topology.
- **FR-006**: The system MUST preserve the current authentication guard behaviour for unsupported account rejection, admin-consent error redirection, and tenant-context mismatch handling.
- **FR-007**: The system MUST derive single-tenant versus multi-tenant sign-in behaviour directly from the web project's tenant-authority configuration without using a separate deployment-mode type or setting.
- **FR-008**: The system MUST treat the shared organisational authority value as multi-tenant behaviour and any other supported tenant-authority value as single-tenant behaviour.
- **FR-009**: The system MUST keep the tenant-authority value as a required web-project configuration concern and MUST NOT rely on the AppHost to forward that value into the app.
- **FR-009a**: If the required web-project tenant-authority value is missing or blank, the system MUST stop startup and present a clear, human-friendly configuration error experience rather than an unhandled framework exception page.
- **FR-010**: The system MUST update automated tests affected by deleted production scaffolding so that they use doubles at the abstraction boundary or are removed when they only validate deleted behaviour.
- **FR-011**: The system MUST preserve the current validation, preview, confirmation, and reporting workflow behaviour introduced by earlier work.
- **FR-012**: The system MUST keep consent handling logic, tenant context extraction, current Graph integration behaviour, current tenant metadata persistence behaviour, and AppHost resource declarations unchanged except where obsolete mode wiring is being removed.

### Key Entities *(include if feature involves data)*

- **Tenant Authority Configuration**: The web-project setting that determines whether sign-in behaves as single-tenant or multi-tenant.
- **Planner Gateway Boundary**: The application boundary used by the import workflow to perform planner actions, with one supported production path and test doubles used only in tests.
- **Tenant Operational Metadata Boundary**: The application boundary used to read and write tenant-scoped operational metadata, with one supported production path and test doubles used only in tests.
- **Authentication Guard Behaviour**: The existing set of sign-in protections covering unsupported account types, consent errors, and tenant-context mismatch handling.
- **Obsolete Runtime Switches**: Planner, storage, and deployment-mode configuration values that no longer represent supported behaviour and must be removed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Configuration and code review confirm that no supported runtime path branches between alternate planner or tenant-metadata implementations.
- **SC-002**: Configuration, code, and test review confirm that obsolete planner, storage, and deployment-mode switches are absent from the maintained codebase.
- **SC-002a**: In configuration validation testing, 100% of startup attempts that still provide removed planner, storage, or deployment-mode keys fail with a clear, human-friendly configuration error experience.
- **SC-003**: In regression testing, 100% of covered sign-in guard scenarios retain the same outcomes for unsupported account rejection, admin-consent guidance, and tenant-context mismatch handling.
- **SC-004**: In verification for this feature, the existing validation, preview, confirmation, and reporting workflow completes successfully for both a tenant-specific authority value and the shared organisational authority value.
- **SC-004a**: In configuration validation testing, 100% of startup attempts with a missing or blank tenant-authority value fail before sign-in becomes available and show a clear, human-friendly configuration error experience.
- **SC-005**: All automated tests updated for this feature pass without depending on deleted in-memory production scaffolding.
- **SC-006**: Architecture review confirms that deployment-topology intent is no longer represented by Application-layer mode types or mode-based branching.

## Assumptions

- This feature is a cleanup and simplification change only; it does not add new end-user workflow features.
- The current Microsoft Graph planner behaviour and current tenant metadata persistence behaviour are already correct and remain the production baseline.
- The current authentication guard outcomes for unsupported account types, admin-consent handling, and tenant-context mismatch handling are correct and must remain unchanged.
- The AppHost itself and broader hosting-resource redesign are outside this specification apart from removing obsolete assumptions about forwarded tenant-authority configuration during implementation planning.
- Consent logic, tenant context extraction, and working multi-tenant support introduced earlier remain in scope only for regression protection, not redesign.
