# Feature Specification: Multi-Tenant Hosted Deployment

**Feature Branch**: `003-add-multitenant-deployment`  
**Created**: 2026-05-15  
**Status**: Draft  
**Input**: User description: "Add support for a shared hosted multi-tenant deployment model alongside the existing self-hosted single-tenant setup. Users from multiple Microsoft 365 tenants should be able to sign in safely, while self-hosted users must still be able to run the app with their own single-tenant Entra configuration. The feature should cover tenant-aware authentication, tenant-aware handling of any stored application metadata, delegated Microsoft Graph access using the signed-in user’s token, graceful handling of missing admin consent with a clear user-facing path for administrators, documentation for both hosted and self-hosted deployment modes, privacy and data-handling guidance, and hosted operational considerations including Aspire-compatible resources, reliability of the existing validation/preview/reporting flow, and OpenTelemetry-based monitoring. The design should support a secure hosted model without introducing unnecessary stored data or blocking future account or entitlement management. Additional details available in issue #10 -- https://github.com/markheydon/import-to-planner/issues/10"

## Clarifications

### Session 2026-05-15

- Q: Which hosted metadata scope should the spec treat as in-scope for persistent storage? → A: Persist only a minimal tenant record, such as tenant identifier, deployment or consent status, and operational timestamps.
- Q: When required admin consent is missing, what path should the spec require the app to present? → A: Show an in-app explanation plus an administrator consent link or equivalent direct action path.
- Q: How should the spec require tenant context to appear in hosted operational telemetry? → A: Use a pseudonymous or access-controlled tenant correlation value for telemetry, with a controlled way to resolve it when support needs it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hosted users sign in and import within their own tenant boundary (Priority: P1)

A user from a customer organisation accesses the shared hosted deployment, signs in with their Microsoft 365 work or school account, and completes the existing import workflow without being exposed to another organisation's data, configuration, or activity.

**Why this priority**: The hosted deployment only succeeds if users from multiple organisations can use the app safely without weakening tenant boundaries or regressing the current import workflow.

**Independent Test**: Can be fully tested by signing in as users from two different Microsoft 365 tenants against the same hosted deployment and confirming that each user can complete validation, preview, and reporting only against their own tenant context.

**Acceptance Scenarios**:

1. **Given** a shared hosted deployment is available, **When** a user from a supported Microsoft 365 tenant signs in, **Then** the app grants access using that user's tenant context and shows only import data and choices that belong to that tenant.
2. **Given** two users from different Microsoft 365 tenants use the same hosted deployment, **When** each user runs the import flow, **Then** tenant-specific state, metadata, and results remain isolated from one another.
3. **Given** a hosted user has signed in successfully, **When** the user validates, previews, and executes an import, **Then** the app continues to use that signed-in user's delegated permissions throughout the workflow.

---

### User Story 2 - Self-hosted organisations retain the existing single-tenant setup (Priority: P1)

An organisation running its own deployment can continue to configure the app for a single Microsoft 365 tenant and use the current sign-in and import experience without having to adopt the hosted multi-tenant model.

**Why this priority**: The feature must extend the product without breaking the existing self-hosted usage model.

**Independent Test**: Can be fully tested by running a self-hosted deployment with a single-tenant identity configuration and confirming that sign-in, validation, preview, execution, and reporting still behave as they do today.

**Acceptance Scenarios**:

1. **Given** an organisation deploys the app in self-hosted mode, **When** it provides single-tenant identity configuration, **Then** users can sign in only from that configured tenant and use the import workflow successfully.
2. **Given** a self-hosted deployment is configured for a single tenant, **When** a user outside that tenant attempts to sign in, **Then** the app denies access safely and explains that the deployment is limited to the organisation's own tenant.

---

### User Story 3 - Users and administrators get a clear consent path (Priority: P2)

A hosted or self-hosted user who can sign in but lacks the necessary Microsoft 365 permissions sees a clear explanation of what consent is missing, what that means for the import workflow, and what their administrator must do next.

**Why this priority**: Consent failures are expected in real deployments. Handling them clearly avoids support churn, reduces confusion, and prevents the workflow from failing as an opaque error.

**Independent Test**: Can be fully tested by signing in with an account or tenant that has not granted the required permissions and confirming that the app presents an actionable, user-facing consent message with an administrator path instead of an unhandled failure.

**Acceptance Scenarios**:

1. **Given** a signed-in user lacks required consent for the hosted or self-hosted deployment, **When** the user attempts an action that needs those permissions, **Then** the app shows an explanatory message describing the missing consent and provides an administrator consent link or equivalent direct action path.
2. **Given** an administrator needs to grant consent, **When** they view the app's guidance or deployment documentation, **Then** they can identify the correct consent path for the relevant deployment mode.
3. **Given** consent has not yet been granted, **When** the user remains on the page, **Then** the rest of the app continues to behave predictably and does not expose misleading success states.

---

### User Story 4 - Hosted operators can support the service responsibly (Priority: P3)

A hosted service operator can understand what limited metadata is stored, monitor the health of the shared deployment, and investigate failures in validation, preview, and reporting without weakening tenant isolation or collecting unnecessary customer data.

**Why this priority**: A shared hosted model is only sustainable if it is supportable in production and aligned with privacy expectations.

**Independent Test**: Can be fully tested by operating a hosted deployment under routine and failure conditions and confirming that service telemetry, documentation, and operational guidance make tenant-scoped investigation possible without requiring broader customer data retention.

**Acceptance Scenarios**:

1. **Given** the shared hosted deployment is in use, **When** validation, preview, or reporting succeeds or fails, **Then** operators can observe the outcome and investigate issues using tenant-aware service telemetry that relies on pseudonymous or access-controlled tenant correlation.
2. **Given** the app stores operational metadata for hosted use, **When** that metadata is reviewed, **Then** it is limited to what is necessary for operation, support, and tenant separation.
3. **Given** deployment documentation is followed, **When** a team chooses hosted or self-hosted mode, **Then** it can understand the mode-specific privacy, consent, and operational responsibilities before go-live.

### Edge Cases

- What happens when a user signs in to the hosted deployment from a tenant that has not yet granted the required permissions? The app must stop the affected workflow step safely and present a clear in-app administrator consent path instead of failing unexpectedly.
- What happens when a user is presented with an in-app administrator consent but doesn't have the required rights to approve the request, the request fails, or the user denies the request? The app must deal with not getting the required permissions needed to proceed gracefully.
- What happens when a deployment is configured for self-hosted single-tenant use but receives a sign-in attempt from a different tenant? The sign-in must be rejected clearly without exposing internal configuration details.
- What happens when a hosted user's tenant has no previously stored metadata? The workflow must still operate correctly and create only the minimum tenant-associated metadata required for that session or supported operations.
- What happens when tenant-associated metadata cannot be read or written during hosted use? The app must preserve tenant separation, avoid cross-tenant fallback behaviour, and provide a clear failure state.
- What happens when validation succeeds but preview or reporting later fails in the hosted model? The user experience must remain predictable, and operators must be able to distinguish which workflow stage failed for the affected tenant.
- What happens when hosted telemetry is reviewed during support? Tenant-scoped investigation must be possible without broadly exposing raw tenant identifiers in operational telemetry.
- What happens when documentation is consulted before deployment? The guidance must make the differences between hosted and self-hosted mode, consent expectations, and data handling responsibilities explicit.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support two deployment modes: a shared hosted mode that allows sign-in from multiple Microsoft 365 tenants, and a self-hosted mode that restricts sign-in to a single configured tenant.
- **FR-002**: The system MUST determine the active tenant context for each signed-in user and apply that tenant context consistently throughout sign-in, import validation, preview, execution, and reporting.
- **FR-003**: The system MUST preserve tenant isolation in the shared hosted mode so that users cannot access another tenant's metadata, workflow state, support data, or results.
- **FR-004**: The system MUST continue to support the current self-hosted single-tenant experience without requiring hosted-only configuration or behaviours.
- **FR-005**: The system MUST use the signed-in user's delegated Microsoft Graph permissions for user-initiated Microsoft 365 operations.
- **FR-006**: The system MUST detect when required consent is missing and present a clear user-facing explanation of the impact, along with an in-app administrator consent link or equivalent direct action path that is appropriate for the active deployment mode.
- **FR-007**: The system MUST ensure any stored application metadata used for hosted operation is associated with the relevant customer tenant.
- **FR-008**: The system MUST minimise stored application metadata and MUST limit persistent hosted metadata to a minimal tenant record containing only the tenant identifier, deployment or consent status, and operational timestamps needed for supportability.
- **FR-009**: The system MUST NOT persist imported task content, plan content, or future account or entitlement placeholders as part of this feature's hosted tenant metadata design.
- **FR-010**: The system MUST preserve the reliability of the existing validation, preview, and reporting workflow in both deployment modes.
- **FR-011**: The system MUST provide documentation that explains hosted and self-hosted deployment modes, consent expectations, configuration responsibilities, privacy boundaries, and operational responsibilities.
- **FR-012**: The system MUST provide privacy and data-handling guidance that explains what tenant-related data is accessed, what limited data is stored, why it is needed, and what the deployment operator is responsible for.
- **FR-013**: The hosted design MUST support service monitoring that allows operators to identify tenant-scoped failures, reliability issues, and abnormal workflow outcomes.
- **FR-014**: The hosted design MUST use pseudonymous or access-controlled tenant correlation in operational telemetry, with a controlled support path for resolving correlation values when investigation requires it.
- **FR-015**: The hosted design MUST allow deployment on resources compatible with the repository's Aspire-based hosting approach.
- **FR-016**: The system MUST avoid design choices that would unnecessarily block future account, entitlement, or subscription management capabilities.

### Quality and Non-Functional Requirements *(mandatory)*

- **NFR-001 Dependency Direction**: The feature MUST preserve the existing architectural boundary direction so that tenancy and deployment-mode decisions do not force inappropriate coupling between presentation, application, domain, and infrastructure concerns.
- **NFR-002 Boundary Clarity**: Tenant context, consent outcomes, and deployment-mode differences MUST be explicit in use-case boundaries and user-facing behaviour.
- **NFR-003 Security Posture**: Hosted multi-tenant behaviour MUST maintain clear tenant isolation and least-necessary data handling in normal, degraded, and failure states.
- **NFR-004 Reliability**: Validation, preview, execution, and reporting MUST remain dependable in both deployment modes, with no new failure mode that leaves users unable to understand the state of their import.
- **NFR-005 Observability**: Hosted operations MUST produce sufficient monitoring evidence for service health, failure investigation, and tenant-scoped support without widening stored customer data or broadly exposing raw tenant identifiers.
- **NFR-006 Documentation Quality**: Hosted and self-hosted guidance MUST be understandable to deployment administrators without requiring them to infer consent, privacy, or operational responsibilities.
- **NFR-007 Policy Alignment**: The feature MUST identify and satisfy applicable repository engineering policies for runtime-mode verification, privacy wording, accessibility of user guidance, and operational safety.
- **NFR-008 Future Readiness**: The tenancy model MUST leave room for later account or entitlement management without requiring a redesign of tenant boundaries or stored metadata ownership.

### Key Entities *(include if feature involves data)*

- **Deployment Mode**: The operating model for the app, distinguishing between shared hosted multi-tenant use and self-hosted single-tenant use.
- **Tenant Context**: The signed-in organisation boundary applied to authentication, authorisation, workflow state, stored metadata, telemetry, and support activity.
- **Consent Status**: The current state of required delegated permissions for a tenant or user, including whether more administrator action is needed before the workflow can continue.
- **Tenant Metadata Record**: The minimum persistent application-managed information associated with a tenant, limited to the tenant identifier, deployment or consent status, and operational timestamps needed for operation and support.
- **Operational Event**: A recorded service outcome or failure used to understand the health of hosted validation, preview, execution, and reporting.
- **Telemetry Tenant Correlation**: The pseudonymous or access-controlled correlation value used to connect hosted operational events to the affected tenant during authorised support activity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In hosted mode, users from at least two different Microsoft 365 tenants can sign in to the same deployment and complete the primary import workflow without cross-tenant data exposure.
- **SC-002**: In self-hosted mode, users from the configured tenant can complete the primary import workflow successfully, and sign-in attempts from other tenants are rejected in every tested case.
- **SC-003**: In consent-missing scenarios, 100% of tested users receive a clear explanatory message and an administrator next step instead of an unhandled error.
- **SC-004**: Validation, preview, and reporting continue to complete successfully in both deployment modes for at least 95% of non-error test runs using the same acceptance dataset used before this feature.
- **SC-005**: Hosted operational guidance enables an operator to determine the affected tenant and workflow stage for every tested validation, preview, or reporting failure within 10 minutes.
- **SC-006**: Documentation for both deployment modes enables a deployment administrator to identify the correct sign-in model, consent expectations, and data-handling responsibilities without additional clarification in a structured review.
- **SC-007**: Review of the design confirms that all newly stored application metadata has a defined tenant association and a documented operational purpose.
- **SC-008**: Review of the design confirms that future account or entitlement management can be added without redefining the core tenant boundary.
- **SC-009**: In consent-missing scenarios, 100% of tested users are offered an in-app administrator consent link or equivalent direct action path that matches the active deployment mode.
- **SC-010**: Hosted telemetry reviews confirm that tenant-scoped investigations can be performed in every tested failure scenario without requiring raw tenant identifiers to be broadly visible in standard operational telemetry.

## Assumptions

- The existing import workflow remains the product's core capability; this feature extends its deployment and tenancy model rather than changing its user purpose.
- Personal Microsoft accounts are out of scope; supported sign-in remains limited to Microsoft 365 work or school accounts.
- Future billing, subscription, and entitlement behaviour is out of scope for this feature, but the design must not make those additions harder.
- Any new hosted persistence is limited to a minimal tenant record for tenant separation, service operation, consent handling, and supportability.
- The repository's existing monitoring and deployment direction remains in place; this feature adds hosted operational requirements rather than replacing the current approach.
- User-facing and operator-facing documentation will both be updated as part of the same feature so that hosted and self-hosted responsibilities are clear at release time.
