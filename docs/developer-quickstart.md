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

## VS Code and the AppHost

The supported entry point is `src/ImportToPlanner.AppHost` (`.NET Aspire`), not the Web project alone.

- VS Code: use **Aspire: Launch default AppHost** in `.vscode/launch.json` (workspace folder = repository root).
- CLI: run `aspire run` from the repository root (or `dotnet run --project src/ImportToPlanner.AppHost`).

Graph sign-in, Azurite-backed storage, and commercial-mode wiring all assume this AppHost path unless you are deliberately reproducing the same references by hand on `ImportToPlanner.Web`.

## CLI first run (with Aspire)

```bash
aspire run
```

On first run, Aspire may prompt for AppHost **parameters** (Azure AD ids, certificate material, `enableCommercialMode`, and so on). Values can also be supplied ahead of time via environment variables or AppHost user secrets (see below).

## Why Aspire is recommended for contributors

Aspire is not only for deployment. In this repository it models the local topology in `AppHost.cs`:

| Resource | Always (local) | When `enableCommercialMode` is true at AppHost startup |
| --- | --- | --- |
| `storage` (Azurite emulator) | Yes | Yes |
| `blobs`, `dataprotection` container | Yes (referenced by `web`) | Yes |
| `tables` | No | Yes (ledger, accounts, billing metadata) |
| `web` (`ImportToPlanner.Web`) | Yes | Yes (+ table reference) |
| `paid-credit-expiry` (`ImportToPlanner.Functions`) | No | Yes (daily paid-credit expiry timer) |

Implications:

- **Commercial off** (default): self-hosted-style path; no table service, no Stripe webhook route, no credit purchase UI, no Functions worker.
- **Commercial on**: you must set `Parameters:enableCommercialMode` to `true` **before** starting the AppHost so `tables` and `paid-credit-expiry` are part of the distributed application model. Toggling only the web flag without restarting the AppHost with commercial mode enabled will not attach table storage or the expiry function.

Use these commands when working from the terminal:

```bash
aspire start --isolated
aspire describe          # HTTPS URL and ports for web (and dashboard)
aspire logs web
aspire logs paid-credit-expiry
aspire stop
```

After changing `ImportToPlanner.Web` or `ImportToPlanner.Functions`, rebuild the affected resource so the running session picks up binaries:

```bash
aspire resource web rebuild
aspire resource paid-credit-expiry rebuild
```

For local development, a container runtime is required so Azurite can run.
ASP.NET Core Data Protection keys are persisted to blob storage via the AppHost `blobs` reference.

## Configuration focus

The key settings are now authority and storage values owned by the Web project:

- `AzureAd:TenantId`
- `AzureAd:HomeTenantId`
- `Storage:TenantMetadataTable`
- `Storage:DataProtectionContainer`
- `Storage:DataProtectionBlob`

### AppHost parameters (orchestration)

These are defined in `src/ImportToPlanner.AppHost/AppHost.cs` and map to environment variables as `Parameters__<name>` (for example `Parameters__enableCommercialMode`).

| Parameter | Typical local source | Effect |
| --- | --- | --- |
| `azureAdTenantId`, `azureAdClientId`, `azureAdHomeTenantId` | `aspire secret` or env | Forwarded to `web` as `AzureAd__*` |
| `graphClientCertificatePassword`, `graphClientCertificateBase64` | `aspire secret` or env | Graph client certificate on `web` |
| `enableCommercialMode` | `aspire secret`, env, or Aspire prompt | When `true` at AppHost startup: adds `tables`, `paid-credit-expiry`, and sets `Features__CommercialMode__Enabled` on `web` |
| `stripeApiKey`, `stripeWebhookSecret` | `aspire secret` (commercial on only) | Forwarded to `web` as `Stripe__ApiKey` and `Stripe__WebhookSecret` |
| `complimentaryTenantIds` (optional) | `aspire secret` or env | Unlimited-credit allowlist on `web` |

Local secrets for the AppHost live in the same user-secrets store as `dotnet user-secrets` on `ImportToPlanner.AppHost`; prefer the Aspire CLI so keys stay consistent with other Aspire workflows:

```bash
aspire secret set Parameters:enableCommercialMode true --apphost src/ImportToPlanner.AppHost
aspire secret list --apphost src/ImportToPlanner.AppHost
aspire secret path --apphost src/ImportToPlanner.AppHost
```

Helper wrappers (same commands, fixed AppHost path):

```bash
./scripts/invoke-aspire-commercial-secrets.sh enable-commercial
./scripts/invoke-aspire-commercial-secrets.sh set-stripe-api-key rk_test_…
./scripts/invoke-aspire-commercial-secrets.sh set-stripe-webhook-secret whsec_…
```

Windows: `./scripts/Invoke-AspireCommercialSecrets.ps1 -Command enable-commercial`

For a single shell session without persisting secrets:

```bash
export Parameters__enableCommercialMode=true
aspire run
```

Staging and production use the same parameter names from CI; see `docs/aca-staging-runbook.md`.

### Web configuration (runtime)

The `web` resource reads `src/ImportToPlanner.Web/appsettings.json` and `appsettings.Development.json`. Under Aspire, AppHost `WithEnvironment` mappings override file defaults for Azure AD, commercial mode, and (when commercial is on) Stripe API credentials. Pack catalogue fields (`Stripe:CreditPacks`) remain in Development appsettings unless you override them via configuration.

Commercial mode on the web app requires:

- `Features:CommercialMode:Enabled` (normally supplied by AppHost from `enableCommercialMode`)
- `Features:CommercialMode:RetentionSweepEnabled` (optional; defaults apply)
- `Storage:TenantMetadataTable`, `Storage:CommercialAccountsTable`, `Storage:CommercialAuditTable`, `Storage:CommercialCreditLedgerTable`, `Storage:CommercialBillingTable` (defaults exist in Development appsettings)
- `Stripe:ApiKey`, `Stripe:WebhookSecret`, and at least one **published** entry under `Stripe:CreditPacks` (paid credit purchase)

When commercial mode is disabled, startup validation and infrastructure registration keep the self-hosted path working without commercial tables or Stripe settings.

Removed keys such as `DeploymentMode:*`, `PlannerGateway:*`, and `HostedStorage:*` are treated as startup validation errors.

Product rules for credits (ledger, FIFO, expiry): [credits-billing-usage-model.md](credits-billing-usage-model.md).

## Commercial account verification loop (Aspire)

1. Run `./scripts/invoke-aspire-commercial-secrets.sh enable-commercial` (or `aspire secret set Parameters:enableCommercialMode true`), then `aspire run`.
2. Confirm `aspire describe` lists `tables` and `web`, and (for paid credits) `paid-credit-expiry`.
3. Confirm the commercial sign-in gate appears for unsigned sessions.
4. Sign in and confirm first-use account creation guidance appears once, then workflow access continues.
5. Open `/profile` and confirm account details and (when the ledger is enabled) credits activity.
6. Delete the account, confirm retention guidance and restore on Home, then restore.
7. Run `./scripts/invoke-aspire-commercial-secrets.sh disable-commercial`, restart the AppHost, and confirm self-hosted sign-in behaviour is unchanged.

## Credits and Stripe (commercial mode on)

Paid credit purchase uses Stripe Checkout and grants credits only from verified webhooks on the `web` resource (`POST /webhooks/stripe`). Use a **Stripe Sandbox** for all local and staging payment testing; do not use live-mode keys locally (non-Production startup rejects `sk_live_` / `rk_live_` prefixes).

### What to configure

1. **AppHost**: enable commercial mode (see above) so `tables` and `paid-credit-expiry` are running.
2. **Stripe Dashboard (Sandbox)**: create Products/Prices for your packs; note each **Price id** (`price_…`).
3. **AppHost secrets** (never commit real keys) — forwarded to `web` when commercial mode is on:

```bash
./scripts/invoke-aspire-commercial-secrets.sh set-stripe-api-key rk_test_…
./scripts/invoke-aspire-commercial-secrets.sh set-stripe-webhook-secret whsec_…
```

Align `Stripe:CreditPacks` in `appsettings.Development.json` with your Sandbox Price ids, or override individual pack fields via environment (for example `Stripe__CreditPacks__0__StripePriceId`). Keep `Stripe:AutomaticTaxEnabled` false unless that Sandbox has an active tax registration.

If you run `ImportToPlanner.Web` without the AppHost, set `Stripe:ApiKey` and `Stripe:WebhookSecret` on that project with `dotnet user-secrets` instead; the Aspire path above is the supported commercial topology.

Table connection strings for `web` come from the AppHost `tables` reference when commercial mode is on; you do not configure Azurite endpoints manually for the default Aspire path.

### Three verification tiers

**1. Automated (default CI — no Stripe account)**

```bash
dotnet test ImportToPlanner.slnx
```

Webhook grant idempotency, FIFO consumption, refunds, commercial-off registration, and related scenarios are covered in `ImportToPlanner.Tests` and `ImportToPlanner.Web.Tests` without calling Stripe.

**2. Optional stripe-mock (HTTP contract only)**

Validates Checkout Session request shape against Stripe’s mock server; it does not grant credits or deliver webhooks.

```bash
./scripts/invoke-stripe-mock.sh contract-test
```

Other commands: `start`, `stop`, `status`. Windows: `./scripts/Invoke-StripeMock.ps1 -Command contract-test`.

**3. Full Sandbox journey (manual, via Aspire)**

1. Start the AppHost with commercial mode enabled and valid Azure AD / Graph secrets so you can sign in.
2. Run `aspire describe` and note the **HTTPS endpoint URL** for `web`.
3. Forward webhooks to the `web` resource (Stripe CLI logged into the same Sandbox as your Prices):

```bash
stripe listen --forward-to https://localhost:<web-https-port>/webhooks/stripe
```

Use the signing secret from `stripe listen` with `./scripts/invoke-aspire-commercial-secrets.sh set-stripe-webhook-secret whsec_…`, then `aspire resource web rebuild` if `web` was already running.

4. Sign in as a **non-allowlisted** commercial user. Use import preview where would-create exceeds remaining credits: shortfall copy, recommended pack, **Confirm import** disabled.
5. Start checkout, optionally abandon once (balance unchanged), then complete payment with a Sandbox test card.
6. After the webhook is delivered, refresh preview: remaining credits increase by the pack size **once**. Replaying the same event must not grant again.
7. Open `/profile`: purchase appears in activity; catalogue has no preview shortfall pre-selection; no Stripe customer id in UI copy.
8. Commercial off: set `Parameters:enableCommercialMode` to `false`, restart AppHost — no buy-credits UI, no `/webhooks/stripe`, startup without Stripe keys.

Credits are **not** granted on the Checkout success redirect alone; fulfilment depends on the webhook reaching `web` while the app is running.

Paid-lot expiry is enforced by the `paid-credit-expiry` function on a daily schedule; unit tests cover the use case with a clock. You do not need to wait for the timer to validate purchase and grant behaviour.

## Minimum Graph prerequisites

Graph prerequisites are needed for all supported planner scenarios:

- A Microsoft 365 account with Planner access.
- Entra ID app registration.
- Valid `AzureAd` settings and certificate details.
- Graph scopes defined under `DownstreamApis:MicrosoftGraph:Scopes`.

See:

- `src/ImportToPlanner.Web/appsettings.json`
- `docs/microsoft-graph-guidelines.md`

## Developer helper scripts

| Script | Purpose |
| --- | --- |
| `./scripts/invoke-aspire-commercial-secrets.sh` | Set AppHost `Parameters:*` for commercial mode and Stripe via `aspire secret` |
| `./scripts/invoke-stripe-mock.sh` | Start/stop Docker `stripe/stripe-mock` and run opt-in contract tests |
| `./scripts/invoke-hugo-site.sh` | Build or preview the public Hugo site in a container |

PowerShell equivalents: `Invoke-AspireCommercialSecrets.ps1`, `Invoke-StripeMock.ps1`, `Invoke-HugoSite.ps1`.

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
| `OfficialReleaseBuild` | Set to `true` only when building from an annotated `vX.Y.Z` tag so About may show the shipping label (for example `v1.0.0`); leave unset on local, PR, CI, and staging builds |

Example local build with metadata (for validating About diagnostics):

```bash
dotnet build ImportToPlanner.slnx \
  /p:SourceRevisionId="$(git rev-parse --short HEAD)" \
  /p:BuildTimestampUtc="$(date -u +"%Y-%m-%dT%H:%M:%SZ")" \
  /p:ContinuousIntegrationBuild=true
```

Example tagged production build (after checking out `vX.Y.Z`):

```bash
dotnet build ImportToPlanner.slnx \
  /p:OfficialReleaseBuild=true \
  /p:SourceRevisionId="$(git rev-parse HEAD)" \
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
