# Aspire Production Readiness

This document tracks deployment preparation only. It does **not** change current implementation behaviour.

## Checklist

- Keep `src/ImportToPlanner.AppHost/AppHost.cs` scoped to a single `web` resource unless scaling requirements are explicitly approved.
- Confirm hosted environment secrets and configuration handoff for `AzureAd:*`, `Storage:*`, certificate credential source, and `OTEL_EXPORTER_OTLP_ENDPOINT`.
- Keep `/health` and `/alive` unexposed in non-development environments unless an authenticated or private ingress policy is in place.
- Ensure CI keeps validating the current scope: `dotnet restore ImportToPlanner.slnx`, `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`, `dotnet build ImportToPlanner.slnx --no-restore`, and `dotnet test ImportToPlanner.slnx --no-build` (AppHost is currently validated via the solution-level restore/build).
- Record deployment handoff ownership for AppHost settings and web app secrets before enabling hosted rollout.
- For hosted mode, keep the first Azure rollout to two environments only: `Staging` for automatic GitHub Actions deployments and `Production` for manual promotion.

## AppHost Deployment Shape

- The AppHost defines one Azure Container Apps environment (`aca-env`) for deployment.
- The AppHost keeps one compute resource (`web`) and publishes it as an Azure Container App.
- The web resource exposes external HTTP endpoints through ACA ingress.
- The AppHost applies environment variables for the web runtime explicitly (`ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT`) because Aspire environments do not automatically flow to child resources.
- Replica defaults are staging-first: non-production uses `minReplicas = 0`; production uses `minReplicas = 1`.
- The current hosted cap is `maxReplicas = 1` so only 0 or 1 web replica runs at a time.

## Runtime Configuration

| Key | Local development (default) | Hosted environment (production-ready expectation) |
| --- | --- | --- |
| `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Instance`, `AzureAd:CallbackPath` | Store in user secrets for local Graph-mode testing. | Provide via AppHost deploy parameters (`azureAdTenantId`, `azureAdClientId`) and platform-managed defaults; never commit values. |
| `AzureAd:ClientCertificates:0:SourceType`, `AzureAd:ClientCertificates:0:CertificateDiskPath`, `AzureAd:ClientCertificates:0:CertificatePassword`, `AzureAd:ClientCertificates:0:CertificateBase64` | Use `SourceType: Path` with an absolute Linux, WSL, or macOS-visible path to a local `.pfx`. | Current hosted baseline uses AppHost deploy parameters (`graphClientCertificatePassword`, `graphClientCertificateBase64`) and startup materialisation to `/tmp/import-to-planner-graph-client.pfx`; production should move to managed certificate sources when ready. |
| `Storage:TenantMetadataTable`, `Storage:DataProtectionContainer`, `Storage:DataProtectionBlob` | Keep defaults from `appsettings.json` unless a local test scenario requires alternate names. | Provide explicit values through hosted configuration so metadata and Data Protection persistence remain stable across restarts. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Usually unset unless developing against a local collector. | Set to the hosted OTLP collector endpoint to export logs, metrics, and traces; when unset, OTLP export remains disabled. |

## Health Endpoint Policy

- Current code only maps `/health` and `/alive` in Development (`MapDefaultEndpoints`).
- For non-development environments, keep these endpoints disabled by default.
- If enabled later for operations tooling, expose them only through private networking or authenticated ingress and document the policy owner.

## Deployment Handoff

1. Confirm `src/ImportToPlanner.AppHost/AppHost.cs` still defines only the `web` app and the `aca-env` environment.
2. Hand off hosted configuration values (`AzureAd:*`, `Storage:*`, certificate source, and OTLP endpoint) to the deployment owner as secret-backed settings, including AppHost parameter mapping in CI.
3. Verify redirect URI and delegated Graph permissions remain aligned with the target hosted URL.
4. Run CI-equivalent validation before rollout: solution build and tests plus AppHost build.
5. Capture post-deployment smoke checks, including sign-in, preview, execute, and telemetry flow, before enabling wider access.

## Hosted Reference Deployment

The approved low-cost hosted baseline for the multi-tenant design is:

- Azure Container Apps ingress owns the public hosted endpoint.
- The AppHost remains a single `web` resource initially.
- Minimal tenant metadata lives in Azure Table Storage only.
- ASP.NET Core Data Protection keys live in Blob storage from that same hosted storage account.
- Platform-managed app settings and secrets are preferred.
- Azure Key Vault is added only when certificate handling genuinely requires it.
- Existing OTLP routing is reused before any paid monitoring expansion.

Use `Staging` as the single shared non-production environment name. GitHub Actions deploys to
`Staging` automatically after successful CI, while `Production` is promoted separately with a
manual approval step and isolated configuration.

## Hosted rollout guardrails and evidence

Before promoting hosted changes, capture and retain the following evidence:

- `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj` and `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj` pass with no new failures.
- Focused authority-path parity evidence exists for planner-facing behaviour changes in both `AzureAd:TenantId=organizations` and tenant-specific authority runs.
- Hosted sign-in evidence confirms supported work or school account admission across at least two customer tenants and rejects unsupported account types before workflow entry.
- Tenant-isolation evidence confirms hosted metadata and active workflow context are partitioned by tenant boundary.
- Consent-flow evidence confirms both user-consent and administrator-consent-required paths render clear UK-English guidance.
- Telemetry evidence confirms deployment mode, tenant-safe key, consent status, and failure category are emitted without tokens, secrets, or raw import payloads.

Rollout guardrails:

- Keep the initial hosted deployment on one active web replica unless approved scaling criteria are met.
- Keep hosted persistence scope limited to tenant-scoped configuration, consent state, and support diagnostics.
- Keep durable Data Protection storage blob-backed for both local and hosted runs.
- Keep hosted-only configuration out of self-hosted defaults.
- Defer extra always-on backing services until measured demand justifies the cost and complexity.

## Azure Resource Options

| Stage | Recommended low-cost options | Notes |
| --- | --- | --- |
| As-is (single web resource) | Azure Container Apps with a consumption or serverless profile, plus Azure Table Storage for minimal tenant metadata and optional Azure Key Vault for certificate material | Keeps baseline cost low and aligns with Aspire deployment tooling when only one web process is hosted. |
| Telemetry-enabled hosted runs | Existing OTLP collector endpoint first, or a minimal Azure Monitor/OpenTelemetry pipeline | Prefer reusing an existing collector to avoid extra always-on cost. |
| Future scaling (if approved) | Container Apps scale rules, optional Front Door or Application Gateway, optional Key Vault references, and additional Aspire resources only after explicit approval | Add only when measurable demand requires it; keep database, cache, and queue resources out of scope unless a separate issue approves them. |
