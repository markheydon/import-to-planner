# Data Model: Hosted Multi-Tenant Support

## Scope

This feature introduces deployment-mode, tenant-context, consent, and hosted
operational-metadata models. The design keeps Domain and Application technology-
neutral while leaving Microsoft identity, Azure hosting, and storage details in
outer adapters.

## Entities

### DeploymentModeConfiguration

- Purpose: Defines whether the app is running in self-hosted single-tenant mode or
  hosted shared multi-tenant mode.
- Fields:
  - `Mode`: `SelfHostedSingleTenant` or `HostedSharedMultiTenant`.
  - `AuthorityTenant`: concrete tenant identifier for self-hosted mode or the
    hosted shared authority identifier for work/school-only sign-in.
  - `UseGraphGateway`: whether planner operations use Microsoft Graph or the
    in-memory gateway.
  - `HostedStorageEnabled`: whether hosted metadata and data-protection backing
    services are configured.
  - `InitialReplicaPolicy`: hosted rollout setting that captures the low-cost
    single-active-replica baseline.
- Validation rules:
  - Self-hosted mode requires a concrete tenant identifier.
  - Hosted shared mode must reject authorities that admit unsupported personal
    accounts.
  - Hosted storage settings must not be required for self-hosted mode.

### TenantContext

- Purpose: Repository-owned representation of the currently authenticated customer
  tenant.
- Fields:
  - `TenantId`: authoritative tenant identifier from the user's sign-in context.
  - `TenantKey`: stable support-safe correlation value derived from the tenant ID
    for diagnostics and telemetry.
  - `DisplayName`: optional tenant display label when available without extra
    sensitive retention.
  - `AccountType`: supported work/school account classification.
  - `UserObjectId`: current signed-in user's directory object identifier.
  - `DeploymentMode`: the active deployment mode for this session.
- Validation rules:
  - `TenantId`, `TenantKey`, and `DeploymentMode` are required.
  - Unsupported account types must never produce a valid `TenantContext`.
  - A tenant mismatch across sign-ins creates a new `TenantContext` rather than
    mutating prior metadata ownership.

### HostedTenantOperationalMetadata

- Purpose: Minimal persisted hosted metadata associated with one customer tenant.
- Fields:
  - `TenantId`
  - `ConsentState`
  - `ConfigurationState`: optional tenant-scoped configuration needed to operate
    the hosted service.
  - `LastConsentCheckUtc`
  - `LastSupportDiagnosticCode`: optional stable diagnostic code for support.
  - `LastUpdatedUtc`
- Validation rules:
  - Records are partitioned by `TenantId`.
  - No per-user usage history, CSV content, preview payloads, or execution reports
    may be stored on this entity.
  - Diagnostic data must remain support-oriented rather than behavioural history.

### ConsentGuidanceState

- Purpose: Structured outcome describing whether the current tenant can continue
  with delegated Graph access and what the user should do next.
- Fields:
  - `Status`: `Unknown`, `UserConsentAvailable`, `Granted`, `AdminConsentRequired`,
    `Declined`, or `Unavailable`.
  - `RequiredScopes`: the delegated Graph scopes relevant to the current workflow.
  - `AdminConsentUri`: optional administrator action link.
  - `UserMessageKey`: presenter-facing key for UK-English wording selection.
  - `DiagnosticCode`: stable support/debug code.
- Validation rules:
  - `AdminConsentUri` is required only when `Status` is `AdminConsentRequired`.
  - The model contains no framework-specific exception types or raw provider text.

### DelegatedGraphSession

- Purpose: Describes the signed-in user's active delegated access context for
  planner operations.
- Fields:
  - `TenantId`
  - `UserObjectId`
  - `Scopes`
  - `AuthenticatedAtUtc`
  - `TokenCacheMode`: `InMemory` initially, with a future extension point for a
    distributed cache.
  - `RequiresReauthentication`: whether the current process restart or consent
    change means the user must sign in again.
- Validation rules:
  - Planner operations may proceed only when the session is bound to the current
    `TenantContext` and required scopes are satisfied.
  - This model is session-scoped and is not retained as operational history.

### ImportWorkflowContext

- Purpose: Extends the existing workflow state with deployment-mode and tenant-aware
  safeguards while preserving the current validation, preview, confirmation, and
  execution semantics.
- Fields:
  - Existing import planning/execution state.
  - `DeploymentMode`
  - `TenantContext`
  - `ConsentGuidanceState`
  - `HostedModeWarningState`: unsupported account, missing consent, or hosted
    configuration guidance.
- Validation rules:
  - Workflow entry is blocked when no supported `TenantContext` exists.
  - Confirmation and execution remain unavailable when consent state is not ready.
  - Returning to earlier steps continues to invalidate stale preview state exactly
    as the current workflow requires.

## Relationships Overview

- `DeploymentModeConfiguration` determines the authentication authority, storage
  requirements, and AppHost resource wiring.
- `TenantContext` is derived from the authenticated session and scopes access to
  `HostedTenantOperationalMetadata`.
- `ConsentGuidanceState` is produced from token-acquisition or hosted metadata
  checks and shapes the workflow's next step.
- `DelegatedGraphSession` must align with the current `TenantContext` before the
  planner gateway is used.
- `ImportWorkflowContext` consumes the other models to preserve the existing
  import workflow semantics across both deployment modes.

## State Transitions

### Deployment mode

1. App starts with configuration resolved from local Aspire or hosted deployment.
2. Mode is classified as self-hosted or hosted shared.
3. Required support resources are enabled only for hosted shared mode.

### Tenant context and consent flow

1. User attempts sign-in.
2. Authentication pipeline validates account type and authority.
3. A valid `TenantContext` is created from claims, or unsupported accounts are
   rejected before workflow entry.
4. Consent is evaluated when the workflow needs delegated Graph access.
5. `ConsentGuidanceState` becomes `Granted`, `AdminConsentRequired`, `Declined`,
   or another explicit outcome.
6. Hosted metadata is updated only with the current tenant's minimal operational
   state.

### Workflow progression

1. User enters the import workflow under a valid tenant context.
2. Existing validation and preview steps run unchanged.
3. Hosted consent or tenant-safety issues pause the workflow with explicit
   presenter guidance.
4. Execution proceeds only after the preview is current and the delegated session
   is valid for the active tenant.

## Mapping Boundaries

- Web owns sign-in validation, unsupported-account rejection, deployment-mode
  configuration mapping, and presenter wording.
- Application owns tenant/consent contracts and workflow decisions that depend on
  those contracts.
- Infrastructure owns Azure Table/Blob persistence, Graph token usage, and any
  provider-specific exception mapping.
- Domain remains free of identity-provider, hosting, storage, and UI concerns.
