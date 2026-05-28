# Developer Quick-Start

This guide is for contributors who want to run, debug, and evolve Import To Planner.

For public-facing operator and deployment guidance, see `docs/README.md`.

## Pick the right path

Start with the shared-organisations authority path first, then move to a specific-tenant authority as needed.

| Scenario | Recommended mode | Why |
| --- | --- | --- |
| First run with shared authority | `AzureAd:HomeTenantId=multiple` via Aspire AppHost | Exercises supported sign-in and consent guards with the default authority path |
| Tenant-constrained verification | `AzureAd:HomeTenantId=<tenant-id-or-domain>` via Aspire AppHost | Confirms single-tenant authority behaviour with the same runtime dependencies |
| Web-only troubleshooting | Run `ImportToPlanner.Web` directly with equivalent storage and AzureAd settings | Useful for focused debugging when AppHost orchestration is not required |

## VS Code launch profiles

Use `.vscode/launch.json` launch options in this order:

1. `Aspire: Run (Single Tenant - In Memory)`
2. `Aspire: Run (Single Tenant + Graph)`
3. `Aspire: Run (Multi Tenant + Hosted Storage)`

The first option is intentionally the default contributor path.
Profile names are legacy labels; runtime behaviour now uses the single supported Graph and storage-backed path.

## CLI first run (with Aspire)

Use Aspire for the default local flow so storage references are wired automatically:

```bash
aspire run
```

## Why Aspire is recommended for contributors

Aspire is not only for deployment. In this repository it is the easiest way to run the supported local topology consistently:

- It keeps launch profiles explicit and repeatable for authority-focused verification.
- It wires `storage`, `blobs`, and `tables` references consistently for every run.
- It reduces hidden setup drift between contributors.

Use these commands when working from the terminal:

```bash
aspire start --isolated
aspire describe
aspire logs web
aspire stop
```

For local development, a container runtime is required so Azurite can run.
ASP.NET Core Data Protection keys are always persisted to blob storage in this feature branch.

## Configuration focus

The key settings are now authority and storage values owned by the Web project:

- `AzureAd:TenantId`
- `AzureAd:HomeTenantId`
- `Storage:TenantMetadataTable`
- `Storage:DataProtectionContainer`
- `Storage:DataProtectionBlob`

Removed keys such as `DeploymentMode:*`, `PlannerGateway:*`, and `HostedStorage:*` are treated as startup validation errors.

## Minimum Graph prerequisites

Graph prerequisites are needed for all supported planner scenarios:

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
