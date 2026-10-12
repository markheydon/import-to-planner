# Quickstart: Complimentary Tenant Credits

Validation guide for spec 019. Contracts: [contracts/complimentary-tenant-credits-contracts.md](./contracts/complimentary-tenant-credits-contracts.md). Entities: [data-model.md](./data-model.md). Parent ledger: [011 quickstart](../011-commercial-credit-ledger/quickstart.md).

## Prerequisites

- [#126](https://github.com/markheydon/import-to-planner/issues/126) credit ledger implemented and tests green.
- Commercial on locally via Aspire `enableCommercialMode` (existing).
- Test tenant GUID available for allowlist (user secrets / local env only — do not commit).
- `dotnet restore ImportToPlanner.slnx`

## Setup commands

```bash
dotnet restore ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Configure complimentary allowlist for local validation (example):

```bash
# AppHost / user secrets — empty default in repo
# Features:CommercialMode:ComplimentaryTenantIds = "<your-test-tenant-guid>"
```

## Automated checks (must exist at implement)

1. **Policy parser (FR-015, SC-006)** — `"not-a-guid,<valid-guid>, ,<valid-guid>"` → one warning per invalid token; only valid GUID matches.
2. **Empty allowlist (SC-005)** — Behaviour matches 011 for insufficient preview/confirm.
3. **Complimentary preview (SC-001)** — Would-create &gt; 25; no insufficient alert; confirm not credit-disabled.
4. **Complimentary execute (SC-002)** — Creates N tasks; credits used = N; run does not stop for exhaustion only.
5. **Ledger audit (SC-004)** — N usage rows and N complimentary grant rows; derived balance ≥ 0.
6. **Ensure skip grant (FR-010)** — Complimentary tenant: no `FreeGrant` on ensure; non-complimentary still grants 25.
7. **Quota exhaustion bypass** — Complimentary mid-run with would-have exhausted normal tenant: all allowed creates complete.
8. **Commercial off (SC-007)** — Allowlist setting present but ignored; no unlimited copy.
9. **Regression** — Run existing `ImportToPlanner.Tests/Credits/*` and Web credit bUnit tests for non-allowlisted tenants unchanged.
10. **Ledger unavailable (FR-016)** — Allowlisted tenant with simulated ledger read/ensure failure: preview/confirm/execute fail closed with ledger error; no insufficient-credits warning; remaining not shown as numeric zero exhaustion.
11. **Architecture** — Policy and ledger extensions stay in Commercial; Web only sees flags/presenters (no Web types in Commercial for allowlist parsing).

No AppHost integration tests (issue #129).

## Manual / Aspire spot-check

1. Set `complimentaryTenantIds` to your test organisation tenant GUID; start Aspire with commercial on.
2. Sign in as that tenant; preview import with would-create &gt; 25 → no shortfall warning; Confirm enabled.
3. Execute → summary shows tasks created, credits used, remaining **Unlimited** (or equivalent).
4. Remove GUID from allowlist (or use second tenant not on list) → insufficient-credit warning returns when over balance.
5. Commercial mode off → no credit UI; allowlist irrelevant.

## Expected outcomes

- Single import pipeline and ledger; no Stripe changes in this story.
- Operator configures allowlist at deploy time only.
- `docs/credits-billing-usage-model.md` updated for complimentary tenants.
