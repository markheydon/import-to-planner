# Quickstart: Profile Credits and Ledger Activity

Validation guide for spec 020. Contracts: [contracts/profile-credits-ledger-contracts.md](./contracts/profile-credits-ledger-contracts.md). Entities: [data-model.md](./data-model.md). Ledger foundation: [011 quickstart](../011-commercial-credit-ledger/quickstart.md). Complimentary rules: [019 quickstart](../019-complimentary-tenant-credits/quickstart.md).

## Prerequisites

- [#126](https://github.com/markheydon/import-to-planner/issues/126) / spec 011 credit ledger implemented and tests green.
- [#129](https://github.com/markheydon/import-to-planner/issues/129) complimentary policy available for allowlist scenarios.
- Commercial on locally via Aspire `enableCommercialMode`.
- `dotnet restore ImportToPlanner.slnx`

## Setup commands

```bash
dotnet restore ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

## Automated checks (must exist at implement)

1. **Summary after ensure (SC-001)** — Tenant with known grant and partial usage: profile summary `RemainingCredits` matches ledger after ensure with `Profile` reason.
2. **Fail closed balance (SC-002)** — Simulated ledger failure on summary: no `0` remaining; UK English error; no buy-credits substitute.
3. **Free lot row (SC-003)** — Current month free lot shows granted, remaining, month-end expiry aligned with `CreditLot`.
4. **Activity grouping (SC-004)** — Two import runs with multiple tasks each: exactly two usage rows with correct totals; grants/expiry visible for non-complimentary; legacy usage without `ImportRunId` yields one activity row per usage transaction (never one row per task).
5. **Twelve-month window (SC-004)** — Seed transaction older than twelve months: omitted from list; `HasOlderHistory` copy visible.
6. **Complimentary profile (SC-006)** — Allowlisted tenant: Unlimited remaining; activity shows grouped usage only; no free lot section.
7. **Commercial off (SC-005)** — No credits section; no new Home wallet (regression bUnit).
8. **Independent activity failure** — Summary succeeds, list fails: balance visible; activity error only.
9. **Tenant isolation (FR-002)** — Unit test: activity/summary requests use session tenant only.
10. **Architecture** — Ledger list + grouping in Commercial; Web presenters only; Application unchanged unless a neutral port is required later.

Optional when [#125](https://github.com/markheydon/import-to-planner/issues/125) lands: paid lot rows, purchase activity, buy-credits on profile for non-allowlisted tenants only (SC-007).

No AppHost integration tests unless explicitly requested.

## Manual / Aspire spot-check

1. Start Aspire with commercial on; sign in as a tenant with imports this month.
2. Open `/profile` → credits section shows remaining, free lot details, activity newest-first.
3. Run another import; refresh profile → remaining and new grouped usage row update.
4. Configure complimentary tenant GUID; confirm Unlimited and usage-only activity.
5. Turn commercial off → profile has no credits section (existing commercial profile behaviour).

## Expected outcomes

- Ledger remains sole statement source (not Stripe).
- Import gates unchanged from 011.
- UK English throughout profile credits copy.
