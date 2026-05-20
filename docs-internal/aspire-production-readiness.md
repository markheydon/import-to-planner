# Aspire Production Readiness

This document tracks deployment preparation only. It does **not** change current implementation behaviour.

## Checklist

- Keep `apphost.cs` scoped to a single `web` resource unless scaling requirements are explicitly approved.
- Confirm hosted environment secrets and configuration handoff for `PlannerGateway:UseGraph`, `AzureAd:*`, certificate credential source, and `OTEL_EXPORTER_OTLP_ENDPOINT`.
- Keep `/health` and `/alive` unexposed in non-development environments unless an authenticated or private ingress policy is in place.
- Ensure CI keeps validating the current scope: `dotnet restore ImportToPlanner.slnx`, `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`, `dotnet build ImportToPlanner.slnx --no-restore`, `dotnet restore apphost.cs`, `dotnet build apphost.cs --no-restore`, and `dotnet test ImportToPlanner.slnx --no-build`.
- Record deployment handoff ownership for AppHost settings and web app secrets before enabling hosted rollout.
- For hosted mode, keep the first Azure rollout to two environments only: `Staging` for automatic GitHub Actions deployments and `Production` for manual promotion.

## Runtime Configuration

| Key | Local development (default) | Hosted environment (production-ready expectation) |
| --- | --- | --- |
| `PlannerGateway:UseGraph` | `false` by default for in-memory mode; set `true` only when testing real tenant flows locally. | `true` when the hosted instance should call Microsoft Graph; keep `false` only for non-production smoke environments. |
| `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Instance`, `AzureAd:CallbackPath` | Store in user secrets for local Graph-mode testing. | Provide via platform-managed app settings and secrets; never commit values. |
| `AzureAd:ClientCertificates:0:SourceType`, `AzureAd:ClientCertificates:0:CertificateDiskPath`, `AzureAd:ClientCertificates:0:CertificatePassword` | Use `SourceType: Path` with an absolute Linux, WSL, or macOS-visible path to a local `.pfx`. | Prefer a cloud-native certificate source, such as Key Vault or certificate store integration, and avoid mounted disk-path certificates where possible. |
| `HostedStorage:ConnectionString`, `HostedStorage:DataProtectionContainer`, `HostedStorage:DataProtectionBlob` | Leave unset for ordinary local single-tenant work. Only set them when intentionally exercising hosted-storage behaviour locally. | Required when `HostedStorage:Enabled=true` so hosted deployments keep ASP.NET Core Data Protection keys durable and shared across restarts. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Usually unset unless developing against a local collector. | Set to the hosted OTLP collector endpoint to export logs, metrics, and traces; when unset, OTLP export remains disabled. |

## Health Endpoint Policy

- Current code only maps `/health` and `/alive` in Development (`MapDefaultEndpoints`).
- For non-development environments, keep these endpoints disabled by default.
- If enabled later for operations tooling, expose them only through private networking or authenticated ingress and document the policy owner.

## Deployment Handoff

1. Confirm `apphost.cs` still defines only the `web` app and required launch profile.
2. Hand off hosted configuration values (`PlannerGateway:UseGraph`, `AzureAd:*`, certificate source, and OTLP endpoint) to the deployment owner as secret-backed settings.
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
- Focused runtime-mode parity evidence exists for planner-facing behaviour changes in both `PlannerGateway:UseGraph=false` and `PlannerGateway:UseGraph=true` runs.
- Hosted sign-in evidence confirms supported work or school account admission across at least two customer tenants and rejects unsupported account types before workflow entry.
- Tenant-isolation evidence confirms hosted metadata and active workflow context are partitioned by tenant boundary.
- Consent-flow evidence confirms both user-consent and administrator-consent-required paths render clear UK-English guidance.
- Telemetry evidence confirms deployment mode, tenant-safe key, consent status, and failure category are emitted without tokens, secrets, or raw import payloads.

Rollout guardrails:

- Keep the initial hosted deployment on one active web replica unless approved scaling criteria are met.
- Keep hosted persistence scope limited to tenant-scoped configuration, consent state, and support diagnostics.
- Keep durable Data Protection storage as a hosted-only concern; ordinary self-hosted local runs stay storage-free by default.
- Keep hosted-only configuration out of self-hosted defaults.
- Defer extra always-on backing services until measured demand justifies the cost and complexity.

## Azure Resource Options

| Stage | Recommended low-cost options | Notes |
| --- | --- | --- |
| As-is (single web resource) | Azure Container Apps with a consumption or serverless profile, plus Azure Table Storage for minimal tenant metadata and optional Azure Key Vault for certificate material | Keeps baseline cost low and aligns with Aspire deployment tooling when only one web process is hosted. |
| Telemetry-enabled hosted runs | Existing OTLP collector endpoint first, or a minimal Azure Monitor/OpenTelemetry pipeline | Prefer reusing an existing collector to avoid extra always-on cost. |
| Future scaling (if approved) | Container Apps scale rules, optional Front Door or Application Gateway, optional Key Vault references, and additional Aspire resources only after explicit approval | Add only when measurable demand requires it; keep database, cache, and queue resources out of scope unless a separate issue approves them. |
