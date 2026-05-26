# Data Model: Simplify Graph Runtime Path

## Scope

This feature removes deployment-topology models from Application and replaces
them with smaller Web-owned configuration models plus clearer test-boundary
contracts. Domain remains unchanged.

## Entities

### TenantAuthorityConfiguration

- Purpose: Web-owned representation of how sign-in authority should be treated.
- Fields:
  - `TenantId`: raw configured `AzureAd:TenantId` value.
  - `AuthorityKind`: `SharedOrganisations` or `SpecificTenant`.
  - `AdminConsentUri`: optional administrator action URI derived from AzureAd
    settings when applicable.
  - `RequiredScopes`: delegated Microsoft Graph scopes used by the workflow.
- Validation rules:
  - `TenantId` is required and must not be blank.
  - `TenantId == "organizations"` maps to `SharedOrganisations`.
  - Any other supported non-placeholder tenant identifier maps to
    `SpecificTenant`.
  - Removed `DeploymentMode:*` keys must not coexist with this configuration.

### StorageConfiguration

- Purpose: Shared Web/Infrastructure configuration model for always-present Azure
  Storage dependencies.
- Fields:
  - `ConnectionString`: connection string supplied by Aspire resource wiring.
  - `TenantMetadataTable`: table name for tenant operational metadata.
  - `DataProtectionContainer`: blob container name for Data Protection keys.
  - `DataProtectionBlob`: blob name for Data Protection keys.
- Validation rules:
  - `ConnectionString`, `TenantMetadataTable`, `DataProtectionContainer`, and
    `DataProtectionBlob` are required.
  - Removed `HostedStorage:*` keys must trigger startup failure rather than being
    mapped silently.

### TenantContext

- Purpose: Repository-owned representation of the current authenticated tenant
  and user.
- Fields retained:
  - `TenantId`
  - `TenantKey`
  - `UserObjectId`
  - `AccountType`
  - `DisplayName`
- Removed field:
  - `DeploymentMode`
- Validation rules:
  - `TenantId`, `TenantKey`, and `UserObjectId` are required.
  - Unsupported account types never produce a valid context.
  - Single-tenant fallback to configured authority is a Web concern only.

### ConsentResolution

- Purpose: Repository-owned workflow outcome indicating whether the user can
  continue planner operations.
- Fields retained:
  - `Status`
  - `RequiredScopes`
  - `AdminConsentUri`
  - `MessageKey`
  - `DiagnosticCode`
- Validation rules:
  - Must not depend on deployment-mode flags.
  - `AdminConsentUri` is populated only when a user-facing administrator path is
    needed.
  - Provider-specific exception text stays outside this model.

### PlannerGatewayRegistration

- Purpose: Operational design invariant for Infrastructure DI.
- Shape:
  - `Implementation`: `GraphPlannerGateway`
  - `Lifetime`: scoped
- Validation rules:
  - No runtime configuration can swap this to another production
    implementation.
  - Tests use doubles at the `IPlannerGateway` boundary rather than runtime
    configuration.

### TenantMetadataStoreRegistration

- Purpose: Operational design invariant for Infrastructure DI.
- Shape:
  - `Implementation`: `TableTenantOperationalMetadataStore`
  - `Lifetime`: singleton or equivalent stable lifetime depending on SDK usage
- Validation rules:
  - No runtime configuration can swap this to an in-memory production
    implementation.
  - Tests use doubles at the `ITenantOperationalMetadataStore` boundary.

### AppHostResourceGraph

- Purpose: Describes the fixed orchestration topology for local and hosted runs.
- Nodes:
  - `storage`
  - `blobs`
  - `tables`
  - `web`
- Relationships:
  - `storage` owns `blobs` and `tables`.
  - `web` references `blobs` and `tables`.
- Validation rules:
  - Resource declarations are unconditional.
  - No environment-variable reads or conditional composition are allowed in the
    AppHost.

## Relationships Overview

- `TenantAuthorityConfiguration` drives Web auth-guard decisions, admin consent
  URI construction, and tenant fallback behaviour.
- `StorageConfiguration` is consumed by Infrastructure and Data Protection setup.
- `TenantContext` and `ConsentResolution` remain the inner-layer contracts used by
  workflow services.
- `PlannerGatewayRegistration` and `TenantMetadataStoreRegistration` express the
  single supported runtime path and replace the deleted runtime mode matrix.
- `AppHostResourceGraph` provides the connection details consumed by
  `StorageConfiguration` during local and hosted orchestration.

## State Transitions

### Startup configuration

1. AppHost starts `storage`, `blobs`, `tables`, and `web`.
2. Web configuration loads AzureAd values from appsettings and user secrets.
3. Startup validation rejects missing required values or removed obsolete keys.
4. If validation passes, Data Protection and tenant metadata storage are wired
   against Azure Storage.

### Authentication and tenant resolution

1. `TenantAuthorityConfiguration` classifies the configured tenant authority.
2. Sign-in uses Microsoft.Identity.Web against that authority.
3. Web auth guards keep unsupported-account, admin-consent, and tenant-mismatch
   protections for shared-organisations authority.
4. `TenantContext` is created from claims or, for specific-tenant authority only,
   from the configured authority when the tenant claim is absent.

### Planner and metadata usage

1. Workflow services request planner access through `IPlannerGateway`.
2. Infrastructure always resolves `GraphPlannerGateway`.
3. Consent resolution uses tenant metadata only through
   `ITenantOperationalMetadataStore`.
4. Infrastructure always resolves `TableTenantOperationalMetadataStore`.

## Mapping Boundaries

- Web owns `TenantAuthorityConfiguration`, startup validation, auth events, and
  user-facing configuration failures.
- Application owns `TenantContext`, `ConsentResolution`, and workflow decisions.
- Infrastructure owns Azure Storage and Graph adapter registration.
- AppHost owns orchestration only and does not own AzureAd application settings.
