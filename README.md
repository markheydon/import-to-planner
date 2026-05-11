# Import To Planner

Single-use Blazor utility to import tasks from CSV into Microsoft Planner with explicit validation, dry-run preview, confirmation, and execution reporting.

## Current Implementation Status

This repository includes both runtime modes:

- Graph mode via `GraphPlannerGateway` for live Microsoft Graph delegated calls (beta API)
- In-memory mode via `InMemoryPlannerGateway` for local development without tenant credentials

Switch modes with `PlannerGateway:UseGraph` in configuration.

Runtime behaviour notes:

- **Task matching**: existing Planner tasks are matched by task name only (exact, case-insensitive). No fuzzy matching or external ID tracking is used. Matched tasks are skipped and reported as `already exists`.
- **Retry-once policy**: transient row-level failures (HTTP 503) are retried once per row. If the retry also fails, the row is reported as an error and execution continues on remaining rows. Throttling (HTTP 429) uses a separate retry budget of up to 3 attempts with back-off.
- **Stale-preview enforcement**: after preview is generated, any change to the target Planner's tasks, buckets, or plan is detected via a SHA-256 fingerprint comparison on execution. If the state has changed, execution is blocked and the user must refresh the preview.
- **Mode semantics**: in-memory mode is functionally equivalent to Graph mode for all preview and execution behaviour; the gateway abstraction (`IPlannerGateway`) ensures identical application logic runs in both modes. Runtime parity is verified by the test suite.

Implemented solution components:

- Web app: `src/ImportToPlanner.Web`
- Application layer: `src/ImportToPlanner.Application`
- Domain models: `src/ImportToPlanner.Domain`
- Graph infrastructure: `src/ImportToPlanner.Infrastructure.Graph`
- Tests: `tests/ImportToPlanner.Tests`
- UI component tests: `tests/ImportToPlanner.Web.Tests`

## Prerequisites

### Required to build/run (in-memory mode)

- .NET 10 SDK

### Additional requirements for Graph mode

- Microsoft 365 tenant with at least one Microsoft 365 group
- Entra app registration configured for delegated authentication (see setup below)
- Certificate credential configured for the Entra app registration (not client secret)

### Optional tooling

- Aspire CLI (`aspire`) for local orchestration workflow (you can also run via `dotnet run`)
  - For the current AppHost in this repository, `aspire start` works without Docker or Podman because the graph only contains the Blazor web project
  - `aspire doctor` still checks for a container runtime as a general Aspire prerequisite and may report `No container runtime detected` in environments such as GitHub Codespaces even when this repository runs successfully
  - If future changes add container-backed Aspire resources, an OCI-compatible runtime will become required; on WSL, [Podman](https://podman.io/) is the recommended alternative to Docker Desktop
- GitHub CLI (`gh`) for issue/PR workflow

## GitHub Codespaces

This repository includes a Codespaces-ready dev container in `.devcontainer/devcontainer.json`.

Recommended baseline for this repository:

- 4 CPU cores
- 8 GB memory
- 32 GB storage

That is intentionally lower than the general Aspire template because the current AppHost only orchestrates the Blazor web project and does not currently require Docker-in-Docker or additional language runtimes.

The dev container intentionally does not install Docker-in-Docker. That keeps Codespaces startup lighter and matches the current AppHost, which only launches a .NET project resource.

If you run `aspire doctor` in Codespaces, it may still report `No container runtime detected`. Treat that as a generic Aspire prerequisite warning, not as a blocker for this repository in its current shape.

This repository also does not commit Aspire agent or MCP configuration by default. The workspace is ready to build and run as-is, but if you plan to use AI tooling that can benefit from Aspire agent setup, run `aspire agent init` locally in your environment.

If you want faster startup times, enable a prebuild in the GitHub repository settings for the `main` branch and this dev container configuration. Prebuilds are configured in GitHub, not in this repository. For a single-user or small-team repository, starting with a prebuild for `main` in your primary region is the most cost-effective default.

## Entra App Registration Setup

Create a single-tenant app registration for delegated auth.

- Supported account types: single tenant
- Redirect URI (Web): `https://localhost:7129/signin-oidc`
- Credential type: certificate
- Do not use client secrets for this project

Required Microsoft Graph delegated API permissions:

- `User.Read`
- `Group.Read.All`
- `GroupMember.Read.All`
- `Tasks.ReadWrite`

For the step-by-step manual setup task, see [issue #8](https://github.com/markheydon/import-to-planner/issues/8).

## Configuration

The app is configured through `appsettings.json` plus local developer secrets.

### In-memory mode (no credentials required)

In-memory mode is the default in this repository (`PlannerGateway:UseGraph` is already `false` in `appsettings.json`), so no configuration changes are required for a first run.

Reference value:

```json
{ "PlannerGateway": { "UseGraph": false } }
```

This mode is ideal for first-run local validation because no Entra tenant values or certificates are needed.

### Graph mode (real tenant)

Use .NET user secrets for local development:

```bash
dotnet user-secrets set "AzureAd:TenantId" "<your-tenant-id>" --project src/ImportToPlanner.Web
dotnet user-secrets set "AzureAd:ClientId" "<your-client-id>" --project src/ImportToPlanner.Web
dotnet user-secrets set "AzureAd:ClientCertificates:0:SourceType" "Path" --project src/ImportToPlanner.Web
dotnet user-secrets set "AzureAd:ClientCertificates:0:CertificateDiskPath" "/absolute/path/to/import-to-planner.pfx" --project src/ImportToPlanner.Web
dotnet user-secrets set "AzureAd:ClientCertificates:0:CertificatePassword" "<your-pfx-password>" --project src/ImportToPlanner.Web
dotnet user-secrets set "PlannerGateway:UseGraph" "true" --project src/ImportToPlanner.Web
```

Notes:

- The `.pfx` must include the private key and correspond to the public certificate uploaded to Entra.
- On WSL/Linux/macOS, use file path certificates (`SourceType: Path`) with OS-visible absolute paths.
- Never commit certificate files, passwords, tenant IDs, or client IDs.

## Run Locally

Common validation steps:

```bash
dotnet restore ImportToPlanner.slnx
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
```

### Run directly with dotnet

```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

### Run with Aspire orchestration

> **Note:** For the current AppHost, `aspire start` does not require Docker or Podman because it only launches the web project. If you add container-backed resources later, you will need a container runtime. `dotnet run` remains the simplest option when you only want to run the web app directly.

```bash
aspire start --isolated
aspire describe
aspire logs web
```

Stop the AppHost when finished:

```bash
aspire stop
```

## CSV Example

```csv
Task Name,Description,Priority,Bucket,Goal
Create user stories,Draft sprint stories,Important,Backlog,Delivery
Review architecture,Validate boundaries,5,Architecture,Quality
Prepare release notes,,Low,,Communication
```

## Import Behaviour Notes

- Dry-run preview is non-destructive and required before execution.
- Existing task matches use task name only.
- Matched rows are skipped and reported as `already exists`.
- If planner state changes between preview and execution, execution is blocked and a fresh preview is required.
- Row failures do not stop the full import run; remaining rows continue and the report reflects partial success.
- Transient Graph row-level failures are retried once before the row is marked failed.
- Runtime-mode semantics are aligned between in-memory and Graph gateways for preview and execution outcomes.

## Important Notes

### Graph API Version

This app uses Microsoft Graph beta API (`https://graph.microsoft.com/beta`).

- v1.0 does not support the full personal planner plan scenarios used here
- beta APIs can change; see official guidance: https://learn.microsoft.com/graph/api/overview?view=graph-rest-beta

### Planner Constraints

- Supported plan tier: basic planner plans
- Goals are not currently settable via Graph API; this app outputs manual actions for goal creation and task-goal mapping
- App supports group-linked and personal planner containers

## CI Notes

CI validates both application and AppHost:

- Solution restore/build/test on `ImportToPlanner.slnx`
- Aspire AppHost restore/build via `dotnet restore apphost.cs` and `dotnet build apphost.cs --no-restore`

See `.github/workflows/ci.yml` for the full pipeline.

## Aspire Production-Readiness Checklist (Planning Only)

This issue tracks deployment preparation only. It does **not** add new runtime resources or change current implementation behaviour.

- [ ] Keep `apphost.cs` scope to a single `web` resource unless scaling requirements are explicitly approved.
- [ ] Confirm hosted environment secrets/config handoff for `PlannerGateway:UseGraph`, `AzureAd:*`, certificate credential source, and `OTEL_EXPORTER_OTLP_ENDPOINT`.
- [ ] Keep `/health` and `/alive` unexposed in non-development environments unless an authenticated or private ingress policy is in place.
- [ ] Ensure CI keeps validating current scope: `dotnet build ImportToPlanner.slnx`, `dotnet test ImportToPlanner.slnx`, and `dotnet build apphost.cs --no-restore`.
- [ ] Record deployment handoff ownership for AppHost settings and web app secrets before enabling hosted rollout.

### Runtime configuration by environment

| Key | Local development (default) | Hosted environment (production-ready expectation) |
| --- | --- | --- |
| `PlannerGateway:UseGraph` | `false` by default for in-memory mode; set `true` only when testing real tenant flows locally. | `true` when the hosted instance should call Microsoft Graph; keep `false` only for non-production smoke environments. |
| `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Instance`, `AzureAd:CallbackPath` | Store in user secrets for local Graph-mode testing. | Provide via platform-managed app settings/secrets; never commit values. |
| `AzureAd:ClientCertificates:0:SourceType`, `AzureAd:ClientCertificates:0:CertificateDiskPath`, `AzureAd:ClientCertificates:0:CertificatePassword` | Use `SourceType: Path` with an absolute Linux/WSL/macOS-visible path to a local `.pfx`. | Prefer a cloud-native certificate source (for example Key Vault/certificate store integration) and avoid mounted disk-path certificates where possible. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Usually unset unless developing against a local collector. | Set to the hosted OTLP collector endpoint to export logs/metrics/traces; when unset, OTLP export remains disabled. |

### Health endpoint exposure policy

- Current code only maps `/health` and `/alive` in Development (`MapDefaultEndpoints`).
- For non-development environments, keep these endpoints disabled by default.
- If enabled later for operations tooling, expose them only through private networking or authenticated ingress and document the owner of that policy.

### Aspire AppHost deployment handoff checklist

1. Confirm `apphost.cs` still defines only the `web` app and required launch profile.
2. Hand off hosted configuration values (`PlannerGateway:UseGraph`, `AzureAd:*`, certificate source, OTLP endpoint) to the deployment owner as secret-backed settings.
3. Verify redirect URI and delegated Graph permissions remain aligned with the target hosted URL.
4. Run CI-equivalent validation before rollout: solution build/tests plus AppHost build.
5. Capture post-deployment smoke checks (sign-in, preview, execute, telemetry flow) before enabling wider access.

### Aspire-compatible Azure resource options (cost-first)

| Stage | Recommended low-cost options | Notes |
| --- | --- | --- |
| As-is (single web resource) | Azure Container Apps (consumption/serverless profile) + optional Azure Key Vault for certificate material | Keeps baseline cost low and aligns with Aspire deployment tooling when only one web process is hosted. |
| Telemetry-enabled hosted runs | Existing OTLP collector endpoint first, or minimal Azure Monitor/OpenTelemetry pipeline | Prefer reusing an existing collector to avoid extra always-on cost. |
| Future scaling (if approved) | Container Apps scale rules, optional Front Door/App Gateway, and optional Key Vault references | Add only when measurable demand requires it; keep DB/cache/queue out of scope unless a separate issue approves them. |

## Known Limitations / Future Work

- Single-tenant configuration only; multi-tenant support is not yet implemented
- Hosted deployment implementation is not yet included
- Certificate loading uses local path-based secrets; Key Vault integration is future work
- Graph beta dependency may require adjustments if API contracts change

## Implementation Roadmap (GitHub Issues)

Tracked in [issue #1](https://github.com/markheydon/import-to-planner/issues/1) with child issues:

1. [#2](https://github.com/markheydon/import-to-planner/issues/2): Implement Graph gateway with beta support
2. [#3](https://github.com/markheydon/import-to-planner/issues/3): Add Entra delegated auth wiring
3. [#4](https://github.com/markheydon/import-to-planner/issues/4): Use real user-accessible containers and plans
4. [#5](https://github.com/markheydon/import-to-planner/issues/5): Add Graph error handling and retries
5. [#6](https://github.com/markheydon/import-to-planner/issues/6): Add Graph gateway test coverage
6. [#7](https://github.com/markheydon/import-to-planner/issues/7): Improve README for new-developer setup
7. [#8](https://github.com/markheydon/import-to-planner/issues/8): Entra app registration and permissions (manual task)
8. [#9](https://github.com/markheydon/import-to-planner/issues/9): Local secret configuration for tenant values (manual task)
