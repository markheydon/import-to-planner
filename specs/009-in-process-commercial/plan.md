# Implementation Plan: Isolate Commercial Accounts (Single Hosted Process)

**Branch**: `009-in-process-commercial` | **Date**: 2026-08-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-in-process-commercial/spec.md`, plus GitHub issue [#99](https://github.com/markheydon/import-to-planner/issues/99) for current-`main` file locations and rejected approaches.

**Note**: Coding, architecture, and tests at implement time are delegated to the C# Expert agent (`AGENTS.md`) using `csharp-async`, `csharp-docs`, `csharp-xunit`, and `dotnet-best-practices-repo`. Blazor pages stay presentation-only; MudBlazor skill applies if UI must change (it should not, unless a 008-path defect is found).

## Summary

Keep shipped commercial account behaviour (spec 008 / issue #64) inside a **single `web` process**. Delete the empty `commercialapiservice` leftover from the abandoned web+API split (#57 / #68 / #71). Move commercial account lifecycle, audit, retention policy, table adapters, and table-backed tenant metadata from Application and `Infrastructure.Graph` into a new outer class library `ImportToPlanner.Commercial`, registered by `web` only when `Features:CommercialMode:Enabled` is true. Self-host omits Tables and commercial persistence. Do not revive #72 or introduce HTTP between Blazor and commercial operations.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK from `global.json`)

**Primary Dependencies**: Blazor Interactive Server, MudBlazor, Microsoft.Identity.Web, Microsoft.Graph (planner path only), Aspire AppHost SDK, Aspire Azure Blob and Table client integrations, Azure.Data.Tables (Commercial project only), OpenTelemetry service defaults, xUnit v3, NSubstitute, bUnit

**Storage**: Existing Azure Storage account modelled in AppHost (`storage` + Azurite). Children: `blobs` (always), `dataprotection` blob container (always), `tables` (commercial only). Clients: `AddAzureBlobServiceClient("blobs")` → DI `BlobServiceClient` (data protection derives container/blob clients from this; keep `WithReference(dataprotection)`). `AddAzureTableServiceClient("tables")` when commercial → DI `TableServiceClient` then `GetTableClient`. Full resource map including `aca-env`, parameters, ServiceDefaults, Graph, and deferred Redis/SQL/Functions: [research.md](./research.md) §11. Runtime paths must not construct Azure Storage service clients.

**Testing**: xUnit v3 unit/component tests in `ImportToPlanner.Tests`; bUnit in `ImportToPlanner.Web.Tests`; architecture compliance via source/solution scans. No AppHost orchestration tests (`Aspire.Hosting.Testing` forbidden). No Playwright suite unless an explicit end-to-end journey is added (not required here). Do not adopt #72 Moq tests.

**Target Platform**: Linux-hosted ASP.NET Core / Azure Container Apps via Aspire; local Aspire + emulator; desktop and mobile browsers unchanged

**Project Type**: Layered Blazor web app (`Web`, `Commercial`, `Infrastructure.Graph`, `Application`, `Domain`) + Aspire AppHost + GitHub Actions staging deploy (commercial flag already present)

**Performance Goals**: No extra process hop for commercial access checks. Preserve 008 latency envelope (first sign-in including explanation under 2 minutes; profile deletion under 1 minute). No avoidable extra Graph or Table round-trips on the access path.

**Constraints**: Inward dependencies only; no Azure.Data.Tables / Graph / Kiota / MudBlazor in Application/Domain; Commercial must not reference Graph/Kiota; UK English UI; secrets out of source/logs/UI; self-host must not require Tables; no Application-wide folder reshuffle; no Functions/HTTP split; constitution 2.2.0; Azure Storage clients come from Aspire client integrations (`WithReference` + `AddAzureTableServiceClient` / `AddAzureBlobServiceClient`), not `new TableServiceClient` / `new BlobServiceClient` on runtime paths

**Scale/Scope**: One new class library; delete one empty web host project; AppHost topology simplification; move of existing commercial types/tests; invert architecture tests that currently **require** commercial contracts in Application; light 008 plan/contract doc alignment

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below — still passes.*

- **I. Dependency Direction**: `Web` / `Commercial` / `Infrastructure.Graph` → Application → Domain. Application will not reference Commercial. Commercial will not reference Graph.
- **II. Technology-neutral core**: Commercial account/audit/profile types leave Application. Remaining inner types are import/planner policy and shared tenant/session abstractions.
- **III. Explicit boundaries**: Web maps claims to `SessionIdentityContext`; Commercial returns structured access/lifecycle results; presenters stay in Web.
- **IV. Replaceable frameworks**: Tables, Aspire, and ACA are outer choices. The spec does not require HTTP, a second ACA app, or Functions. Aspire client integrations (`AddAzureTableServiceClient` / `AddAzureBlobServiceClient`) are the replaceable adapter for service-client construction; swapping storage later still happens at this outer seam, not inside use cases.
- **V. Traceability**: Changes map to spec 009 / issue #99. No opportunistic Application-wide reorganisation.
- **VI. Testable behaviour**: Use-case tests with in-memory stores at the Commercial boundary; registration tests for on/off mode; architecture scans. No production deploy required.
- **VII. Explicit failures**: Preserve structured commercial failures; Web keeps human-friendly messages; no raw Table dumps.
- **VIII. Security**: Tables and commercial login only when commercial mode is on; secrets stay out of logs/UI; profile delete remains the authenticated user acting on their own account.
- **IX. Quality evidence**: Architecture tests inverted and extended; commercial and self-host tests updated; format gate at implement.
- **X. Self-hosted viability**: Commercial registration omitted when mode is off; AppHost still skips `tables` when commercial mode is false.
- **Policy alignment (non-constitutional)**: `docs-internal/engineering-policies.md` already lists `ImportToPlanner.Commercial` on the outer layer map. AppHost will not be tested via Aspire testing packages. Test stack stays xUnit v3 / NSubstitute / Assert.

No constitution violations requiring Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/009-in-process-commercial/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── commercial-module-contracts.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.AppHost/
│   ├── AppHost.cs                          # drop commercialapiservice; tables → web only
│   └── ImportToPlanner.AppHost.csproj      # drop API project reference
├── ImportToPlanner.ApiService.Commercial/  # DELETE entire project
├── ImportToPlanner.Commercial/             # NEW class library
│   ├── DependencyInjection.cs              # AddCommercialStorageClients / AddCommercial
│   ├── Accounts/                           # models, store contracts, table adapters
│   ├── Audit/
│   ├── Access/                             # access + profile/lifecycle/purge use cases
│   └── TenantMetadata/                     # TableTenantOperationalMetadataStore (moved)
├── ImportToPlanner.Application/
│   ├── Abstractions/                       # keep ITenantOperationalMetadataStore, ICurrentTenantContextAccessor
│   ├── Models/                             # keep SessionIdentityContext, TenantOperationalMetadata; remove commercial account types
│   ├── Services/                           # import use cases only; remove *Commercial*
│   └── DependencyInjection.cs              # stop registering commercial use cases
├── ImportToPlanner.Infrastructure.Graph/
│   ├── DependencyInjection.cs              # CSV + planner (+ self-host metadata); no Tables/commercial stores
│   ├── TenantMetadata/SelfHostTenantOperationalMetadataStore.cs
│   └── CommercialAccounts/                 # DELETE (moved)
├── ImportToPlanner.Web/
│   ├── Program.cs                          # compose Commercial only when mode on
│   ├── ImportToPlanner.Web.csproj          # ProjectReference Commercial
│   └── Features/CommercialAccounts/        # presenters, profile UI, retention hosted service (host-owned)
tests/
├── ImportToPlanner.Tests/
│   ├── ArchitectureComplianceTests.cs      # invert + extend boundaries; topology source scans
│   ├── InfrastructureRegistrationTests.cs  # Graph vs Commercial registration
│   └── *Commercial*                        # retarget to Commercial types/stubs
└── ImportToPlanner.Web.Tests/              # commercial stubs from Commercial contracts

ImportToPlanner.slnx                        # replace API project with Commercial
specs/008-commercial-user-accounts/
├── plan.md                                 # docs drift: no API service; stores not Application-owned
└── contracts/commercial-account-contracts.md
```

**Structure Decision**: Existing layered solution plus one outer Commercial library and deletion of the unused API host. Web remains the only deployed user-facing project. Graph remains the planner/CSV adapter. Application/Domain stay import-centric with shared tenant/session seams.

## Complexity Tracking

> None. The extra class library is the constitution-aligned isolation mechanism already named in engineering policies, not a fourth unjustified inner layer.

## Phase 0 Research

See [research.md](./research.md). All Technical Context items are resolved from `main`, issue #99, and Aspire Azure Storage client-integration docs; no remaining NEEDS CLARIFICATION.

## Phase 1 Design

- [data-model.md](./data-model.md) — ownership move; 008 fields and transitions unchanged.
- [contracts/commercial-module-contracts.md](./contracts/commercial-module-contracts.md) — project, DI, AppHost, evidence.
- [quickstart.md](./quickstart.md) — restore, test, format, static topology, optional staging check.

## Implementation sketch (for `/speckit-tasks`)

Suggested order (issue #99 starting files):

1. Add `ImportToPlanner.Commercial` project (Application/Domain refs; Tables packages). Wire `slnx` and Web/AppHost references as needed.
2. Move commercial models, abstractions, use cases, table account/audit adapters, and `TableTenantOperationalMetadataStore` into Commercial; add DI extensions. Commercial (or Web, if registration stays on the host) must call `AddAzureTableServiceClient(connectionName: "tables")` and resolve `TableServiceClient` from DI; adapters use `GetTableClient` only. Do not add a parallel client factory.
3. Strip Application commercial registrations and types; strip Graph commercial/table branching; drop unused Graph Tables packages.
4. Compose from `web` only when commercial mode is on; omit no-op commercial stores; gate retention hosted service registration.
5. Delete `ImportToPlanner.ApiService.Commercial` and all `commercialapiservice` AppHost wiring (`minCommercialApiServiceReplicas`, `WithReference`/`WaitFor`).
6. Invert and extend architecture tests; retarget commercial unit/bUnit tests; add registration and topology source scans.
7. Align 008 plan/contracts text only.
8. `dotnet test`, `dotnet format … --verify-no-changes`. If AppHost is running, `aspire resource web rebuild` after Web/Commercial changes.

## Architecture impact statement

| Topic | Statement |
|-------|-----------|
| Dependency direction | New outer project depends inward; Application loses commercial types. |
| Boundaries | Commercial owns account/audit/retention/table tenant metadata; Graph owns planner/CSV/self-host metadata; Web owns identity mapping, UI, host composition. |
| Adapters | Table adapters leave Graph; no HTTP adapter. Azure Storage **service** clients stay Aspire-registered (`TableServiceClient` / `BlobServiceClient` from DI). Named `TableClient`s and data-protection `BlobClient`s are derived from those, including the existing `dataprotection` container path. |
| Traceability | Issue #99 / spec 009 FRs. |
| Testability | Commercial policy tests without Graph; self-host tests without Tables. |
| Errors | Existing structured commercial results; Web presentation unchanged. |
| Security | Commercial Tables and login remain commercial-mode-only. |
| Self-host | Mode off: no `tables`, no Commercial persistence, no commercial gate. |
