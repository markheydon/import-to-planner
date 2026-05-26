# Implementation Plan: Simplify Graph Runtime Path

**Branch**: `005-simplify-graph-path` | **Date**: 2026-05-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-simplify-graph-path/spec.md`

## Summary

Remove the pre-production scaffolding introduced before Graph integration and
multi-tenant support were fully wired, so the app runs through one supported
planner path and one supported tenant-metadata path with no runtime mode
switching. The implementation replaces the root experimental Aspire script with
a conventional `ImportToPlanner.AppHost` project, makes Azure Storage an
unconditional Aspire resource for both local and hosted execution, moves
authority classification fully into the Web layer based on `AzureAd:TenantId`,
renames `HostedStorage:*` to `Storage:*`, and removes Application-layer
deployment-topology models in favour of smaller Web-owned configuration and
boundary services.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK 10.0.100 via `global.json`)  
**Primary Dependencies**: Blazor Interactive Server, MudBlazor,
Microsoft.Identity.Web, Microsoft.Graph, Aspire.AppHost.Sdk, Azure Storage
Blobs/Tables SDKs, OpenTelemetry service defaults, CsvHelper, xUnit, bUnit  
**Storage**: Azure Storage is always available; Azurite via Aspire emulator for
local development, Azure Blob Storage for Data Protection key persistence, and
Azure Table Storage for tenant operational metadata  
**Testing**: xUnit unit and integration-style tests in `ImportToPlanner.Tests`,
bUnit/web-host tests in `ImportToPlanner.Web.Tests`, plus focused architecture
compliance and startup-validation coverage  
**Target Platform**: Linux-hosted ASP.NET Core web app orchestrated locally and
in deployment through Aspire, with desktop and mobile browser support for the
existing workflow  
**Project Type**: Layered Blazor web application with an Aspire AppHost project  
**Performance Goals**: Preserve current validation, preview, confirmation, and
execution responsiveness; add no avoidable extra Graph calls during tenant or
consent checks; keep local startup simple enough for clone -> secrets ->
`aspire run` without manual emulator setup  
**Constraints**: Preserve dependency direction; keep deployment topology out of
Application/Domain; no AppHost `if` statements or environment-variable reads; no
runtime planner/storage mode flags; use human-friendly fail-fast startup errors
for missing or obsolete configuration; keep AzureAd configuration owned by the
Web project; keep Key Vault out of scope  
**Scale/Scope**: One Web project, one new AppHost project, one Azure Storage
resource graph (`storage` with blob and table references), one supported Graph
planner implementation, one supported table-backed metadata implementation, and
targeted updates across configuration, tests, and developer documentation

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below.*

- **Dependency Direction Gate**: Planned changes remove the known violation by
  deleting `DeploymentMode` and `DeploymentModeConfiguration` from Application.
  Web owns authority classification, human-friendly startup validation, and auth
  event decisions; Infrastructure owns Graph and Azure Storage adapters; Domain
  stays unchanged.
- **Core Policy Neutrality Gate**: Inner-layer logic will no longer carry hosting
  topology, storage enablement flags, or AppHost decisions. Application will
  depend only on repository-owned tenant, consent, planner, and metadata
  abstractions.
- **Boundary Explicitness Gate**: Planning introduces explicit contracts for Web
  authority classification, storage settings, consent resolution input, and
  planner/metadata test doubles. Human-facing wording remains in Web/presenter
  layers.
- **Replaceability Gate**: Aspire, Microsoft.Identity.Web, Azure Storage, and
  Graph remain adapter concerns. The new AppHost project formalises orchestration
  without making it an inner-layer invariant.
- **Architecture Evidence Gate**: Implementation must provide forbidden-reference
  evidence for Application/Domain, focused startup-validation tests for missing
  and removed keys, DI evidence that no runtime planner/storage branching
  remains, and regression tests for retained auth guard behaviour.
- **Policy Alignment Gate (Non-Constitutional)**: The plan preserves graceful
  user-facing failures, UK English wording, existing workflow semantics, and the
  repository's requirement to keep secrets out of source control by moving local
  AzureAd setup to `dotnet user-secrets --project src/ImportToPlanner.Web`.

## Project Structure

### Documentation (this feature)

```text
specs/005-simplify-graph-path/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── runtime-cleanup-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
ImportToPlanner.AppHost/
├── ImportToPlanner.AppHost.csproj                ← new conventional Aspire AppHost project
└── Program.cs                                    ← unconditional storage + web resource graph

src/
├── ImportToPlanner.Application/
│   ├── Abstractions/
│   │   └── [planner, tenant metadata, tenant context, consent contracts]
│   ├── Models/
│   │   └── TenantContext.cs                      ← remove deployment-mode field
│   └── Services/
│       └── ImportPlanningUseCase.cs              ← remove deployment-mode branching
├── ImportToPlanner.Infrastructure.Graph/
│   ├── DependencyInjection.cs                    ← register Graph + table store unconditionally
│   ├── GraphPlannerGateway.cs                    ← retained production gateway
│   └── TenantMetadata/
│       └── TableTenantOperationalMetadataStore.cs← retained production metadata store
├── ImportToPlanner.ServiceDefaults/
│   └── [unchanged unless AppHost wiring requires small support changes]
└── ImportToPlanner.Web/
    ├── Program.cs                                ← remove deployment-mode synthesis and validate config
    ├── DependencyInjection.cs                    ← auth guards driven by AzureAd authority classification
    ├── ClaimsTenantContextAccessor.cs            ← authority-driven tenant handling
    ├── MicrosoftIdentityAccessTokenProvider.cs   ← remove self-hosted mode branching
    ├── HostedDataProtectionConfigurator.cs       ← always persist keys to blob storage
    ├── appsettings.json                          ← rename HostedStorage:* to Storage:*
    └── appsettings.Development.json              ← remove mode flags and dev-only hosted toggles

tests/
├── ImportToPlanner.Tests/
│   ├── [use-case tests updated to use boundary doubles]
│   └── ArchitectureComplianceTests.cs            ← extend forbidden-reference coverage if needed
└── ImportToPlanner.Web.Tests/
    ├── [startup/auth/data-protection tests updated for authority-based behaviour]
    └── TestInfrastructure/
        └── [shared planner and metadata doubles]
```

**Structure Decision**: Keep the existing layered solution and add a single
conventional Aspire AppHost project at the repository root. This is the minimum
structural change that replaces the experimental root script, keeps orchestration
separate from the Web app, and avoids introducing new application layers or
backing services.

## Complexity Tracking

No constitution violations are planned. The additional AppHost project is needed
to replace an unsupported experimental script surface with the repository's
conventional project structure, not to add architectural complexity.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Replace the root file-based Aspire script with a conventional
   `ImportToPlanner.AppHost` project and delete `apphost.cs` and
   `aspire.config.json`.
2. Declare the entire Aspire resource graph unconditionally: `storage` with blob
   and table references, plus the `web` project reference chain.
3. Treat AzureAd settings as Web-owned configuration only; the AppHost must not
   forward tenant, client, or certificate settings.
4. Always configure Data Protection key persistence to blob storage and rename
   `HostedStorage:*` to `Storage:*`.
5. Remove Application-layer deployment-topology models and replace them with
   smaller Web-owned authority and consent services.
6. Replace deleted in-memory production implementations in tests with boundary
   doubles or delete tests that only exercised removed runtime paths.
7. Fail startup gracefully and humanely when `AzureAd:TenantId` is missing or
   blank, or when removed planner/storage/deployment-mode keys are still present.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md),
and [contracts/runtime-cleanup-contracts.md](contracts/runtime-cleanup-contracts.md).

Key design outcomes:

- Authority classification is a Web-only concern derived directly from
  `AzureAd:TenantId`; `organizations` implies the retained multi-tenant auth
  guard behaviour and any other supported tenant value implies single-tenant
  behaviour.
- Application no longer receives deployment-topology aggregates. Consent and
  tenant behaviour are exposed through repository-owned abstractions or smaller
  response models rather than a mode record.
- Infrastructure registers the Graph planner gateway and table-backed metadata
  store as the only production implementations, while tests provide explicit
  doubles at the abstraction boundary.
- The new AppHost becomes a stable orchestration entrypoint for local and hosted
  environments with unconditional storage references and no environment-variable
  composition.
- Startup validation becomes a first-class slice: removed keys and missing
  required values fail before sign-in is available, but with a friendly operator
  experience instead of raw framework exception output.

### Post-design Constitution Check

- **Dependency Direction Gate**: Pass. Deployment-topology logic moves out of
  Application; Web and Infrastructure own configuration and adapter decisions.
- **Core Policy Neutrality Gate**: Pass. Application/Domain remain free of Aspire,
  Azure Storage, Microsoft.Identity.Web, and hosting-mode concepts.
- **Boundary Explicitness Gate**: Pass. The contracts artefact documents
  authority classification, storage settings, startup validation, and boundary
  double expectations explicitly.
- **Replaceability Gate**: Pass. The AppHost formalises orchestration without
  coupling inner layers to Aspire-specific APIs.
- **Architecture Evidence Gate**: Pass. The quickstart defines focused checks for
  forbidden references, startup validation, retained auth guards, and AppHost
  storage wiring.
- **Policy Alignment Gate**: Pass. The design preserves graceful failures, UK
  English wording, existing workflow semantics, and approved local secret
  handling.
