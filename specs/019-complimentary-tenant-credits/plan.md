# Implementation Plan: Complimentary Tenant Credits

**Branch**: `019-complimentary-tenant-credits` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/019-complimentary-tenant-credits/spec.md`, clarifications session 2026-10-12, product contract `docs/credits-billing-usage-model.md`, GitHub issue [#129](https://github.com/markheydon/import-to-planner/issues/129). Depends on [#126](https://github.com/markheydon/import-to-planner/issues/126) ledger. Related: [#125](https://github.com/markheydon/import-to-planner/issues/125) buy-CTA guard only.

**Note**: Implementation delegates to C# Expert (`AGENTS.md`) with `csharp-async`, `csharp-xunit`, `dotnet-best-practices-repo`, `mudblazor` for preview/summary copy. No AppHost tests. Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` before merge.

## Summary

Add a deploy-time comma-separated **Entra tenant GUID allowlist** for hosted commercial mode so operator and tester organisations import with **unlimited credits**: no confirm gate for insufficient credits, no buy-credits path, **Unlimited** remaining in UI, but **full usage metering** on the existing ledger via paired **Usage + ComplimentaryGrant** transactions. Skip monthly free grant/expiry for allowlisted tenants; execution **never stops solely for credit exhaustion**. Non-allowlisted and commercial-off behaviour unchanged.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`global.json`)

**Primary Dependencies**: Existing Blazor + MudBlazor import Home, `ImportWorkflowCoordinator`, `ImportToPlanner.Commercial` credit module (011), Aspire AppHost parameters, Azure Tables ledger store, xUnit v3, NSubstitute, bUnit

**Storage**: Same `CommercialCreditLedger` table; new entry/lot types only. No new Aspire resources.

**Testing**: Unit tests (policy, ensure, store pairs, quota); Web bUnit (warning/confirm/summary); extend existing credit integration tests. No `Aspire.Hosting.Testing`. No Playwright unless explicitly requested later.

**Target Platform**: Hosted commercial (ACA via Aspire); local Azurite; self-host unchanged

**Project Type**: Layered Blazor (`Web`, `Commercial`, `Application`, `Domain`, `Infrastructure.Graph`) + AppHost

**Performance Goals**: Allowlist parse is O(n) on tiny n; no extra Graph calls; one policy check per credit action; complimentary usage still one EGT per create (usage + grant pair)

**Constraints**: Clean Architecture inward deps; UK English UI; no tenant GUIDs in repo; commercial off ignores allowlist; `IOptionsMonitor` for live config; issue #129 — no AppHost tests; format gate enforced in CI

**Scale/Scope**: Small delta on 011 — options + policy + ledger enum/store + ensure/quota branches + Web snapshot/presenters + AppHost env + product doc

## Constitution Check

*GATE: Passes before Phase 0. Re-checked after Phase 1 — passes.*

- **I. Dependency direction**: Policy, ledger extensions, and paired recording stay in Commercial. Application quota port behaviour extended via Commercial implementation. Web maps flags to UK English only.
- **II. Technology-neutral core**: `ComplimentaryUnlimitedAccess` on balance/snapshot results; no MudBlazor in Commercial.
- **III. Explicit boundaries**: Ensure/quota outcomes remain structured; presenters build “Unlimited” strings.
- **IV. Replaceable frameworks**: Allowlist is configuration; ledger adapter pattern unchanged.
- **V. Traceability**: Maps to spec 019 FR-001–FR-017 and issue #129 acceptance criteria.
- **VI. Testable**: In-memory store + policy tests; bUnit for UI gates.
- **VII. Explicit failures**: Ledger unavailable unchanged; complimentary does not mask failures as insufficient credits.
- **VIII. Security**: Tenant id from session; GUID exact match; invalid tokens ignored with logs.
- **IX. Quality evidence**: Tests + format + architecture scans for Commercial/Web boundaries.
- **X. Self-hosted viability**: Commercial off → no policy, no UI, no ledger reads for allowlist.

No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/019-complimentary-tenant-credits/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── complimentary-tenant-credits-contracts.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/ImportToPlanner.AppHost/
└── AppHost.cs                          # parameter complimentaryTenantIds → Features__CommercialMode__ComplimentaryTenantIds

src/ImportToPlanner.Commercial/
├── Configuration/
│   └── CommercialCreditModeOptions.cs  # ComplimentaryTenantIds; bound in AddCommercial
├── Models/
│   ├── CreditLedgerEnums.cs            # ComplimentaryGrant, LotType.Complimentary
│   └── CommercialCreditBalanceModels.cs  # ComplimentaryUnlimitedAccess on result
├── Credits/
│   ├── ComplimentaryTenantPolicy.cs    # IComplimentaryTenantPolicy
│   ├── EnsureCurrentCreditBalanceUseCase.cs
│   ├── ImportTaskCreationCreditQuota.cs
│   └── Storage/TableCreditLedgerStore.cs
└── DependencyInjection.cs              # register policy + options

src/ImportToPlanner.Web/
├── Features/Import/Workflows/
│   ├── ImportWorkflowCoordinator.cs    # skip insufficient path when complimentary
│   └── WorkflowCreditBalanceSnapshot.cs
├── Features/Import/Pages/Home/
│   ├── Home.razor                      # alert / remaining display
│   ├── Home.WorkflowActions.razor.cs   # canExecute credit clause
│   └── HomeExecutionReport.razor
├── Features/Import/Presenters/
│   ├── ImportCreditPreviewPresenter.cs
│   └── ImportExecutionPresenter.cs

docs/
└── credits-billing-usage-model.md      # complimentary section

tests/ImportToPlanner.Tests/Credits/     # policy, ensure, store, quota, regression
tests/ImportToPlanner.Web.Tests/         # bUnit complimentary + commercial-off
```

**Structure Decision**: Extend 011 touchpoints only; no new projects, routes, or Stripe work.

## Complexity Tracking

> None.

## Phase 0 Research

See [research.md](./research.md). No NEEDS CLARIFICATION in Technical Context.

## Phase 1 Design

- [data-model.md](./data-model.md) — allowlist, complimentary lot/grant, snapshot flags.
- [contracts/complimentary-tenant-credits-contracts.md](./contracts/complimentary-tenant-credits-contracts.md) — config, ensure, UI, quota, ledger, security.
- [quickstart.md](./quickstart.md) — validation scenarios.

### Constitution re-check (post-design)

Still passes: complimentary logic remains outer Commercial + Web presentation; single import pipeline; self-host unaffected; fail-closed ledger errors preserved.

## Implementation notes (for `/speckit-tasks`)

1. **Configuration** — AppHost parameter + Commercial `CommercialCreditModeOptions` binding in `AddCommercial`; operator doc in `docs/credits-billing-usage-model.md` (not committed GUIDs).
2. **Policy** — Parse and `IsComplimentary`; log invalid segments.
3. **Ensure** — Early complimentary branch before expiry/grant loops.
4. **Store** — `RecordComplimentaryUsageAsync` or branch inside `RecordUsageAsync` with paired transactions.
5. **Quota** — Allow + never exhausted for complimentary; keep retry on usage failure.
6. **Web** — Snapshot flag; coordinator + Home + presenters + execution report unlimited copy.
7. **Buy CTA** — Guard any existing or stub purchase prompts with complimentary flag (#125-ready).
8. **Tests** — Per quickstart; keep 011 non-allowlisted cases green.
9. **Format** — `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes`.
