# Implementation Plan: Commercial User Accounts

**Branch**: `008-commercial-user-accounts` | **Date**: 2026-05-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-commercial-user-accounts/spec.md`

## Summary

Introduce a commercial-only account flow that sits alongside the existing
self-hosted sign-in path without regressing self-hosted behaviour. The planned
implementation adds an explicit commercial-mode parameter in Aspire and the
staging deployment workflow, keeps mode selection as an outer-layer concern,
persists commercial account and audit records in Azure Table Storage using the
existing storage account, and defers Azure Functions to an optional follow-on
resource until scheduled retention and credits work justify a separate compute
surface.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK 10.0.100 via `global.json`)  
**Primary Dependencies**: Blazor Interactive Server, MudBlazor,
Microsoft.Identity.Web, Microsoft.Graph, Aspire AppHost SDK, Aspire Azure Blob
and Table client integrations, Azure.Data.Tables, OpenTelemetry, xUnit, bUnit  
**Storage**: Commercial account state and audit events stored in Azure Table
Storage tables within the existing shared Azure Storage account; Blob storage
continues to hold ASP.NET Core data-protection keys only  
**Testing**: xUnit unit and integration-style tests in `ImportToPlanner.Tests`,
bUnit/component tests in `ImportToPlanner.Web.Tests`, plus architecture
compliance tests and deployment-mode parity checks for authentication flow  
**Target Platform**: Blazor Server web application orchestrated locally by
Aspire and deployed to Linux-hosted Azure Container Apps, with desktop and
mobile browser support preserved  
**Project Type**: Layered web application (`Web`, `Infrastructure`,
`Application`, `Domain`) with an Aspire AppHost and GitHub Actions deployment
workflow  
**Performance Goals**: Keep first commercial sign-in and restore flows within
the existing interactive web latency envelope, avoid avoidable extra Graph or
storage round-trips during access checks, and retain scale-to-zero capability in
non-production  
**Constraints**: Preserve clean-architecture dependency direction; keep
commercial/self-hosted selection out of Domain and Application; do not add new
databases beyond the existing Azure Storage account; restrict persistence
choices to Azure Tables or Blobs already modelled in the app; keep user-facing
wording in UK English; preserve self-hosted viability; do not introduce
scheduled compute unless clearly justified  
**Scale/Scope**: One hosted commercial mode plus one self-hosted mode, one AppHost
with the existing `web`, `storage`, `blobs`, `dataprotection`, and `tables`
resources, and focused changes across AppHost, Web auth/UI, Application
account-lifecycle use cases, Infrastructure storage adapters, tests, and
deployment configuration

## Constitution Check

*GATE: Pre-phase assessment passes. Re-check after Phase 1 design below.*

- **Dependency Direction Gate**: Planned changes preserve `Web/Infrastructure ->
  Application -> Domain`. Commercial-mode configuration, Microsoft identity,
  Aspire resource modelling, and Azure Storage adapters remain in outer layers.
- **Inner-Layer Purity Gate**: Application and Domain use repository-owned
  account, audit, retention, and access-decision types only. No Microsoft,
  Aspire, or Azure SDK types cross into inner layers.
- **Boundary Contract Gate**: Account resolution, deletion, restoration, audit
  recording, and retention purge are modelled through explicit application
  contracts and documented adapter responsibilities.
- **Replaceability Gate**: Azure Table Storage, Blob storage, Microsoft
  identity, Aspire, GitHub Actions, and any future scheduled compute remain
  replaceable outer-layer delivery choices.
- **Architecture Evidence Gate**: Implementation must extend architecture
  compliance tests, add focused use-case and UI tests, and prove commercial-mode
  changes do not regress self-hosted sign-in behaviour.
- **Self-Hosted Viability Gate**: Commercial login, profile, audit, and retention
  work are additive. Self-hosted deployments continue using the current
  Microsoft 365 sign-in path without hosted-only gating.
- **Policy Alignment Gate (Non-Constitutional)**: The plan aligns with
  `docs-internal/engineering-policies.md`, `tests/README.md`, and `AGENTS.md`:
  smallest-practical tests first, UK English wording, privacy-safe diagnostics,
  and explicit self-hosted impact documentation.

## Project Structure

### Documentation (this feature)

```text
specs/008-commercial-user-accounts/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── commercial-account-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.AppHost/
│   └── AppHost.cs
├── ImportToPlanner.Application/
│   ├── Abstractions/
│   │   ├── ICurrentTenantContextAccessor.cs
│   │   ├── [new commercial account lifecycle contracts]
│   │   └── [new audit and purge contracts]
│   ├── Models/
│   │   └── [new commercial account, access decision, and audit models]
│   └── Services/
│       └── [new use-case services for create/delete/restore/access checks]
├── ImportToPlanner.Domain/
│   └── [no provider-specific types introduced; domain changes optional and minimal]
├── ImportToPlanner.Infrastructure.Graph/
│   ├── DependencyInjection.cs
│   └── [new Azure Table adapters for accounts and audit records]
└── ImportToPlanner.Web/
    ├── Program.cs
    ├── DependencyInjection.cs
    ├── Components/
    │   ├── Layout/
    │   └── Pages/
    └── [new commercial access-mode options, presenters, and profile UI]

.github/
└── workflows/
    └── deploy-staging.yml

tests/
├── ImportToPlanner.Tests/
│   ├── ArchitectureComplianceTests.cs
│   └── [new application and storage adapter tests]
└── ImportToPlanner.Web.Tests/
    └── [new login-gate, profile, and self-hosted parity tests]
```

**Structure Decision**: Keep the current four-project solution plus the existing
AppHost and deployment workflow. Commercial account persistence is implemented as
new outer-layer storage adapters within the existing Infrastructure project,
with application use cases added behind explicit interfaces. No new database
project is introduced. An Azure Functions project is deliberately deferred from
the baseline implementation, but the AppHost design leaves room to add one later
for scheduled retention or credits work.

## Complexity Tracking

No constitution gate violations are planned. No complexity override is required.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Commercial versus self-hosted behaviour is selected by an explicit outer-layer
   commercial-mode option supplied from Aspire and deployment configuration, not
   by reintroducing a cross-layer deployment-mode abstraction.
2. Azure Table Storage is the baseline persistence for both commercial account
   records and audit records because the data is structured, queryable, and
   already aligned with existing storage usage.
3. Blob storage remains limited to data-protection keys and other opaque blob
   use cases; it is not the preferred account store for this feature.
4. A single referenced `tables` resource is sufficient; keyed storage clients are
   optional and only needed if multiple independently configured table resources
   appear later.
5. Commercial retention policy is expressed in application use cases, while the
   first implementation can execute expiry cleanup through a web-hosted scheduled
   service; Azure Functions remains an optional follow-on when scheduled work
   becomes broader or operationally separate.
6. The staging deployment workflow must pass the new commercial-mode parameter so
   hosted environments can enable or disable the feature without code changes.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md),
and [contracts/commercial-account-contracts.md](contracts/commercial-account-contracts.md).

Key design outcomes:

- The AppHost adds a non-secret commercial-mode parameter and forwards it into
  the web project, while the staging workflow passes the corresponding parameter
  through environment variables.
- Commercial account lifecycle, audit emission, and retention-state decisions are
  represented as application use cases with repository-owned request/response
  contracts.
- Azure Table Storage holds both account and audit records in separate tables on
  the existing storage account, preserving Blob storage for data protection only.
- Self-hosted behaviour remains the regression baseline: the web layer branches
  between commercial login-gate behaviour and the existing automatic sign-in
  path without changing the rest of the import workflow semantics.
- Scheduled purge work is planned so it can start as a commercial-only hosted
  service inside the web app and later move to Azure Functions without breaking
  the core account model or storage contracts.

### Post-design Constitution Check

- **Dependency Direction Gate**: Pass. AppHost, workflow, auth, and storage
  details remain outside Application and Domain.
- **Inner-Layer Purity Gate**: Pass. Account, audit, and retention models are
  repository-owned and provider-neutral.
- **Boundary Contract Gate**: Pass. Access checks, profile retrieval, deletion,
  restoration, and retention purge are documented as explicit boundaries.
- **Replaceability Gate**: Pass. Azure Tables, future Azure Functions, and
  Aspire parameter wiring are outer-layer concerns only.
- **Architecture Evidence Gate**: Pass. The design defines targeted unit,
  adapter, component, and parity tests together with architecture compliance
  extensions.
- **Self-Hosted Viability Gate**: Pass. The self-hosted path keeps its current
  sign-in semantics and is not blocked by commercial account features.
- **Policy Alignment Gate**: Pass. The design keeps minimal persisted identity
  data, privacy-safe diagnostics, UK English wording, and staged rollout control.
