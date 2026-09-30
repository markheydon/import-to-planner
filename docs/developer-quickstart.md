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

Commercial mode settings:

- `Features:CommercialMode:Enabled`
- `Features:CommercialMode:RetentionSweepEnabled`
- `Storage:CommercialAccountsTable` (required when commercial mode is enabled)
- `Storage:CommercialAuditTable` (required when commercial mode is enabled)

Removed keys such as `DeploymentMode:*`, `PlannerGateway:*`, and `HostedStorage:*` are treated as startup validation errors.

When commercial mode is disabled, startup validation and infrastructure registration intentionally keep the existing self-hosted path working without requiring commercial table settings.

## Commercial account verification loop

1. Run with `Features:CommercialMode:Enabled=true` and confirm the sign-in gate appears for unsigned sessions.
2. Sign in and confirm first-use account creation guidance appears once, then workflow access continues.
3. Open `/profile` and confirm only `TenantId`, `UserId`, and account created date are shown from persisted account data.
4. Delete the account, then confirm the Home page shows retention guidance and offers restore.
5. Restore the account and confirm the existing account record is reactivated.
6. Re-run with `Features:CommercialMode:Enabled=false` and confirm self-hosted sign-in behaviour is unchanged.

## Minimum Graph prerequisites

Graph prerequisites are needed for all supported planner scenarios:

- A Microsoft 365 account with Planner access.
- Entra ID app registration.
- Valid `AzureAd` settings and certificate details.
- Graph scopes defined under `DownstreamApis:MicrosoftGraph:Scopes`.

See:

- `src/ImportToPlanner.Web/appsettings.json`
- `docs/microsoft-graph-guidelines.md`

## Public Hugo site (contributors)

Preview the end-user product site without installing Hugo locally. Docker or Podman is required.

```bash
./scripts/invoke-hugo-site.sh build
./scripts/invoke-hugo-site.sh preview   # static site on port 8080
./scripts/invoke-hugo-site.sh serve     # live reload on port 1313
```

Windows parity: `./scripts/Invoke-HugoSite.ps1` with the same commands.

Source content lives under `website/content/`. Build output is `website/public/` (gitignored).
Production publishes only on `v*` tags via GitHub Actions — see `docs/release-runbook.md`.

Native Hugo installs are optional; container scripts are the supported contributor path.

## Build metadata (in-app About, FR-004)

The signed-in **About** page can show optional support context (UTC build time and source
revision) in addition to the MinVer release label. These fields are **optional**: when a
property is not set at build time, About omits that row rather than showing placeholders.

`Directory.Build.props` embeds values into assembly metadata when the corresponding MSBuild
property is non-empty:

| MSBuild property | Purpose |
| --- | --- |
| `SourceRevisionId` | Git commit SHA (short or full) built into the app |
| `BuildTimestampUtc` | UTC build timestamp (ISO-8601 string, for example `2026-09-30T12:00:00Z`) |
| `ContinuousIntegrationBuild` | Set to `true` in CI or publish pipelines so tooling treats the build as deterministic CI output (recommended alongside the properties above) |

Example local build with metadata (for validating About diagnostics):

```bash
dotnet build ImportToPlanner.slnx \
  /p:SourceRevisionId="$(git rev-parse --short HEAD)" \
  /p:BuildTimestampUtc="$(date -u +"%Y-%m-%dT%H:%M:%SZ")" \
  /p:ContinuousIntegrationBuild=true
```

Ordinary contributor builds without these properties still produce an honest pre-release
release label from MinVer; only the optional About rows stay hidden.

**Release label vs staging:** Application builds on `main`, pull requests, and staging use
MinVer pre-release identifiers (not an unqualified shipping tag). The live public Hugo site
continues to show the last **`v*`** tag until the next tagged release cut. For tagging,
deploy order, and verification, see [Public site and application release runbook](release-runbook.md)
(especially [SemVer tagging and deploy](release-runbook.md#semver-tagging-and-deploy)).

## Public vs internal documentation split

- `website/` and `https://docs.importplanner.app` are public end-user guides (Hugo).
- `docs/` is engineering-only and is not published to the public site artefact.
