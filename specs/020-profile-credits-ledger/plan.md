# Implementation Plan: Profile Credits and Ledger Activity

**Branch**: `020-profile-credits-ledger` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/020-profile-credits-ledger/spec.md`, clarifications session 2026-10-12, product contract `docs/credits-billing-usage-model.md`, GitHub issue [#138](https://github.com/markheydon/import-to-planner/issues/138) (parent [#124](https://github.com/markheydon/import-to-planner/issues/124)). **Depends on** [#126](https://github.com/markheydon/import-to-planner/issues/126) / spec 011. **Coordinates with** [#129](https://github.com/markheydon/import-to-planner/issues/129) / spec 019 and [#125](https://github.com/markheydon/import-to-planner/issues/125) (paid lots and buy-credits when available).

**Note**: Implementation delegates to C# Expert (`AGENTS.md`) with `csharp-async`, `csharp-xunit`, `dotnet-best-practices-repo`, `mudblazor` for profile UI. No AppHost tests. Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes` before merge.

## Summary

Add a **credits section on `/profile`** for hosted commercial mode: show ledger-derived **remaining credits after ensure**, **current free monthly lot** (non-complimentary), and a **rolling twelve-month activity report** with import usage **grouped by import run**. Fail closed with friendly UK English when reads fail; never show remaining as zero on errors. Complimentary allowlisted tenants see **Unlimited** and **usage-only** activity. Paid lots, purchases, and buy-credits extend the same section when [#125](https://github.com/markheydon/import-to-planner/issues/125) ships. No Home wallet chrome; import credit gates unchanged.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`global.json`)

**Primary Dependencies**: Blazor + MudBlazor (`Profile.razor`), `ImportToPlanner.Commercial` credit module (011/019), `IEnsureCurrentCreditBalanceUseCase`, Azure Tables `TableCreditLedgerStore`, xUnit v3, NSubstitute, bUnit

**Storage**: Existing `CommercialCreditLedger` table; new read query on transaction rows only. No new Aspire resources.

**Testing**: Commercial unit tests (list API, grouping, window, complimentary filter); Web bUnit for profile credits visibility and fail-closed; extend `InMemoryCreditLedgerStore`. No `Aspire.Hosting.Testing`. No Playwright unless explicitly requested.

**Target Platform**: Hosted commercial (ACA via Aspire); local Azurite; self-host unchanged (no credit UI)

**Project Type**: Layered Blazor (`Web`, `Commercial`, `Application`, `Domain`, `Infrastructure.Graph`) + AppHost

**Performance Goals**: One ensure + one partition query per profile load; in-memory aggregation acceptable for twelve-month window; pagination optional if row count is large within window

**Constraints**: Clean Architecture inward deps; UK English UI; commercial off registers no profile credit services; balance and activity errors independent; format gate in CI

**Scale/Scope**: Read path + profile UI on existing route; add `EnsureBalanceReason.Profile` for profile-load ensure audit; #125 hooks reserved in contracts/models

## Constitution Check

*GATE: Passes before Phase 0. Re-checked after Phase 1 — passes.*

- **I. Dependency direction**: Ledger list, aggregation, and ensure stay in Commercial. Web maps structured results to MudBlazor; no table access in Web.
- **II. Technology-neutral core**: Profile results use Commercial records/enums; no MudBlazor in Commercial.
- **III. Explicit boundaries**: Summary/activity use cases return structured outcomes; `ProfileCreditPresenter` owns user-facing strings.
- **IV. Replaceable frameworks**: Read use cases depend on `ICreditLedgerStore` abstraction; Azure remains adapter.
- **V. Traceability**: Maps to spec 020 FR-001–FR-014 and user stories 1–5.
- **VI. Testable**: In-memory store + bUnit profile scenarios; grouping logic unit-tested without production deploy.
- **VII. Explicit failures**: Fail-closed codes from Commercial; no silent zero balance.
- **VIII. Security**: Tenant id from session only; no Stripe ids or diagnostics in UI.
- **IX. Quality evidence**: Tests + format + architecture scans (Commercial/Web boundaries).
- **X. Self-hosted viability**: Commercial off → no profile credit registration or UI; import path for self-host unchanged.

No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/020-profile-credits-ledger/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── tasks.md
├── quickstart.md
├── contracts/
│   └── profile-credits-ledger-contracts.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/ImportToPlanner.Commercial/
├── Abstractions/
│   └── ICreditLedgerStore.cs              # add ListTransactionsAsync, HasTransactionsBeforeAsync
├── Models/
│   ├── CreditLedgerEnums.cs               # EnsureBalanceReason.Profile
│   └── ProfileCreditModels.cs             # summary/activity result types (new)
├── Credits/
│   ├── EnsureCurrentCreditBalanceUseCase.cs  # unchanged behaviour; new reason only
│   ├── GetProfileCreditSummaryUseCase.cs  # new
│   ├── GetProfileCreditActivityUseCase.cs # new (12m window, grouping)
│   └── Storage/TableCreditLedgerStore.cs  # list implementation
└── DependencyInjection.cs                 # register profile read use cases

src/ImportToPlanner.Web/
├── Features/CommercialAccounts/Pages/
│   ├── Profile.razor                      # credits section UI
│   └── Profile.razor.cs                   # load summary + activity
└── Features/CommercialAccounts/Presenters/
    └── ProfileCreditPresenter.cs          # UK English labels

tests/ImportToPlanner.Tests/Credits/        # list, grouping, summary, activity
tests/ImportToPlanner.Web.Tests/            # ProfilePageTests extensions
```

**Structure Decision**: Extend 011/019 Commercial storage and ensure; add profile-specific read use cases and Web presenters. No coordinator or Home changes for this story.

## Complexity Tracking

> None.

## Phase 0 Research

See [research.md](./research.md). No NEEDS CLARIFICATION in Technical Context.

## Phase 1 Design

- [data-model.md](./data-model.md) — summary, activity entries, `HasOlderHistory` via `HasTransactionsBeforeAsync` (§2), failure handling.
- [contracts/profile-credits-ledger-contracts.md](./contracts/profile-credits-ledger-contracts.md) — use cases, store list API (`ListTransactionsAsync`, `HasTransactionsBeforeAsync` §5), UI rules.
- [quickstart.md](./quickstart.md) — validation scenarios.

### Constitution re-check (post-design)

Still passes: read-only ledger path; presentation in Web; complimentary and fail-closed rules align with 019 and 011; self-host unaffected; import gates untouched.

## Implementation notes (for `/speckit-tasks`)

1. **Enums** — Add `EnsureBalanceReason.Profile` for audit on profile load; amend [011 commercial-credit-ledger-contracts.md](../011-commercial-credit-ledger/contracts/commercial-credit-ledger-contracts.md) §2 `Reason` enum (per T001 and 020 contracts §2).
2. **Store** — `ListTransactionsAsync` plus `HasTransactionsBeforeAsync` on `ICreditLedgerStore` (lightweight existence check for `HasOlderHistory` without loading pre-window rows); table query + `InMemoryCreditLedgerStore`.
3. **Summary use case** — Ensure → map balance + current free lot + paid lots (empty until #125).
4. **Activity use case** — Twelve-month filter (`request.OccurredUtc.AddMonths(-12)`), complimentary filter, group usage by `ImportRunId` when present (legacy rows without id: one grouped row per usage transaction), set `HasOlderHistory` via `HasTransactionsBeforeAsync` (not from the windowed list alone).
5. **DI** — Register use cases when commercial on only.
6. **Profile UI** — MudPaper section; separate alerts for balance vs activity errors; twelve-month omission copy; reuse unlimited formatting from import presenters where appropriate.
7. **#125 readiness** — Contract slots for paid lots, purchase rows, buy-credits button guarded by feature/checkout availability and non-complimentary policy.
8. **Tests** — Per [quickstart.md](./quickstart.md); keep 011/019 import tests green.
9. **Format** — `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes`.
