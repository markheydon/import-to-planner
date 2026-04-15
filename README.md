# Import To Planner

Single-use Blazor utility to import tasks from CSV into Microsoft Planner with explicit validation, dry-run preview, confirmation, and execution reporting.

## Current Implementation Status

This repository includes both runtime modes:

- Graph mode via `GraphPlannerGateway` for live Microsoft Graph delegated calls (beta API)
- In-memory mode via `InMemoryPlannerGateway` for local development without tenant credentials

Switch modes with `PlannerGateway:UseGraph` in configuration.

Implemented solution components:

- Web app: `src/ImportToPlanner.Web`
- Application layer: `src/ImportToPlanner.Application`
- Domain models: `src/ImportToPlanner.Domain`
- Graph infrastructure: `src/ImportToPlanner.Infrastructure.Graph`
- Tests: `tests/ImportToPlanner.Tests`

## Prerequisites

### Required to build/run (in-memory mode)

- .NET 10 SDK

### Additional requirements for Graph mode

- Microsoft 365 tenant with at least one Microsoft 365 group
- Entra app registration configured for delegated authentication (see setup below)
- Certificate credential configured for the Entra app registration (not client secret)

### Optional tooling

- Aspire CLI (`aspire`) for local orchestration workflow (you can also run via `dotnet run`)
  - `aspire start` requires an OCI-compatible container runtime even when the AppHost has no container resources; on WSL, [Podman](https://podman.io/) is the recommended alternative to Docker Desktop
- GitHub CLI (`gh`) for issue/PR workflow

## GitHub Codespaces

This repository includes a Codespaces-ready dev container in `.devcontainer/devcontainer.json`.

Recommended baseline for this repository:

- 4 CPU cores
- 8 GB memory
- 32 GB storage

That is intentionally lower than the general Aspire template because the current AppHost only orchestrates the Blazor web project and does not currently require Docker-in-Docker or additional language runtimes.

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

> **Note:** `aspire start` requires a container runtime (e.g. Podman or Docker Desktop) to be running before you start the AppHost. On WSL, Podman is recommended (`sudo apt install podman`). The `dotnet run` path does not have this requirement.

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

## Known Limitations / Future Work

- Single-tenant configuration only; multi-tenant support is not yet implemented
- Production hosting/deployment guidance is not yet included
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
