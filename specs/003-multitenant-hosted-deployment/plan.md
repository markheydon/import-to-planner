# Implementation Plan: Multi-Tenant Hosted Deployment

**Branch**: `003-add-multitenant-deployment` | **Date**: 2026-05-16 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/003-multitenant-hosted-deployment/spec.md`

## Summary

This feature adds the deployment and operational design for a shared hosted multi-tenant
version of Import To Planner while preserving the existing self-hosted single-tenant mode.
The initial hosted reference architecture stays intentionally small: Aspire continues to
model a single `web` resource, Azure Container Apps provides the public hosted compute
target, one shared `beta` environment receives GitHub Actions deployments automatically,
and a separate production environment is promoted manually after verification. Persistent
hosted metadata is limited to a minimal tenant record stored in low-cost Azure Table
Storage, while secrets remain platform-managed and OpenTelemetry reuses the existing OTLP
path before any paid monitoring expansion is considered.

## Technical Context

**Language/Version**: C# 14 / .NET 10 with .NET Aspire AppHost SDK 13.3.0  
**Primary Dependencies**: ASP.NET Core Blazor Web App, Microsoft Identity Web,
Microsoft Graph, .NET Aspire, OpenTelemetry, Azure Container Apps deployment tooling  
**Storage**: Existing app remains stateless for import content; hosted mode adds minimal
tenant metadata in Azure Table Storage  
**Testing**: Existing CI validation (`dotnet restore`, `dotnet format --verify-no-changes`,
`dotnet build`, `dotnet restore apphost.cs`, `dotnet build apphost.cs`, `dotnet test`) plus
hosted smoke checks after deployment  
**Target Platform**: Azure-hosted shared web application plus self-hosted deployments  
**Project Type**: Blazor web application with Aspire-managed hosting topology and GitHub
Actions delivery  
**Performance Goals**: Preserve current validation/preview/reporting reliability while
keeping hosted infrastructure on low-cost, consumption-oriented services  
**Constraints**: UK English documentation, least-cost Azure choices first, tenant
isolation, minimal stored metadata, no unnecessary always-on infrastructure, AppHost
should remain a single `web` resource until measurable demand justifies expansion  
**Scale/Scope**: One shared hosted deployment path (`beta` and `production`) and one
preserved self-hosted deployment path

## Constitution Check

*GATE: Pre-phase assessment — all gates pass.*

- **Dependency Direction Gate**: PASS. This planning work is documentation-only. The target
  design keeps tenant-aware policy decisions in application and infrastructure boundaries
  rather than pushing them into the Aspire AppHost.
- **Inner-Layer Purity Gate**: PASS. Hosted deployment choices are documented as outer-layer
  concerns. No new framework or Azure concepts are introduced into Domain or Application
  layers by this planning change.
- **Boundary Contract Gate**: PASS. The plan explicitly separates deployment-mode
  configuration, tenant metadata persistence, and telemetry concerns from core import
  workflow behaviour.
- **Replaceability Gate**: PASS. Azure Container Apps, Table Storage, Key Vault, and OTLP
  reuse are documented as the initial hosted adapter choices, not constitutional
  requirements. Future replacements remain possible at the infrastructure boundary.
- **Architecture Evidence Gate**: PASS. This plan requires future implementation work to
  preserve the current single `web` AppHost shape initially and to document any later
  resource expansion with explicit approval.
- **Policy Alignment Gate (Non-Constitutional)**: PASS. The plan preserves current runtime
  behaviour, keeps tenant-sensitive values out of broad telemetry, and defines hosted
  operational checks and deployment evidence requirements.

## Project Structure

### Documentation (this feature)

```text
specs/003-multitenant-hosted-deployment/
├── spec.md              # Approved feature requirements
├── plan.md              # This hosted deployment and operations plan
└── quickstart.md        # Hosted verification guide
```

### Source Code (repository root)

```text
apphost.cs

src/
├── ImportToPlanner.Application/
├── ImportToPlanner.Domain/
├── ImportToPlanner.Infrastructure.Graph/
├── ImportToPlanner.ServiceDefaults/
└── ImportToPlanner.Web/

docs-internal/
└── aspire-production-readiness.md

.github/workflows/
└── ci.yml
```

**Structure Decision**: Keep the current repository structure and minimal AppHost. Use
feature documentation to define the hosted topology first, then let later implementation
work evolve the AppHost, delivery workflow, and infrastructure adapters only after the
decision gates in this plan are approved.

## Hosted Reference Architecture

### Deployment modes

| Mode | Purpose | Identity boundary | Recommended hosting |
| --- | --- | --- | --- |
| Self-hosted single-tenant | Existing organisation-run deployment | Restricted to one configured Entra tenant | Existing self-hosted model remains supported |
| Shared hosted multi-tenant | Repository reference deployment for customer tenants | Multiple Microsoft 365 work or school tenants, isolated per signed-in tenant context | Azure Container Apps, modelled through Aspire |

### Initial Azure hosted topology

| Concern | Decision | Cost / scope rationale |
| --- | --- | --- |
| Public endpoint | Azure Container Apps ingress for the single `web` app | Lowest-complexity public entry point; no Front Door or Application Gateway until required |
| App compute | One Azure Container Apps app for the Blazor web resource | Consumption-oriented baseline and Aspire-friendly deployment target |
| AppHost topology | Keep Aspire AppHost scoped to a single `web` resource initially | Avoid premature infrastructure growth while the hosted model is still being proven |
| Tenant metadata persistence | One Azure Storage account with Azure Table Storage for minimal tenant records | Low-cost persistence without introducing a full database platform |
| Secrets and app settings | Container Apps environment configuration and secrets | Platform-managed settings are cheaper and simpler than introducing extra config services |
| Certificate handling | Use Key Vault only when certificate-backed confidential client auth genuinely requires it | Avoids Key Vault baseline cost where a simpler secret-backed path is acceptable |
| Telemetry export | Reuse the existing OTLP endpoint or existing collector first | Avoids paying for extra always-on monitoring services before they are needed |

### Minimal tenant metadata record

Hosted mode may persist a single minimal record per tenant in Azure Table Storage. The
record is the only planned hosted persistence in this feature and is limited to:

- tenant identifier
- pseudonymous tenant correlation value for support workflows
- deployment mode or hosted enablement status
- consent status
- first-seen timestamp
- last-sign-in timestamp
- last-consent-check timestamp
- last-operational-event timestamp

Imported CSV content, preview content, execution payloads, future entitlement placeholders,
and general customer business data remain out of scope for persistence in this feature.

### Tenant-aware configuration and telemetry

- Environment-wide configuration remains in platform-managed settings on each Container
  Apps environment, separated between `beta` and `production`.
- Tenant-aware runtime context comes from the signed-in tenant plus the minimal tenant
  record in Table Storage; it does not require Azure App Configuration in the first hosted
  release.
- Operational telemetry must emit a pseudonymous tenant correlation value, not a broadly
  visible raw tenant identifier.
- Support resolution from correlation value back to the affected tenant must be restricted
  to an access-controlled support path backed by the tenant metadata record.

## Aspire Topology and Evolution Plan

### Current and approved initial state

- `apphost.cs` continues to define only one `web` resource.
- The hosted deployment target is still the web application only; no database, cache,
  queue, worker, or gateway resource is added to the AppHost yet.
- Aspire remains responsible for local orchestration, deployment modelling, and later
  hosted evolution, but not for forcing premature resource sprawl.

### Deferred resources

The following are explicitly deferred unless later evidence shows they are required:

- Front Door or Application Gateway
- Redis or any other cache
- queue or background worker resources
- always-on relational database resources
- dedicated monitoring stack beyond the existing OTLP path

### Allowed future placeholders

The following may be introduced in a later approved phase if implementation evidence shows
they are needed:

- a named Aspire storage resource representing tenant metadata persistence
- a dedicated ingress or edge resource if public endpoint or custom-domain needs outgrow
  direct Container Apps ingress
- additional telemetry pipeline resources if the existing OTLP path is insufficient

## Environment and Promotion Model

### Environment choice

Use `beta` as the single shared non-production environment name and apply it consistently
across documentation, GitHub Actions environments, and Azure naming.

### Environment responsibilities

| Environment | Purpose | Deployment path | Isolation expectations |
| --- | --- | --- | --- |
| `beta` | Shared pre-production validation for multi-tenant hosted changes | Automatic deployment from GitHub Actions after successful CI | Separate Azure resources, identity settings, and secrets from production |
| `production` | Official customer-facing hosted service | Manual promotion after `beta` verification and approval | Fully isolated configuration, secrets, telemetry routing, and release approvals |

### Promotion policy

1. GitHub Actions continues to enforce build and test validation before any hosted
   deployment step.
2. Successful changes deploy automatically to `beta`.
3. `beta` must pass smoke checks for sign-in, validation, preview, import execution path,
   consent guidance, and telemetry flow before promotion is considered.
4. Promotion to `production` is a separate manual release step with explicit approval.
5. `production` secrets, Entra registrations, redirect URIs, certificate material, and
   telemetry settings remain isolated from `beta`.

## CI/CD Strategy

### Validation baseline

The current CI workflow already provides the baseline gate and remains mandatory:

- `dotnet restore ImportToPlanner.slnx`
- `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`
- `dotnet build ImportToPlanner.slnx --no-restore`
- `dotnet restore apphost.cs`
- `dotnet build apphost.cs --no-restore`
- `dotnet test ImportToPlanner.slnx --no-build`

### Planned hosted delivery flow

1. CI validates the solution and AppHost exactly as above.
2. A deployment workflow packages and deploys the Aspire-modelled web app to `beta`.
3. `beta` smoke checks confirm sign-in, hosted tenant isolation, validation/preview flow,
   admin-consent guidance path, and telemetry emission.
4. A manual approval gate promotes the verified release to `production`.

### Rollback expectation

- `beta` rollback may redeploy the previously known-good revision automatically or through a
  documented operator action.
- `production` rollback must be a separate controlled step that restores the previous known
  good revision and preserves secret/configuration isolation.
- Rollback planning must assume the app remains stateless for workflow payloads and that
  tenant metadata schema changes must be backwards-compatible or explicitly versioned.

### Configuration ownership

| Configuration area | Owner expectation |
| --- | --- |
| Build/test workflow definition | Repository maintainers via GitHub Actions |
| `beta` application settings and secrets | Deployment owner for non-production |
| `production` application settings and secrets | Production service owner |
| Entra app registrations and redirect URIs | Identity owner / deployment owner |
| Certificate source and rotation process | Security or deployment owner |
| OTLP endpoint and telemetry routing | Operations owner |

## Hosted Operations

### Post-deployment health and smoke checks

For both `beta` and `production`, the hosted verification baseline is:

1. confirm the deployed web endpoint responds successfully
2. confirm sign-in redirects and callback handling work
3. confirm a hosted tenant can load containers and plans
4. confirm validation and preview still behave predictably
5. confirm execution reporting still behaves predictably
6. confirm consent-missing scenarios show the correct in-app administrator path
7. confirm telemetry reaches the configured OTLP endpoint with tenant correlation present

### Tenant-aware incident investigation

- Investigate by workflow stage first: sign-in, consent, validation, preview, execution, or
  reporting.
- Use pseudonymous tenant correlation from telemetry to isolate the affected tenant.
- Resolve correlation to the underlying tenant only through the controlled support path.
- Do not rely on raw tenant identifiers in standard dashboards, logs, or alerts.

### Consent and administrator-consent troubleshooting

- Hosted users who lack required consent must receive an in-app explanation and an
  administrator path without exposing internal configuration details.
- Operations guidance must distinguish user-actionable failures from administrator-only
  consent actions.
- `beta` must be the first place where new consent-path behaviour is exercised before any
  production rollout.

### Support and privacy boundaries

- Support staff may inspect workflow-stage outcomes and pseudonymous tenant correlation.
- Support staff must not require access to imported task content because that content is not
  stored by the hosted service.
- Tenant identifiers, certificates, and secret values must stay out of broad operational
  logs and out of repository-managed configuration files.

## Cost Control Rules

- Prefer Azure Container Apps consumption-oriented hosting over higher-baseline services.
- Keep the AppHost to a single `web` resource until measurable demand proves a new resource
  is necessary.
- Use Azure Table Storage for minimal tenant metadata instead of an always-on relational
  database.
- Reuse the existing OTLP collector or endpoint before adding Azure Monitor or other paid
  telemetry components.
- Introduce Key Vault only when certificate handling or secret governance genuinely requires
  it.
- Keep environment count to `beta` and `production` only for the first hosted release.

## Documentation Deliverables

- hosted Azure deployment strategy
- environment and promotion model
- Aspire AppHost evolution plan
- configuration and secret ownership
- monitoring and tenant-correlation approach
- cost assumptions and scale-out triggers
- hosted smoke-check and support runbook guidance

## Decision Gates

The following decisions must be explicitly approved before implementation expands beyond the
current AppHost and documentation scope:

1. approve Azure Container Apps plus Table Storage as the hosted reference architecture
2. approve `beta` plus `production` as the initial environment model
3. approve the minimal tenant metadata shape and storage location
4. approve pseudonymous tenant correlation and the restricted support-resolution path
5. approve the hosted CI/CD promotion and rollback model
6. only after those approvals, evolve the AppHost and deployment workflows

## Complexity Tracking

No constitution violations or complexity exceptions are introduced by this documentation
change.
