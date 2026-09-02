# Quickstart: Commercial Credit Ledger

Validation guide for spec 011. Implementation details belong in `tasks.md`. Contracts: [contracts/commercial-credit-ledger-contracts.md](./contracts/commercial-credit-ledger-contracts.md). Entities: [data-model.md](./data-model.md).

## Prerequisites

- Solution restores (`dotnet restore ImportToPlanner.slnx`).
- Tests: xUnit v3, NSubstitute, built-in Assert; bUnit for Web. No AppHost tests. No Playwright suite required for this feature.
- Commercial on: existing Azurite / `tables` path from 009. New table name `Storage:CommercialCreditLedgerTable` (default `CommercialCreditLedger`).
- Commercial off: current self-host path; credit setting not required.
- Do not configure Stripe. Do not add a free-account SKU.

## Setup commands

```bash
dotnet restore ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Local hosted commercial (existing Aspire commercial flag): enable `Features:CommercialMode:Enabled` / AppHost `enableCommercialMode` as today. Confirm `aspire start` still runs without a Stripe secret and without a new storage resource.

## Automated checks (must exist at implement)

Run `dotnet test ImportToPlanner.slnx` and expect coverage equivalent to:

1. **First grant (US1 / SC-001, SC-003)** — In-memory ledger: first `EnsureCurrentBalance` in a UTC month grants 25 at `OccurredUtc`; mid-month still 25; second call same month does not grant.
2. **Month boundary (SC-004, SC-014)** — Clock at month-end leftover 10 free; next preview/confirm/execute in the new month writes expiry then a new grant of 25; remaining is 25, not 35.
3. **Dormant (FR-007)** — No sign-in and no balance call ⇒ no grant or expiry rows.
4. **Validation does not grant (SC-005)** — CSV parse errors ⇒ no ledger writes.
5. **Preview does not consume (SC-005)** — Preview with N ≤ M leaves remaining unchanged except due expiry/grant.
6. **Insufficient confirm (SC-006, SC-013)** — N > M disables/refuses confirm with N, M, shortfall; live re-check after another consumer reduced M.
7. **Ledger unavailable (SC-015)** — Preview/confirm/execute fail closed; no grant; remaining not treated as 0.
8. **Execute metering (SC-008, SC-009)** — One usage per created task; stop at 0; no negative; remaining Create rows visible as credit-exhausted; buckets/reuse do not consume.
9. **Usage record failure (SC-016)** — Created task kept; retry then stop; not-started rows visible.
10. **Concurrent grant** — Two Ensure calls same month ⇒ one `FreeGrant`.
11. **Commercial off (SC-011)** — No table client; no warning; confirm not credit-blocked; summary has no credit figures.
12. **Architecture** — Ledger types not in Application/Domain; Commercial still Graph/Stripe-free; format gate.

## Manual / Aspire spot-check (commercial on)

1. Sign in a tenant with no ledger rows this UTC month → session works; remaining 25 after first preview.
2. Preview with would-create 26 → prominent warning, Confirm import disabled, no checkout.
3. Preview with would-create 1 → confirm enabled (other gates permitting).
4. Execute creating 1 task → summary: tasks created 1, credits used 1 (free monthly), remaining 24.
5. Commercial mode off: full import; no credit copy on Home.

## Expected outcomes

- Hosted commercial tenants share one ledger per `TenantId`.
- Self-host / commercial off unchanged aside from the no-op quota registration.
- Paid purchases remain unimplemented; insufficient-credits copy does not fake a payment.
- `ImportWorkflowCoordinator` gating unchanged except live credit confirm compare and fail-closed ledger errors on preview/execute.
