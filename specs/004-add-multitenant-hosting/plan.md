# Implementation Plan: Hosted Multi-Tenant Support

**Branch**: `004-add-multitenant-hosting` | **Date**: 2026-05-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-add-multitenant-hosting/spec.md`

## Summary

Add a deployment-mode-aware identity and hosting design that keeps the current
self-hosted single-tenant experience intact while enabling a shared hosted
multi-tenant deployment for Microsoft 365 work or school tenants. The planned
implementation keeps delegated Microsoft Graph access on the signed-in user's
authority, introduces tenant-aware consent guidance and minimal tenant-scoped
operational metadata, and uses Aspire for both local orchestration and Azure
deployment with the lowest-cost credible hosted baseline: a single Azure
Container Apps web resource plus one Azure Storage account for tenant metadata
and ASP.NET Core data-protection persistence.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK 10.0.100 via `global.json`)  
**Primary Dependencies**: Blazor Interactive Server, MudBlazor, Microsoft.Identity.Web,
Microsoft.Graph, Aspire AppHost SDK, OpenTelemetry, CsvHelper, xUnit, bUnit,
plus planned Azure Storage integration packages for hosted metadata and data
protection persistence  
**Storage**: Self-hosted mode adds no new backing store; hosted mode uses one
low-cost Azure Storage account, with Table Storage for tenant-scoped operational
metadata and Blob storage for ASP.NET Core data-protection keys. No per-user
usage history, import-content history, or retained preview/report history is
persisted  
**Testing**: xUnit unit and integration-style tests in `ImportToPlanner.Tests`,
bUnit component/workflow tests in `ImportToPlanner.Web.Tests`, plus runtime-mode
verification for affected planner behaviour in both `PlannerGateway:UseGraph`
modes  
**Target Platform**: Blazor Server web application running locally through Aspire
and deployed through Aspire to Linux-hosted Azure Container Apps, with desktop
and mobile browser support preserved for the primary workflow
**Project Type**: Layered web application (`Web`, `Infrastructure`,
`Application`, `Domain`) with an Aspire AppHost at the repository root  
**Performance Goals**: Preserve current validation, preview, confirmation, and
execution responsiveness; avoid avoidable extra Graph round-trips during tenant
resolution and consent handling; keep the first hosted rollout scale-to-zero
capable with `minReplicas=0` and an initial single active web replica to contain
cost and operational complexity  
**Constraints**: Preserve clean-architecture dependency direction; keep inner
layers technology-neutral; use UK English for user and contributor wording;
reject unsupported account types before workflow entry; retain only minimal
tenant-scoped metadata; keep hosted telemetry privacy-safe; keep AppHost changes
compatible with current Aspire usage; prefer least-cost Azure resources and
avoid unjustified always-on services such as Redis, SQL, or separate queues  
**Scale/Scope**: One shared hosted deployment admitting supported work or school
tenants on first use, one self-hosted deployment mode with existing single-
tenant behaviour, one AppHost with a single web resource plus storage-backed
hosted support resources, and focused changes across Web auth/workflow surfaces,
Application contracts, Infrastructure adapters, tests, and deployment
documentation

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below.*

- **Dependency Direction Gate**: Planned changes preserve `Web/Infrastructure ->
  Application -> Domain`. Authentication, Aspire hosting, Azure Storage, and
  OpenTelemetry wiring stay in outer layers; Application only receives explicit
  tenant and consent contracts.
- **Inner-Layer Purity Gate**: Domain/Application will use repository-owned
  tenant, consent, and operational-metadata concepts only. Microsoft.Identity,
  Graph SDK, Azure Storage, and UI wording remain outside inner layers.
- **Boundary Contract Gate**: The feature introduces explicit contracts for
  deployment mode, current tenant context, tenant-scoped metadata, and consent
  outcomes, together with documented mapping responsibilities for Web and
  Infrastructure adapters.
- **Replaceability Gate**: Aspire, Microsoft.Identity.Web, Azure Container Apps,
  Azure Storage, and OpenTelemetry remain replaceable delivery concerns rather
  than architectural invariants.
- **Architecture Evidence Gate**: Implementation must provide dependency-
  direction evidence, forbidden-reference checks for Domain/Application,
  runtime-mode parity evidence where planner behaviour changes, and focused tests
  for tenant isolation, consent guidance, and deployment-mode behaviour.
- **Policy Alignment Gate (Non-Constitutional)**: The plan captures repository
  requirements from `docs-internal/engineering-policies.md`,
  `docs-internal/aspire-production-readiness.md`, `tests/README.md`, and
  `AGENTS.md`: smallest-practical tests first, dual runtime-mode verification
  when planner behaviour changes, UK English wording, privacy-safe diagnostics,
  staged hosted rollout, and low-cost Aspire-compatible Azure resource choices.

## Project Structure

### Documentation (this feature)

```text
specs/004-add-multitenant-hosting/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── hosted-tenant-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
apphost.cs                                              ← extend local/publish resource graph for hosted mode
src/
├── ImportToPlanner.Application/
│   ├── Abstractions/
│   │   ├── [current use-case contracts]
│   │   ├── ICurrentTenantContextAccessor.cs            ← new
│   │   └── ITenantOperationalMetadataStore.cs          ← new
│   ├── Models/
│   │   ├── TenantContext.cs                            ← new
│   │   ├── TenantOperationalMetadata.cs                ← new
│   │   └── ConsentResolution.cs                        ← new
│   └── Services/
│       └── [workflow/use-case services updated for deployment mode and consent handling]
├── ImportToPlanner.Domain/
│   └── [no provider-specific types introduced; domain remains tenant-neutral]
├── ImportToPlanner.Infrastructure.Graph/
│   ├── GraphPlannerGateway.cs                          ← preserve delegated user token behaviour
│   ├── DependencyInjection.cs                          ← hosted metadata and storage adapters wired here
│   └── TenantMetadata/
│       ├── TableTenantOperationalMetadataStore.cs      ← new
│       └── [mapping/support types]                     ← new
└── ImportToPlanner.Web/
    ├── Program.cs                                      ← deployment-mode, auth, data-protection wiring
    ├── DependencyInjection.cs                          ← auth events, token acquisition, consent services
    ├── MicrosoftIdentityAccessTokenProvider.cs         ← retain delegated Graph token acquisition
    ├── Components/
    │   └── [pages/components updated for hosted consent and unsupported-account guidance]
    ├── Presenters/
    │   └── [tenant-aware user messaging updates]
    └── Workflows/
        └── [existing workflow coordination updated for hosted/self-hosted parity]

tests/
├── ImportToPlanner.Tests/
│   ├── GraphPlannerGatewayTests.cs                     ← extend only if adapter behaviour changes
│   ├── [new tenant metadata / consent / deployment-mode tests]
│   └── ArchitectureComplianceTests.cs                  ← extend forbidden-reference evidence if needed
└── ImportToPlanner.Web.Tests/
    └── [workflow and sign-in guidance tests for both deployment modes]
```

**Structure Decision**: Keep the existing four-project solution and single AppHost.
Hosted metadata storage is implemented as another outer-layer adapter inside the
current Infrastructure project rather than introducing a fifth top-level project.
The AppHost remains centred on one `web` resource, adding only the minimum local
and publish-time support resources required for hosted mode.

## Complexity Tracking

No constitution gate violations are planned. No complexity override is required.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Hosted shared deployment uses a work-or-school-only multi-tenant authority,
   while self-hosted remains single-tenant by configuration.
2. Tenant context is established from sign-in claims and never reuses another
   tenant's metadata automatically.
3. Hosted consent handling uses delegated consent first and an administrator
   fallback path when tenant policy requires it.
4. Hosted persistence remains minimal: tenant-scoped configuration, consent
   state, and support diagnostics only.
5. Hosted Azure footprint stays low-cost: Azure Container Apps plus a single
   Azure Storage account, orchestrated through Aspire.
6. Data-protection persistence uses Blob storage from that same account; Key
   Vault remains optional rather than baseline.
7. The initial hosted rollout stays single-replica by design to avoid
   introducing an unjustified always-on distributed token-cache service.
8. OpenTelemetry stays the monitoring path, enriched with tenant-safe hosted
   context.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md),
and [contracts/hosted-tenant-contracts.md](contracts/hosted-tenant-contracts.md).

Key design outcomes:

- Deployment mode is an explicit boundary concern. Web and Infrastructure own
  authority selection, Aspire resource wiring, and Azure configuration mapping.
- Application receives explicit tenant-context, consent-state, and operational-
  metadata abstractions without taking framework or provider dependencies.
- Hosted persistence is intentionally minimal and tenant-scoped, using Table
  Storage for configuration/consent/diagnostics and Blob storage for data-
  protection keys within the same storage account.
- The hosted rollout is designed around one active web replica initially, which
  satisfies the low-cost requirement and postpones Redis or SQL token-cache
  services until measurable demand justifies them.
- Manual and automated verification are split by deployment mode so self-hosted
  single-tenant behaviour remains the regression baseline while hosted
  multi-tenant behaviour gains targeted tenant-isolation and consent coverage.

### Post-design Constitution Check

- **Dependency Direction Gate**: Pass. Identity, storage, telemetry, and Aspire
  concerns remain in Web/Infrastructure/AppHost. Application contracts depend
  only on repository-owned abstractions.
- **Inner-Layer Purity Gate**: Pass. Domain/Application models describe tenant
  context, consent state, and operational metadata without Microsoft or Azure
  SDK types.
- **Boundary Contract Gate**: Pass. Current-tenant access, tenant metadata
  storage, consent resolution, and hosted configuration mapping are explicitly
  documented in the contracts artefact.
- **Replaceability Gate**: Pass. Azure Container Apps, Azure Storage,
  Microsoft.Identity.Web, and OTLP routing remain outer-layer choices.
- **Architecture Evidence Gate**: Pass. The quickstart and design artefacts
  define architecture checks, planner runtime-mode parity checks, hosted/self-
  hosted test slices, and tenant-isolation evidence.
- **Policy Alignment Gate**: Pass. The design preserves workflow semantics, UK
  English wording, privacy-safe diagnostics, minimal hosted data retention, and
  the repository's staged Aspire rollout expectations.
