# ACA Staging Runbook

This runbook describes the minimum setup to deploy the Aspire AppHost to the `staging` GitHub Environment and validate the deployed app safely.

If you are new to Aspire deployment, follow the sections in order. The first pass is intentionally practical and low-friction.

## Scope

- Deployment target: Azure Container Apps (ACA)
- Deployment command: `aspire deploy --environment Staging`
- CI/CD entrypoint: `.github/workflows/deploy-staging.yml`
- Current app shape: one `web` compute resource in one ACA environment (`aca-env`)

## New contributor quick path

If you forked this repository and want a straightforward path:

1. Run one local deploy attempt first:
   - `az login`
   - `aspire deploy --environment Staging`
2. Let Aspire prompt you for missing Azure deployment values.
3. Reuse the saved values for GitHub `staging` environment variables.

Important: the three `aspire secret get` commands only cover Azure deployment target values (subscription, location, resource group). They do not return OIDC app-registration values such as `AZURE_CLIENT_ID`.

The shared Azure values you need for CI are:

- `Azure__SubscriptionId`
- `Azure__Location`
- `Azure__ResourceGroup`

You can inspect what Aspire saved locally with:

- `aspire secret get "Azure:SubscriptionId"`
- `aspire secret get "Azure:Location"`
- `aspire secret get "Azure:ResourceGroup"`
- `aspire secret path` (shows where the local secret store lives)

Use those outputs to populate GitHub environment values instead of retyping from memory.

If `aspire deploy` exits with `An Azure subscription id is required`, seed the shared settings directly:

```bash
aspire secret set "Azure:SubscriptionId" "<subscription-id>"
aspire secret set "Azure:Location" "<azure-region>"
aspire secret set "Azure:ResourceGroup" "<resource-group-name>"
```

You can then verify what is stored:

```bash
aspire secret get "Azure:SubscriptionId"
aspire secret get "Azure:Location"
aspire secret get "Azure:ResourceGroup"
```

Before running a manual hosted deploy, prepare runtime certificate inputs from your own `.pfx` file:

1. Export or obtain the Graph client certificate `.pfx` and its password.
2. Convert the `.pfx` file to base64 (single line, no wrapping):

Linux/macOS:

```bash
base64 -w 0 /path/to/graph-client.pfx
```

PowerShell (Windows):

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\path\to\graph-client.pfx"))
```

3. Export runtime parameters for deploy (replace placeholder values):

```bash
export Parameters__azureAdTenantId="<tenant-id>"
export Parameters__azureAdClientId="<client-id>"
export Parameters__graphClientCertificatePassword="<certificate-password>"
export Parameters__graphClientCertificateBase64="<single-line-base64-pfx>"
```

4. Run deploy:

```bash
aspire deploy --environment Staging
```

Important: this hosted flow does not depend on your local certificate file path. The app recreates a runtime certificate file from base64 during startup.

## GitHub staging environment mapping (what comes from where)

Use this mapping when creating values under GitHub Settings > Environments > `staging`.

| GitHub key | Type in GitHub | Source | How to get it |
| --- | --- | --- | --- |
| `AZURE_CLIENT_ID` | Secret | Microsoft Entra app registration used by GitHub OIDC | Entra app Overview page -> Application (client) ID |
| `AZURE_TENANT_ID` | Secret | Microsoft Entra tenant that owns the OIDC app registration | Entra tenant Overview -> Tenant ID (or `az account show --query tenantId -o tsv`) |
| `AZURE_SUBSCRIPTION_ID` | Secret | Aspire deployment target settings | `aspire secret get "Azure:SubscriptionId"` |
| `AZURE_LOCATION` | Variable | Aspire deployment target settings | `aspire secret get "Azure:Location"` |
| `AZURE_RESOURCE_GROUP` | Variable | Aspire deployment target settings | `aspire secret get "Azure:ResourceGroup"` |
| `GRAPH_CLIENT_CERTIFICATE_PASSWORD` | Secret | Password protecting the Graph client certificate `.pfx` | Certificate export/process owner |
| `GRAPH_CLIENT_CERTIFICATE_BASE64` | Secret | Base64-encoded `.pfx` bytes for hosted runtime materialisation | `base64 -w 0 <path-to-pfx>` on Linux/macOS or equivalent on Windows |

Why this split exists:

- OIDC identity values (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`) come from identity setup.
- Deployment target values (`subscription`, `location`, `resource group`) come from Aspire deploy prompts and local Aspire secret storage.
- Hosted web runtime values reuse `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` for the app runtime parameters in this single-tenant baseline, plus `GRAPH_CLIENT_CERTIFICATE_*` for certificate handoff.

## 1) One-time Azure and GitHub setup

### Azure prerequisites

- Create or choose a subscription for non-production staging usage.
- Create a Microsoft Entra application/service principal for GitHub OIDC.
- Configure a federated credential on that app for this repository and branch/workflow usage.
- Grant deployment permissions at the staging resource-group scope (or a tightly scoped equivalent).

### GitHub Environment prerequisites

Create a `staging` environment in repository settings and add these secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Add these environment variables to the same `staging` environment:

- `AZURE_LOCATION`
- `AZURE_RESOURCE_GROUP`

Add these additional environment secrets:

- `GRAPH_CLIENT_CERTIFICATE_PASSWORD`
- `GRAPH_CLIENT_CERTIFICATE_BASE64`

If you are unsure about the source of each value, use the mapping table above before saving.

Recommended protections:

- Restrict secret access to the `staging` environment only.
- Add required reviewers only if you want manual gatekeeping for staging.

## 2) Staging application configuration checklist

The deployed web app must receive valid hosted configuration values. Keep these values out of source control.

Required values:

- `AzureAd:TenantId`
- `AzureAd:ClientId`
- `AzureAd:Instance`
- `AzureAd:CallbackPath`
- `AzureAd:ClientCertificates:0:SourceType`
- `AzureAd:ClientCertificates:0:CertificateDiskPath`
- `AzureAd:ClientCertificates:0:CertificatePassword`
- `AzureAd:ClientCertificates:0:CertificateBase64`
- `Storage:TenantMetadataTable`
- `Storage:DataProtectionContainer`
- `Storage:DataProtectionBlob`
- Optional telemetry: `OTEL_EXPORTER_OTLP_ENDPOINT`

Notes:

- The app startup validator rejects missing required keys and placeholder defaults.
- The AppHost now supplies AzureAd values via Aspire deploy parameters.
- The app can materialise `AzureAd:ClientCertificates:0:CertificateBase64` into `AzureAd:ClientCertificates:0:CertificateDiskPath` at startup.

### About the three shared Azure values in CI

The staging workflow passes three `aspire deploy` values explicitly:

- `Azure__SubscriptionId` from `AZURE_SUBSCRIPTION_ID`
- `Azure__Location` from `AZURE_LOCATION`
- `Azure__ResourceGroup` from `AZURE_RESOURCE_GROUP`

If these are missing, GitHub deployment can authenticate to Azure successfully and still fail during `aspire deploy`.

### Hosted runtime parameters passed by CI

The staging workflow also passes Aspire parameters for hosted web runtime settings:

- `Parameters__azureAdTenantId` from `AZURE_TENANT_ID`
- `Parameters__azureAdClientId` from `AZURE_CLIENT_ID`
- `Parameters__graphClientCertificatePassword` from `GRAPH_CLIENT_CERTIFICATE_PASSWORD`
- `Parameters__graphClientCertificateBase64` from `GRAPH_CLIENT_CERTIFICATE_BASE64`

If these are missing, revisions may provision but fail at runtime with startup configuration errors.

## 3) Certificate handling for staging

The hosted path uses base64 certificate materialisation during startup:

1. Store `GRAPH_CLIENT_CERTIFICATE_BASE64` and `GRAPH_CLIENT_CERTIFICATE_PASSWORD` as GitHub Environment secrets.
2. The AppHost forwards those values into web app configuration.
3. At startup, the web app decodes `AzureAd:ClientCertificates:0:CertificateBase64` and writes `/tmp/import-to-planner-graph-client.pfx` with user-only read/write permissions.
4. Microsoft.Identity.Web reads that file path using `AzureAd:ClientCertificates:0:CertificateDiskPath`.

For contributors:

- Do not copy another developer's local certificate path.
- Use your own `.pfx` plus password and generate your own base64 payload.
- The container path is fixed by runtime configuration and is created at startup from base64.

Production hardening (later):

- Move from startup file materialisation to a managed certificate source (for example, Key Vault integration) when ready.

## 4) First deployment procedure

1. Confirm CI is passing on `main`.
2. Run the `Deploy Staging` workflow using `workflow_dispatch`.
3. Verify the Azure login step succeeded using OIDC.
4. Confirm the shared Azure values are present in the workflow environment (`AZURE_SUBSCRIPTION_ID`, `AZURE_LOCATION`, and `AZURE_RESOURCE_GROUP`).
5. Confirm `aspire deploy --environment Staging` completed without errors.
6. Capture deployment output and resulting ACA endpoint URL.

## 5) First-deploy smoke checks

After the first successful staging deployment, run and record these checks:

1. Authentication and authority path
- Unauthenticated access redirects to sign-in.
- `AzureAd:TenantId=organizations` behaviour works as expected.
- Tenant-specific authority behaviour still works when configured.

2. Planner workflow path
- Container and plan loading succeeds from Microsoft Graph.
- CSV preview succeeds with expected validation behaviour.
- Import execution completes for a representative sample file.

3. Persistence and runtime safety
- Tenant metadata reads/writes succeed (table-backed path).
- Data Protection persistence survives a container restart.
- No secrets, tokens, or raw payloads appear in logs.

4. Observability
- If configured, telemetry export is visible at the expected endpoint.
- Failures produce actionable diagnostics without leaking sensitive values.

## 6) Rollback and safety notes

- Prefer stopping new staging deployments over forcing production promotion.
- If deployment state appears stale, rerun deployment after reviewing cached state and deployment inputs.
- Keep production isolated: separate environment secrets, approvals, and promotion decision.
