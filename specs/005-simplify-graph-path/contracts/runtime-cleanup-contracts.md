# Runtime Cleanup Contracts

## Purpose

This feature removes the runtime mode matrix and replaces it with explicit
contracts for AppHost orchestration, Web-owned configuration, startup
validation, and abstraction-boundary test doubles.

## Contract Overview

### AppHost resource graph contract

- Owner: Aspire AppHost
- Purpose: Declare all runtime resources needed by the Web project with no
  conditional composition.

Expected shape:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

builder.AddProject<Projects.ImportToPlanner_Web>("web")
    .WithReference(blobs)
    .WithReference(tables);
```

Rules:
- No `if` statements.
- No environment-variable reads.
- No conditional `WithEnvironment` calls.
- No AzureAd configuration forwarding.

### Web AzureAd configuration contract

- Owner: Web project configuration
- Purpose: Supply Microsoft.Identity.Web with the authority and credentials it
  needs without AppHost mediation.

Expected shape:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "organizations | <tenant-guid-or-domain>",
    "ClientId": "<client-id>",
    "CallbackPath": "/signin-oidc",
    "ClientCertificates": [
      {
        "SourceType": "Path",
        "CertificateDiskPath": "<path>",
        "CertificatePassword": "<secret>"
      }
    ]
  }
}
```

Rules:
- `TenantId` is required.
- `TenantId == "organizations"` enables the retained shared-organisations guard
  path.
- Any other supported tenant value enables the single-tenant path.
- AzureAd values come from Web appsettings and user secrets, not AppHost
  forwarding.

### Storage configuration contract

- Owner: Web and Infrastructure configuration boundary
- Purpose: Represent the always-on Azure Storage settings used by Data Protection
  and tenant metadata persistence.

Expected shape:

```json
{
  "Storage": {
    "TenantMetadataTable": "TenantOperationalMetadata",
    "DataProtectionContainer": "dataprotection",
    "DataProtectionBlob": "keys.xml"
  }
}
```

Rules:
- The connection string is supplied through Aspire resource wiring.
- `HostedStorage:*` keys are invalid and must trigger startup failure.
- Container and blob names remain Web-owned configuration values.

### Startup validation contract

- Owner: Web startup
- Purpose: Fail before sign-in becomes available when required configuration is
  missing or removed configuration is still present.

Validation cases:
- Missing or blank `AzureAd:TenantId`.
- Presence of removed `PlannerGateway:*` keys.
- Presence of removed `HostedStorage:*` keys.
- Presence of removed `DeploymentMode:*` keys.
- Missing required `Storage:*` values or missing storage connection wiring.

Rules:
- Failures must be human-friendly, actionable, and suitable for operators.
- Raw framework exception pages are not acceptable as the user experience.

### Planner and tenant metadata boundary contracts

- Owner: Application abstractions, implemented by Infrastructure and doubled in tests
- Purpose: Keep production runtime fixed while preserving isolated tests.

Required abstractions:

```csharp
public interface IPlannerGateway { ... }
public interface ITenantOperationalMetadataStore { ... }
```

Rules:
- Production DI always resolves `GraphPlannerGateway`.
- Production DI always resolves `TableTenantOperationalMetadataStore`.
- Tests must supply explicit doubles at these abstractions rather than runtime
  flags or alternative production registrations.

### Consent and tenant-context boundary contract

- Owner: Web to Application boundary
- Purpose: Preserve working auth safeguards without leaking deployment topology
  into Application.

Expected responsibilities:
- Web classifies the configured authority.
- Web handles unsupported-account rejection and auth-event error mapping.
- Web constructs or delegates consent/admin-consent information.
- Application consumes `TenantContext` and `ConsentResolution` only.

Rules:
- `DeploymentMode` and `DeploymentModeConfiguration` are deleted.
- `TenantContext` must not carry a deployment-mode field.
- User-facing wording remains in Web/presenters.

## Compatibility Notes

- Local developer flow becomes: clone repository, set Web user secrets, run
  `aspire run`, authenticate against a real Microsoft 365 tenant.
- There is no supported legacy or upgrade path for removed runtime flags, so
  obsolete keys are treated as configuration errors.
- Key Vault integration is explicitly out of scope for this feature.
