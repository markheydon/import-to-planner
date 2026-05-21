# Aspire staging deployment (GitHub Actions)

This repository now deploys to Azure automatically through Aspire when `CI` succeeds on `main`.

Workflow file:

- `.github/workflows/cd-staging.yml`

## Trigger behaviour

- Automatic: runs after `CI` completes successfully for pushes to `main`.
- Manual: runs via `workflow_dispatch`.
- Environment target: `Staging` (GitHub Environment).

## AppHost deployment configuration

`apphost.cs` now publishes the `web` resource as an Azure Container App with low-cost scale settings:

- `minReplicas = 0`
- `maxReplicas = 1`

This keeps non-active usage costs down while still allowing one active replica when traffic exists.

## Required GitHub Environment configuration (`Staging`)

Configure these in **Settings → Environments → Staging**.

### Required secrets

| Name | Purpose |
| --- | --- |
| `AZURE_CLIENT_ID` | Federated identity application (OIDC) client ID used by `azure/login`. |
| `AZURE_TENANT_ID` | Azure tenant ID for login. |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID for deployment. |
| `HOSTED_STORAGE_CONNECTION_STRING` | Hosted storage account connection string used by `HostedStorage:ConnectionString`. |

### Required variables

| Name | Purpose |
| --- | --- |
| `AZURE_LOCATION` | Azure region used for deployment/provisioning. |

### Optional secrets

| Name | Purpose |
| --- | --- |
| `DEPLOYMENT_AUTHORITY_TENANT` | Overrides `DeploymentMode:AuthorityTenant` when needed. |
| `AZURE_AD_TENANT_ID` | Overrides `AzureAd:TenantId` when needed. |

### Optional variables

| Name | Default | Purpose |
| --- | --- | --- |
| `AZURE_ENV_NAME` | _(unset)_ | Existing `azd` environment name to select before deploy. |
| `HOSTED_STORAGE_TENANT_METADATA_TABLE` | `TenantOperationalMetadata` | Overrides hosted table name. |
| `HOSTED_STORAGE_DATA_PROTECTION_CONTAINER` | `dataprotection` | Overrides Data Protection blob container name. |
| `HOSTED_STORAGE_DATA_PROTECTION_BLOB` | `keys.xml` | Overrides Data Protection blob name. |

## Runtime values injected by the workflow

The deploy workflow sets these for `aspire deploy`:

- `DeploymentMode__Mode=HostedSharedMultiTenant`
- `PlannerGateway__UseGraph=true`
- `HostedStorage__Enabled=true`
- `HostedStorage__ConnectionString` from `HOSTED_STORAGE_CONNECTION_STRING`

These align staging with the hosted Graph-backed path and durable hosted storage.

## Operational notes

- If `AZURE_LOCATION` or `HOSTED_STORAGE_CONNECTION_STRING` is missing, the workflow fails early by design.
- Keep `Staging` environment protection rules enabled if you require approvals or branch restrictions.
- Manual runs reuse the same workflow and environment configuration as automatic runs.
