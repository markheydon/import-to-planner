# Quickstart: Simplify Graph Runtime Path Verification

## Purpose

Use this guide after implementation to verify that the app now runs through one
supported Graph/storage path, uses the new conventional AppHost, and fails
gracefully for missing or obsolete configuration.

## Prerequisites

- .NET 10 SDK installed.
- Aspire CLI installed.
- Repository dependencies restored.
- Web project user secrets configured for:
  - `AzureAd:TenantId`
  - `AzureAd:ClientId`
  - certificate path and password settings used by Microsoft.Identity.Web
- No removed `PlannerGateway:*`, `HostedStorage:*`, or `DeploymentMode:*` keys
  present in active configuration.

## Verification Checkpoints

| Checkpoint | Command or action | Evidence to capture |
| --- | --- | --- |
| Automated regression baseline | `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj` and `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj` | Passing output for startup validation, tenant handling, Data Protection, and workflow slices |
| Architecture cleanup evidence | `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj --filter FullyQualifiedName~ArchitectureComplianceTests` | Passing evidence that Application/Domain no longer reference deployment-topology models |
| AppHost orchestration | `aspire run` | AppHost starts `storage`, `blobs`, `tables`, and `web` with no manual Azurite setup |
| Friendly startup failure for missing tenant ID | Start the app with blank or missing `AzureAd:TenantId` | Human-friendly configuration failure shown before sign-in is available |
| Friendly startup failure for removed keys | Reintroduce one removed key such as `DeploymentMode:Mode` temporarily and start the app | Human-friendly configuration failure explaining the obsolete key |
| Shared-organisations auth guard regression | Run with `AzureAd:TenantId=organizations` | Unsupported-account rejection, admin-consent mapping, and tenant-mismatch handling still behave correctly |
| Specific-tenant auth regression | Run with a concrete tenant ID | Sign-in remains single-tenant and the import workflow remains unchanged |
| Storage-backed persistence | Restart after local run | Data Protection keys and tenant metadata remain wired through blob/table storage |

## Recommended Verification Order

### 1. Run focused automated tests first

```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj
```

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj
```

Expected evidence:
- Tests no longer rely on `InMemoryPlannerGateway` or
  `InMemoryTenantOperationalMetadataStore` as production implementations.
- Startup-validation tests cover missing `AzureAd:TenantId` and removed key
  detection.
- Data Protection tests cover always-on blob persistence with `Storage:*`
  settings.
- Workflow tests still protect validation, preview, confirmation, and execution
  behaviour.

### 2. Verify architecture-boundary cleanup

Suggested checks:

```bash
rg -n "DeploymentModeConfiguration|enum DeploymentMode|PlannerGateway:UseGraph|HostedStorage:" src tests
```

```bash
rg -n "Aspire|Azure\.Storage|Microsoft\.Identity|Microsoft\.Graph" src/ImportToPlanner.Application src/ImportToPlanner.Domain
```

Expected evidence:
- Deleted runtime mode concepts no longer appear in maintained source.
- Application and Domain do not take new hosting or vendor dependencies.

### 3. Verify the target local developer flow

Configure the Web project secrets, then run:

```bash
aspire run
```

Expected evidence:
1. Azurite starts automatically through the AppHost.
2. The Web app starts without manual environment-variable setup.
3. AzureAd settings come from the Web project only.
4. The app authenticates against a real Microsoft 365 tenant.

### 4. Verify graceful fail-fast startup behaviour

Check two negative cases separately:

1. Remove or blank `AzureAd:TenantId`.
2. Reintroduce one removed key such as `HostedStorage:Enabled`.

Expected evidence:
1. Startup stops before sign-in is available.
2. The failure is friendly, actionable, and operator-readable.
3. No raw framework exception page is shown.

### 5. Verify retained auth guard behaviour

For `AzureAd:TenantId=organizations`, verify:

1. Unsupported personal accounts are rejected before entering the workflow.
2. Admin-consent-required failures still redirect to clear guidance.
3. Tenant-context mismatch handling still avoids reusing another tenant's
   metadata automatically.

For a specific tenant ID, verify:

1. Sign-in remains constrained to that tenant.
2. The import workflow semantics remain unchanged.
3. No hosted-only messaging appears unexpectedly.

### 6. Verify storage-backed behaviour

During and after an `aspire run` session, confirm:

1. Blob-backed Data Protection configuration initialises successfully.
2. The Data Protection container bootstrapper creates the target container when
   needed.
3. Table-backed tenant metadata access is wired through the storage reference.
4. Restarting the app does not break the configured persistence path.

## Review Checklist

- The new AppHost project replaces the root script and is present in the solution.
- The AppHost resource graph is unconditional and free of environment-variable
  composition.
- The Web project owns AzureAd settings through appsettings and user secrets.
- Removed configuration keys fail fast and gracefully if reintroduced.
- The app uses one supported planner gateway and one supported metadata store at
  runtime.
- Data Protection always persists keys to blob storage.
- Existing auth-guard behaviour remains intact.
- Existing validation, preview, confirmation, and reporting behaviour remains
  intact.
- Documentation remains in UK English and describes the simplified local setup.

## Deployment Readiness Notes

- Keep Key Vault integration out of this feature.
- Keep the AppHost topology to one Web project and one Azure Storage resource
  graph.
- Ensure contributor documentation is updated to describe the new local setup and
  the removal of all mode flags.
