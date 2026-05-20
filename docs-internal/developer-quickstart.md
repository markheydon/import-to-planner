# Developer Quick-Start

This guide is for contributors who want to run, debug, and evolve Import To Planner.

For public-facing operator and deployment guidance, see `docs/README.md`.

## Pick the right path

Start with the simplest path first, then move to Graph or hosted scenarios as needed.

| Scenario | Recommended mode | Why |
| --- | --- | --- |
| First run, no tenant credentials | Self-hosted single-tenant + in-memory gateway | Fastest way to explore the workflow and UI without authentication or consent setup |
| Real Planner integration for your own tenant | Self-hosted single-tenant + Graph gateway | Tests delegated Graph behaviour while keeping deployment assumptions simple |
| Hosted shared-service behaviour | Hosted shared multi-tenant + hosted storage | Exercises tenant-aware sign-in, consent handling, and tenant metadata boundaries |

## VS Code launch profiles

Use `.vscode/launch.json` launch options in this order:

1. `Aspire: Run (Single Tenant - In Memory)`
2. `Aspire: Run (Single Tenant + Graph)`
3. `Aspire: Run (Multi Tenant + Hosted Storage)`

The first option is intentionally the default contributor path.

## CLI first run (without Aspire)

If you prefer a plain CLI run, use explicit environment overrides so the app stays in local single-tenant in-memory mode:

```bash
DeploymentMode__Mode=SelfHostedSingleTenant \
HostedStorage__Enabled=false \
PlannerGateway__UseGraph=false \
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

## Why Aspire is recommended for contributors

Aspire is not only for deployment. In this repository it is the easiest way to switch safely between local single-tenant and hosted-oriented scenarios:

- It keeps launch profiles explicit and repeatable for the three supported development paths.
- It enables hosted storage only when a profile requires it.
- It reduces hidden setup drift between contributors.

Use these commands when working from the terminal:

```bash
aspire start --isolated
aspire describe
aspire logs web
aspire stop
```

For the default single-tenant in-memory profile, no container runtime is required.
For hosted-storage scenarios, a container runtime is required so Azurite can run.

## Configuration matrix

The key runtime switches are:

- `DeploymentMode:Mode`
- `PlannerGateway:UseGraph`
- `HostedStorage:Enabled`

| DeploymentMode:Mode | PlannerGateway:UseGraph | HostedStorage:Enabled | Typical use |
| --- | --- | --- | --- |
| `SelfHostedSingleTenant` | `false` | `false` | First-run local exploration and most workflow/UI work |
| `SelfHostedSingleTenant` | `true` | `false` | Single-tenant Graph integration testing |
| `HostedSharedMultiTenant` | `true` | `true` | Hosted multi-tenant sign-in, consent, and tenant metadata behaviour |

## Minimum Graph prerequisites

Only needed for Graph-backed scenarios (`PlannerGateway:UseGraph=true`):

- A Microsoft 365 account with Planner access.
- Entra ID app registration.
- Valid `AzureAd` settings and certificate details.
- Graph scopes defined under `DownstreamApis:MicrosoftGraph:Scopes`.

See:

- `src/ImportToPlanner.Web/appsettings.json`
- `docs-internal/microsoft-graph-guidelines.md`

## Public vs internal documentation split

- `docs/` is public-facing. Keep it focused on running and operating the app for organisations.
- `docs-internal/` is contributor-facing. Keep implementation, debugging, and engineering workflow detail here.
