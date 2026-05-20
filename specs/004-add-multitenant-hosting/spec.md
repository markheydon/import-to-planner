# Feature Specification: Hosted Multi-Tenant Support

**Feature Branch**: `004-add-multitenant-hosting`  
**Created**: 2026-05-19  
**Status**: Draft  
**Input**: User description: "Add support for a shared hosted multi-tenant deployment model alongside the existing self-hosted single-tenant setup. Users from multiple Microsoft 365 tenants should be able to sign in safely, while self-hosted users must still be able to run the app with their own single-tenant Entra configuration. The feature should cover tenant-aware authentication, tenant-aware handling of any stored application metadata, delegated Microsoft Graph access using the signed-in user’s token, graceful handling of missing admin consent with a clear user-facing path for administrators, documentation for both hosted and self-hosted deployment modes, privacy and data-handling guidance, and hosted operational considerations including Aspire-compatible resources, reliability of the existing validation/preview/reporting flow, and OpenTelemetry-based monitoring. The design should support a secure hosted model without introducing unnecessary stored data or blocking future account or entitlement management. Additional details and context are available in GitHub issue #10."

## Clarifications

### Session 2026-05-19

- Q: What hosted consent model should apply when required permissions are missing? → A: Allow standard user consent when permitted; otherwise direct the user to an administrator consent path.
- Q: What metadata may the hosted model retain? → A: Store only tenant-scoped operational metadata needed for configuration, consent state, and support diagnostics.
- Q: Which customer tenants should the hosted deployment admit on first use? → A: Allow any supported work or school tenant on first use, while keeping the design extensible for future approval controls.
- Q: How should hosted sign-in handle unsupported account types? → A: Reject unsupported account types at sign-in entry and show a clear explanation.
- Q: How should the app handle a returning user whose current sign-in resolves to a different tenant than previously stored metadata? → A: Treat the current sign-in as a new tenant context and do not reuse prior tenant metadata automatically.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Use Hosted Deployment Across Organisations (Priority: P1)

As a user in a customer organisation, I want to sign in to a shared hosted deployment with my work account and complete the existing import workflow safely so that my organisation can use the service without needing a dedicated deployment.

**Why this priority**: Safe multi-tenant access is the core product outcome of this feature and the main change that enables hosted adoption.

**Independent Test**: Can be fully tested by signing into one hosted deployment from users in at least two different customer organisations and verifying that each user can complete validation, preview, confirmation, and reporting without seeing another organisation's stored metadata or workflow context.

**Acceptance Scenarios**:

1. **Given** a hosted deployment configured for shared use, **When** a user signs in from any supported work or school customer organisation, **Then** the app authenticates the user, establishes the correct customer-tenant context, and allows the user to start the import workflow without requiring prior tenant approval.
2. **Given** a sign-in attempt uses an unsupported account type, **When** the hosted deployment evaluates the request, **Then** the app rejects entry to the workflow and shows a clear explanation that only supported work or school accounts can use the hosted service.
3. **Given** a returning user signs in and their current authentication resolves to a different customer tenant than previously stored metadata, **When** the app establishes the active tenant context, **Then** the app treats the current sign-in as a separate tenant context and does not automatically reuse or migrate the prior tenant's metadata.
4. **Given** two different customer organisations use the same hosted deployment, **When** each organisation stores operational metadata needed for the app to function, **Then** that metadata is associated with the correct customer tenant and is not exposed across tenant boundaries.
5. **Given** a signed-in hosted user starts validation, preview, confirmation, or execution, **When** the app performs planner-related actions on the user's behalf, **Then** those actions use the signed-in user's delegated authority rather than a shared customer-independent identity.

---

### User Story 2 - Resolve Consent and Privacy Expectations Clearly (Priority: P2)

As a customer administrator or end user, I want clear guidance when required permissions have not yet been granted and clear information about data handling so that my organisation can approve and operate the app confidently.

**Why this priority**: Multi-tenant sign-in is not supportable without a clear path for administrator consent and explicit privacy guidance for customer organisations.

**Independent Test**: Can be fully tested by attempting first-time use from a customer organisation without the required permissions and verifying that the app explains the problem, tells the user what their administrator must do, and provides documentation that explains accessed data and retained metadata.

**Acceptance Scenarios**:

1. **Given** a customer organisation has not granted required permissions, **When** a user attempts to use a hosted deployment, **Then** the app first allows the user to approve standard delegated consent when the tenant permits it and otherwise shows a clear explanation that administrator action is required together with a path the administrator can follow to grant access.
2. **Given** a user or administrator reviews the hosted deployment guidance, **When** they read the relevant documentation, **Then** they can understand what organisational data the app accesses, what limited metadata it may retain, and why that handling is necessary.
3. **Given** the app needs to persist operational metadata for a customer organisation, **When** that metadata is stored or displayed for support purposes, **Then** it is limited to tenant-scoped configuration, consent state, and support diagnostics needed to operate the service and remains attributable to the correct customer tenant.

---

### User Story 3 - Preserve Self-Hosted Operation and Hosted Readiness (Priority: P3)

As a self-hosting operator or service operator, I want the app to keep supporting single-tenant deployments while making hosted deployments observable and reliable so that existing users are not disrupted and hosted operations remain supportable.

**Why this priority**: The feature must not regress the current deployment model, and hosted adoption depends on maintaining the reliability and supportability of the existing import experience.

**Independent Test**: Can be fully tested by configuring one deployment for self-hosted single-tenant use and another for shared hosted use, then verifying that both can complete the current validation, preview, confirmation, and reporting flow while operational monitoring surfaces failures and performance issues without leaking sensitive tenant information.

**Acceptance Scenarios**:

1. **Given** a self-hosted operator configures the app for a single customer tenant, **When** users sign in and run the import workflow, **Then** the app continues to support the existing single-tenant behaviour without requiring hosted-only configuration.
2. **Given** either deployment mode is in use, **When** a user runs validation, preview, confirmation, or execution, **Then** the workflow preserves the current safeguards, sequencing, and reporting semantics.
3. **Given** hosted operations encounter sign-in, consent, workflow, or service failures, **When** operators review monitoring and diagnostics, **Then** they can identify the affected customer context and failure category without exposing secrets or unnecessary tenant-sensitive content.

### Edge Cases

- What happens when a user signs into the hosted deployment from a supported customer organisation that has not yet granted the required permissions?
- What happens when a returning hosted user belongs to a customer organisation with existing operational metadata but their current sign-in resolves to a different tenant context than expected?
- How does the system behave when a hosted deployment receives sign-in attempts from unsupported account types?
- What happens when a user is presented with a consent request but the tenant requires administrator approval, the request fails, or the user declines it?
- What happens when self-hosted configuration is incomplete or incorrectly set for single-tenant use?
- How does the workflow preserve preview and confirmation safeguards if hosted monitoring or metadata persistence is temporarily unavailable?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support a shared hosted deployment mode that allows users from multiple supported customer organisations to sign in safely through their work or school accounts.
- **FR-001a**: The hosted deployment MUST admit any supported work or school customer tenant on first use without requiring pre-approval, while keeping the design extensible for future approval, entitlement, or subscription controls.
- **FR-001b**: The hosted deployment MUST reject unsupported account types before they enter the hosted workflow and MUST show clear user-facing guidance that only supported work or school accounts can use the hosted service.
- **FR-002**: The system MUST continue to support a self-hosted deployment mode in which an operator can configure access for a single customer tenant without adopting hosted-only behaviour.
- **FR-003**: The system MUST determine and retain the active customer-tenant context for each signed-in session so that authentication, authorisation, stored metadata, and user guidance remain tenant-aware.
- **FR-003a**: When a returning user's current sign-in resolves to a different tenant than previously stored metadata, the system MUST treat the current sign-in as a separate tenant context and MUST NOT automatically reuse, reassign, or migrate the prior tenant's metadata.
- **FR-004**: The system MUST treat customer-tenant identity as the boundary for any stored application metadata and MUST prevent one customer tenant from viewing or affecting another tenant's metadata.
- **FR-005**: The system MUST use the signed-in user's delegated authority for planner-related actions performed on that user's behalf.
- **FR-006**: The system MUST allow a hosted user to complete standard delegated consent when the customer tenant permits it and MUST detect when the required permissions still have not been granted.
- **FR-007**: The system MUST present a clear user-facing explanation and administrator consent path instead of an unhandled failure whenever hosted access requires administrator approval, consent fails, or the user declines the request.
- **FR-008**: The system MUST document the setup, configuration, and operating expectations for both hosted and self-hosted deployment modes, including how they differ.
- **FR-009**: The system MUST document what customer data is accessed, what operational metadata may be retained, why that handling is necessary, and the privacy expectations for each deployment mode.
- **FR-010**: The system MUST preserve the current validation, preview, confirmation, and reporting workflow semantics in both deployment modes.
- **FR-011**: The system MUST provide operational diagnostics and monitoring for hosted sign-in, consent, workflow, and service failures in a way that supports tenant-aware troubleshooting without exposing secrets or unnecessary tenant-sensitive values.
- **FR-012**: The system MUST limit hosted stored metadata to tenant-scoped configuration, consent state, and support diagnostics needed for service reliability and supportability.
- **FR-012a**: The system MUST NOT retain per-user usage history, import-content history, or preview and reporting history beyond the active operational need of the current workflow unless a future feature explicitly adds that scope.
- **FR-013**: The system MUST keep the hosted design compatible with future account, entitlement, or subscription management rather than coupling tenant access to a fixed commercial model.
- **FR-014**: The system MUST define hosted resource needs in a way that remains compatible with the repository's current deployment orchestration approach and does not introduce unjustified backing services.
- **FR-015**: The system MUST provide focused automated regression coverage for hosted and self-hosted behaviour changes at the smallest practical level before relying on broader workflow validation.

### Quality and Non-Functional Requirements *(mandatory)*

- **NFR-001 Dependency Direction**: Solution MUST preserve allowed dependency direction (Web/Infrastructure -> Application -> Domain).
- **NFR-002 Inner-Layer Purity**: Domain/Application behaviour MUST remain technology-neutral and MUST NOT leak framework, transport, or presentation concerns.
- **NFR-003 Boundary Contracts**: Use-case input/output contract changes MUST be explicit, including where tenant context, consent state, and stored-metadata ownership are mapped across boundaries.
- **NFR-004 Framework Replaceability**: Features MUST treat framework, identity-provider, telemetry, and hosting-library choices as replaceable adapter concerns, not architectural invariants.
- **NFR-005 Compliance Evidence**: Feature delivery MUST define measurable architecture evidence, regression evidence, and tenant-isolation evidence for the affected behaviour.
- **NFR-006 Policy Alignment**: Feature MUST align with applicable repository policies covering runtime-mode verification, workflow consistency, UK English wording, privacy-safe diagnostics, and protection of security-sensitive values.
- **NFR-007 Workflow Consistency**: User-facing validation, preview, confirmation, and reporting semantics MUST remain consistent across hosted and self-hosted modes.
- **NFR-008 Operational Safety**: Hosted monitoring and diagnostics MUST preserve supportability without exposing secrets, raw credentials, or unnecessary tenant-sensitive content.
- **NFR-009 Documentation Language**: Contributor-facing and user-facing wording introduced by this feature MUST remain in UK English.

### Key Entities *(include if feature involves data)*

- **Deployment Mode**: The operating mode that determines whether the app is running as a shared hosted service for multiple customer organisations or as a self-hosted service for a single customer tenant.
- **Customer Tenant Context**: The tenant identity and related session context that define which organisation a signed-in user belongs to and which stored metadata boundary applies.
- **Signed-In User Session**: The authenticated session for a user, including the delegated authority used for planner-related actions and any consent state needed to guide the workflow.
- **Tenant-Scoped Operational Metadata**: The tenant-scoped configuration, consent state, and support diagnostics retained for reliability, support, or continuity, always associated with one customer tenant.
- **Consent Guidance State**: The information needed to explain missing permissions, identify whether administrator action is required, and direct the user to the appropriate next step.
- **Import Workflow Outcome**: The validation, preview, confirmation, execution, and reporting results associated with a user's current import attempt.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In acceptance testing, one hosted deployment supports successful sign-in and import workflow access for users from at least two distinct customer organisations without requiring separate deployments.
- **SC-001a**: In hosted first-use testing, a previously unseen supported work or school customer tenant can begin sign-in and reach the import workflow without an operator pre-approval step.
- **SC-001b**: In account-type validation testing, 100% of unsupported account-type sign-in attempts are rejected before workflow access and produce clear user guidance.
- **SC-002**: In regression testing, 100% of covered tenant-scoped metadata scenarios prevent cross-tenant visibility or cross-tenant mutation.
- **SC-002b**: In tenant-context mismatch testing, 100% of returning-user sign-ins that resolve to a different tenant context are treated as separate tenant contexts without automatic metadata reuse or reassignment.
- **SC-002a**: In specification and design review for this feature, no hosted persistence scope includes per-user usage history, import-content history, or retained preview and reporting history.
- **SC-003**: In consent-related acceptance testing, 100% of hosted permission scenarios either complete standard delegated consent successfully when permitted or produce a clear administrator next step rather than an unhandled failure.
- **SC-004**: In verification for this feature, the current validation, preview, confirmation, and reporting flow completes successfully in both hosted and self-hosted modes for representative import scenarios.
- **SC-005**: Documentation review confirms that operators can distinguish hosted and self-hosted setup paths, consent expectations, and privacy commitments without unresolved ambiguity.
- **SC-006**: Operational review confirms that hosted monitoring can identify sign-in failures, consent failures, and import workflow failures by customer context without exposing secrets or unnecessary tenant-sensitive content.
- **SC-007**: All automated tests added or updated for this feature pass, and no new architecture boundary violations are introduced.

## Assumptions

- Supported hosted users sign in with Microsoft 365 work or school accounts; personal consumer accounts are out of scope for this feature.
- Hosted access is open to supported work or school customer tenants by default, and future approval or entitlement controls remain out of scope for this feature.
- The current self-hosted single-tenant deployment remains the behavioural baseline and must keep working unless a change is explicitly documented.
- Billing, subscription charging, and entitlement enforcement are out of scope for this feature, but the design must not make those future capabilities harder to add.
- Customer-facing and contributor-facing documentation added for this feature will explain both deployment modes and remain in UK English.
- The app should avoid retaining customer data beyond tenant-scoped configuration, consent state, and support diagnostics needed for operational continuity and support.
- Hosted deployment changes should build on the repository's existing deployment and observability model rather than introducing a separate operational stack without clear justification.
