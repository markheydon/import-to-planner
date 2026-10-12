# Complimentary Tenant Credits Contracts

Boundary contracts for deploy-time complimentary unlimited credits (spec 019, issue [#129](https://github.com/markheydon/import-to-planner/issues/129)). Extends [commercial-credit-ledger-contracts.md](../../011-commercial-credit-ledger/contracts/commercial-credit-ledger-contracts.md); where this document is silent, 011 rules still apply for non-complimentary tenants.

## 1. Configuration

When `Features:CommercialMode:Enabled` is **true**:

| Setting | Required | Default | Notes |
|---------|----------|---------|--------|
| `Features:CommercialMode:ComplimentaryTenantIds` | No | empty string | Comma-separated Entra tenant GUIDs. AppHost parameter `complimentaryTenantIds` maps to this key. |

When **false**:

- Setting ignored; no complimentary policy registration; behaviour identical to pre-019 commercial-off/self-host.

Rules:

- Do not commit production tenant GUIDs in source.
- Document parameter name and empty default for operators (product doc + deployment notes).
- Invalid tokens: log diagnostic; ignore token; valid GUIDs in the same string remain effective.
- Membership evaluated on each credit-related action when `TenantId` is known (preview, confirm, execute, sign-in balance ensure).

## 2. Complimentary tenant policy (Commercial)

`IComplimentaryTenantPolicy.IsComplimentary(string tenantId)` → `bool`

| Input | Rule |
|-------|------|
| `tenantId` | From authenticated commercial session; never from unauthenticated client override. |
| Empty allowlist | Always `false`. |
| Match | `Guid.TryParse` on tenant id and on each valid list entry; equality match only. |

Commercial library only. Application and Web depend on abstractions or snapshot flags, not on configuration parsing.

## 3. Ensure current balance (amendment)

For complimentary tenants, `EnsureCurrentCreditBalance` MUST:

1. Skip free lot expiry and free monthly grant (no `FreeGrant` / `FreeExpiry` for this path).
2. Return success with `ComplimentaryUnlimitedAccess = true`.
3. Set numeric `RemainingCredits`, `FreeRemaining`, `PaidRemaining` to 0 for internal consistency.
4. Set `GrantApplied` and `ExpiryApplied` to false.

For non-complimentary tenants: unchanged 011 contract.

Failure modes unchanged (`credits.ledger_unavailable`, etc.). Fail closed on preview/confirm/execute; sign-in may still succeed when ensure fails (011).

## 4. Preview balance comparison (UI contract amendment)

After successful preview (commercial on):

| Field | Complimentary tenant | Non-complimentary |
|-------|----------------------|-------------------|
| `InsufficientCredits` | MUST be false | `N > M` as today |
| `ComplimentaryUnlimitedAccess` | true | false |
| `RemainingCredits` (display) | Present as **Unlimited** (UK English) | Integer M |
| Insufficient-credits `MudAlert` | MUST NOT show | Show when `N > M` |

Confirm import disabled for credits only when `InsufficientCredits` and not complimentary. Ledger unavailable still fail-closed (error, not insufficient warning).

## 5. Confirm import (coordinator amendment)

| If | Then |
|----|------|
| Complimentary tenant | Proceed if other gates pass; MUST NOT refuse for `M < N`. |
| Non-complimentary | Unchanged 011 live re-check. |
| Ledger unavailable | Unchanged fail-closed. |

## 6. Task-creation quota (Application port amendment)

Commercial `IImportTaskCreationQuota` implementation:

| Step | Complimentary tenant |
|------|----------------------|
| `BeforeCreateAsync` | Return `Allow` without exhaustion check (still fail if ledger unavailable when ensure required). |
| `RecordSuccessfulCreateAsync` | Record paired usage + complimentary grant; MUST NOT return exhausted solely due to zero remaining. |

Non-complimentary: unchanged 011 behaviour (stop at zero, usage from free lots).

## 7. Ledger store (Commercial)

`ICreditLedgerStore` (or dedicated method) for complimentary usage:

- Atomically append `Usage` and `ComplimentaryGrant` entries (quantity 1 each) linked to import run and created task id.
- Do not decrement `FreeMonthly` or `Paid` lots for complimentary tenants.
- Never produce negative derived balance.

## 8. Import summary (UI contract)

Commercial execution summary MUST include:

| Field | Complimentary | Notes |
|-------|---------------|--------|
| Tasks created | int | Unchanged |
| Credits used | int | Equals successful creates |
| Remaining | Unlimited / complimentary copy | NOT numeric 0 with exhausted framing |

Optional breakdown line: credits used labelled as complimentary usage (not “free monthly”).

## 9. Buy credits CTA

When `ComplimentaryUnlimitedAccess` is true on the active snapshot or session credit context:

- MUST NOT show buy-credits as required to continue importing.
- Future Stripe checkout entry points (#125) MUST honour the same guard.

## 10. Security

- Allowlist is deployment configuration; not editable by end users.
- Complimentary status derived only from configured GUID set and authenticated tenant id.
- No widening matchers (substring, email, user id).

## 11. Documentation contract

`docs/credits-billing-usage-model.md` MUST document complimentary tenants, configuration, ledger pairing, UI, and relation to monthly free and paid credits.
