# Implementation Plan: Commercial Credit Ledger

**Branch**: `011-commercial-credit-ledger` | **Date**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-commercial-credit-ledger/spec.md`, product contract `docs-internal/credits-billing-usage-model.md`, GitHub issue [#126](https://github.com/markheydon/import-to-planner/issues/126). User increment constraints: no Stripe/checkout/webhooks/paid purchases; no free-account SKU or free-import count; preserve `ImportWorkflowCoordinator` gating except the live credit confirm rule; follow Clean Architecture, 008/009 commercial storage, and `docs-internal/engineering-policies.md`.

**Note**: Coding, architecture, and tests at implement time are delegated to the C# Expert agent (`AGENTS.md`) using `csharp-async`, `csharp-docs`, `csharp-xunit`, `dotnet-best-practices-repo`, and the `mudblazor` skill for preview warning and summary chrome. Do not add public pricing docs in this increment.

## Summary

Add a tenant-scoped, append-only credit ledger for **commercial mode on only**: lazy 25 free credits per UTC calendar month, explicit free-credit expiry, preview warning and confirm block when would-create exceeds **live** remaining, and execution that charges one credit per successfully created Planner **task** without going negative. Persistence follows 008/009 (Commercial + existing `tables`). Application gains only a technology-neutral create-loop quota port. Paid lots are modelled, not implemented. Self-hosted / commercial-off journeys stay credit-free.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK from `global.json`)

**Primary Dependencies**: Blazor Interactive Server, MudBlazor (preview `MudAlert` / summary text only), existing commercial access and `ImportWorkflowCoordinator`, Azure.Data.Tables in Commercial only, Aspire `tables` already wired for commercial mode, xUnit v3, NSubstitute, bUnit. **Not in this increment**: Stripe.net, checkout UI, webhooks, invoices.

**Storage**: Existing AppHost `tables` (Azurite locally). New table name via `Storage:CommercialCreditLedgerTable` (default `CommercialCreditLedger`). No new Aspire storage resource, no SQL, no Redis. Commercial off must not require the new setting.

**Testing**: xUnit v3 unit tests in `ImportToPlanner.Tests` (ledger use cases with in-memory store; execution quota stop/retry; architecture scans). bUnit in `ImportToPlanner.Web.Tests` (warning, disabled confirm, summary, commercial-off). No `Aspire.Hosting.Testing`. No Playwright suite unless a later journey is explicitly requested.

**Target Platform**: Linux-hosted ASP.NET Core / Azure Container Apps via Aspire; local Aspire + emulator; desktop and mobile browsers unchanged

**Project Type**: Layered Blazor web app (`Web`, `Commercial`, `Infrastructure.Graph`, `Application`, `Domain`) + Aspire AppHost

**Performance Goals**: One tenant-partition read/EGT per ensure or per credit consume; no extra Graph calls for metering; preview remains allowed regardless of remaining; avoid scanning other tenants

**Constraints**: Inward dependencies only; no Azure Tables / MudBlazor / Stripe in Application or Domain; Commercial must not reference Graph/Kiota/MudBlazor/Stripe; UK English UI; secrets out of source/logs/UI; self-host must not require Tables or a ledger; coordinator gates unchanged except credit confirm + fail-closed ledger errors; no overage; no header wallet; UTC month boundaries; `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes`

**Scale/Scope**: One extra table and Commercial credit module; small Application execution-loop change; Home preview/summary presenter copy; startup validator key when commercial on. No new projects, routes, or paid SKUs.

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below — still passes.*

- **I. Dependency Direction**: Ledger adapters and policy live in `ImportToPlanner.Commercial`. Application depends only on a quota port and existing preview/execution models. Domain unchanged. Web maps session tenant id and presents UK English.
- **II. Technology-neutral core**: Grant quantity, lot types, and diagnostic codes are repository types. Table entities, MudBlazor alerts, and any future Stripe types stay outer.
- **III. Explicit boundaries**: Ensure/confirm/consume have request/response records without UI strings. Presenters assemble N/M/shortfall copy. Quota port is the Application seam for stopping creates.
- **IV. Replaceable frameworks**: Tables are the current adapter; a later store still implements the same Commercial store contract. Spec remains valid without Stripe.
- **V. Traceability**: Work maps to spec 011 FRs / US1–US5 and issue #126. Paid checkout is explicitly out of scope (#125).
- **VI. Testable behaviour**: In-memory ledger and quota doubles; bUnit for disable/warning; no production deploy required.
- **VII. Explicit failures**: Ledger unavailable, insufficient credits, exhaustion, and usage-record failure are distinct structured outcomes; Web maps to human-friendly messages; remaining is never invented as zero.
- **VIII. Security**: Ledger partitioned by authenticated `TenantId`; no client-supplied tenant override; no new secrets; commercial Tables only when mode is on.
- **IX. Quality evidence**: Architecture tests extended; commercial on/off tests; format gate at implement.
- **X. Self-hosted viability**: Credit registration omitted when commercial mode is false; import journey and coordinator behaviour match today aside from a no-op quota.
- **Policy alignment (non-constitutional)**: `engineering-policies.md` — xUnit v3, NSubstitute, Assert, no AppHost tests, commercial persistence only when enabled, UK English.

No constitution violations requiring Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/011-commercial-credit-ledger/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── commercial-credit-ledger-contracts.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/ImportToPlanner.Commercial/
├── DependencyInjection.cs                 # keyed TableClient + credit use cases when mode on
├── Abstractions/                          # ICreditLedgerStore, ICommercialCreditUseCase
├── Models/                                # transactions, lots, balance snapshot, failure codes
├── Credits/
│   ├── CommercialCreditPolicy.cs          # 25 free / UTC month helpers (constant, not a SKU)
│   ├── EnsureCurrentCreditBalanceUseCase.cs
│   └── ImportTaskCreationCreditQuota.cs   # IImportTaskCreationQuota implementation
└── Credits/Storage/
    └── TableCreditLedgerStore.cs          # EGT consume/grant/expiry; month marker

src/ImportToPlanner.Application/
├── Abstractions/IImportTaskCreationQuota.cs
└── Services/ImportExecutionUseCase.cs     # BeforeCreate + RecordSuccessfulCreate around task creates only

src/ImportToPlanner.Web/
├── Infrastructure/StartupConfigurationValidator.cs   # require credit table name when commercial on
├── appsettings.json / appsettings.Development.json
├── Features/Import/Workflows/ImportWorkflowCoordinator.cs  # preview snapshot + confirm re-check only
├── Features/Import/Workflows/WorkflowCoordinationState.cs
├── Features/Import/Pages/Home/            # canExecute credit clause; MudAlert; no purchase CTA
├── Features/Import/Pages/Home/Home.CommercialAccess.razor.cs  # Ensure on Allow/CreateAccount; ignore ledger fail
├── Features/Import/Presenters/            # UK English warning + summary credit figures
└── Features/Import/Pages/Home/HomeExecutionReport.razor

tests/ImportToPlanner.Tests/               # ledger, quota, execution stop, architecture, registration
tests/ImportToPlanner.Web.Tests/           # bUnit warning/confirm/summary/commercial-off; HomePageTestContext config
```

**Structure Decision**: Extend the existing Commercial outer library and Web import feature. Do not add a billing project, API host, or Stripe adapter. Graph remains planner-only.

## Complexity Tracking

> None. The extra table and quota port are the 008/009 isolation pattern, not a new inner layer.

## Phase 0 Research

See [research.md](./research.md). Technical Context has no NEEDS CLARIFICATION remaining.

## Phase 1 Design

- [data-model.md](./data-model.md) — lots, immutable transactions, month marker, derived balance, would-create, table keys.
- [contracts/commercial-credit-ledger-contracts.md](./contracts/commercial-credit-ledger-contracts.md) — composition, Ensure, preview/confirm UI, quota, summary, security.
- [quickstart.md](./quickstart.md) — test and Aspire validation scenarios.

### Constitution re-check (post-design)

Still passes: ledger stays in Commercial; Application only stops the create loop via a neutral quota; self-host no-op; fail-closed errors are specified; no Stripe; coordinator changes limited to credit confirm and preview balance snapshot.

## Implementation notes (for `/speckit-tasks`)

- Would-create = count of preview `TaskActions` with `PlannedEntityAction.Create` (not `OutcomeSummary.CreatedCount`, which includes buckets).
- Inject a clock for UTC month tests.
- Month marker `AddEntity` uniqueness is the grant idempotency mechanism.
- Insufficient-credits presenter must not add a buy button or “purchase complete” copy.
- After coding, if AppHost is running, rebuild the `web` resource only (`aspire resource web rebuild`); no new resources.
