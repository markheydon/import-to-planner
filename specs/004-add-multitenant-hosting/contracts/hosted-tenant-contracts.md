# Hosted Tenant Contracts

## Purpose

This feature introduces explicit contracts for deployment mode, current tenant
context, hosted tenant metadata, and consent resolution so that Web and
Infrastructure adapters can support multi-tenant hosting without leaking identity,
Azure, or UI concerns into inner layers.

## Contract Overview

### Deployment mode contract

- Owner: Web/AppHost configuration boundary
- Purpose: Resolve whether the app is running as self-hosted single-tenant or
  hosted shared multi-tenant.

Expected shape:

```json
{
  "DeploymentMode": {
    "Mode": "SelfHostedSingleTenant | HostedSharedMultiTenant",
    "AllowHostedMultiTenant": true,
    "InitialHostedReplicaPolicy": "SingleActiveReplica"
  },
  "AzureAd": {
    "TenantId": "<tenant-guid-or-organizations>",
    "ClientId": "<client-id>",
    "Instance": "https://login.microsoftonline.com/",
    "CallbackPath": "/signin-oidc"
  }
}
```

Notes:
- Self-hosted mode uses one concrete tenant identifier.
- Hosted mode uses a work-or-school-only shared authority.
- The contract does not expose provider-specific event-handler details to
  Application.

### Current tenant context contract

- Owner: Application boundary
- Purpose: Give Application services access to the active tenant context without
  taking a dependency on `HttpContext`, claims APIs, or Microsoft identity types.

Proposed interface shape:

```csharp
public interface ICurrentTenantContextAccessor
{
    TenantContext GetRequiredContext();
}
```

Response shape:

```csharp
public sealed record TenantContext(
    string TenantId,
    string TenantKey,
    string UserObjectId,
    DeploymentMode Mode,
    SupportedAccountType AccountType,
    string? DisplayName);
```

Notes:
- Web owns claims extraction and unsupported-account rejection.
- Application consumes only the repository-owned `TenantContext` record.

### Tenant operational metadata store contract

- Owner: Application boundary, implemented by Infrastructure
- Purpose: Read and update the minimum hosted metadata needed for tenant-aware
  operation and support.

Proposed interface shape:

```csharp
public interface ITenantOperationalMetadataStore
{
    Task<TenantOperationalMetadata?> GetAsync(
        string tenantId,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        TenantOperationalMetadata metadata,
        CancellationToken cancellationToken);
}
```

Entity expectations:
- Partition by `TenantId`.
- Store only tenant-scoped configuration, consent state, and support diagnostics.
- Exclude per-user activity history, CSV content, preview payloads, and execution
  reports.

### Consent resolution contract

- Owner: Application boundary for structured outcomes; Web presenters own wording.
- Purpose: Describe whether the user can proceed, whether administrator action is
  required, and what the next step is.

Proposed response shape:

```csharp
public sealed record ConsentResolution(
    ConsentResolutionStatus Status,
    IReadOnlyList<string> RequiredScopes,
    Uri? AdminConsentUri,
    string MessageKey,
    string? DiagnosticCode);
```

Status values:
- `Granted`
- `UserConsentAvailable`
- `AdminConsentRequired`
- `Declined`
- `Unavailable`

Notes:
- Infrastructure/Web may map provider exceptions into this contract.
- Application and presenters must not depend on raw MSAL or Graph exception text.

### Telemetry enrichment contract

- Owner: Web/Infrastructure/ServiceDefaults boundary
- Purpose: Standardise hosted observability dimensions without leaking secrets.

Expected attributes:
- `deployment.mode`
- `tenant.key`
- `consent.status`
- `planner.gateway.mode`
- `failure.category`

Must not include:
- Access tokens
- Refresh tokens
- Certificate material
- Raw CSV/import payloads
- Unnecessary tenant-sensitive values shown directly to end users

## Mapping Responsibilities

### Web

Owns:
- Deployment-mode configuration selection.
- Claims extraction and unsupported-account rejection.
- Sign-in and consent flow entry points.
- User-facing UK-English wording.

### Application

Owns:
- Repository-owned tenant, metadata, and consent contracts.
- Workflow decisions that depend on those contracts.

### Infrastructure

Owns:
- Azure Table and Blob adapters.
- Graph token usage and provider-specific exception mapping.
- Storage-backed metadata persistence and data-protection support.

## Compatibility Notes

- Existing self-hosted configuration remains valid and continues to be the default
  behavioural baseline.
- Hosted mode adds explicit contracts rather than implicit branches in existing
  use-case code.
- If a later feature approves multiple active hosted replicas, the same contracts
  can support a distributed token-cache implementation without reworking
  Application or Domain.
