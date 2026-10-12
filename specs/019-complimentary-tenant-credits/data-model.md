# Data Model: Complimentary Tenant Credits

Extends [011 data model](../011-commercial-credit-ledger/data-model.md). Commercial-only; inactive when `Features:CommercialMode:Enabled` is false.

## Ownership

| Entity | Owner | Notes |
|--------|--------|-------|
| `ComplimentaryTenantAllowlist` | Commercial (configuration) | Parsed from `Features:CommercialMode:ComplimentaryTenantIds` via `CommercialCreditModeOptions` in `ImportToPlanner.Commercial`; not persisted. |
| `IComplimentaryTenantPolicy` | Commercial service | Stateless membership check per request. |
| `CreditEntryType.ComplimentaryGrant` | Commercial enum extension | Pairs with `Usage` for complimentary tenants. |
| `LotType.Complimentary` | Commercial enum extension | Single logical lot per tenant for complimentary offset entries. |
| `CommercialCreditBalanceResult.ComplimentaryUnlimitedAccess` | Commercial result | Drives UI gating and unlimited copy; not a ledger row. |
| Existing ledger entities | Commercial (unchanged) | `CreditLedgerTransaction`, `CreditLot`, markers — extended usage rules only. |

## 1. Complimentary tenant allowlist (configuration)

| Field | Type | Notes |
|-------|------|--------|
| `ComplimentaryTenantIds` | string | Comma-separated Entra **tenant** GUIDs (`tid`). Default `""`. Trim segments; ignore empty. |
| Parsed entries | `IReadOnlySet<Guid>` | Built on each policy evaluation from current options. |
| Invalid tokens | n/a | Logged; ignored; do not invalidate valid entries. |

Validation:

- Not read when commercial mode is off.
- No production GUIDs in source control; operator docs only.
- Membership: `Guid.TryParse(tenantId)` and set contains equal GUID.

## 2. Complimentary organisation (derived)

Not a stored row. A commercial tenant whose `TenantId` GUID is in the parsed allowlist.

- Tenant-level (shared by all users), same as ledger scope.
- Re-evaluated on each credit-related action (sign-in balance refresh, preview, confirm, execute).

## 3. Credit lot (extension)

Add `LotType.Complimentary`:

| Field | Notes |
|-------|--------|
| `LotType` | `Complimentary` for the tenant’s complimentary offset lot. |
| `GrantedQuantity` / `RemainingQuantity` | May stay 0 in derived projections when usage and complimentary grants are paired; storage adapter keeps non-negative invariants. |
| `ExpiresAtUtc` | No expiry for complimentary lot (not a purchasable allocation). |

Free monthly and paid lots are **not** created or consumed for complimentary tenants during ensure or usage (FR-010).

## 4. Credit ledger transaction (extension)

Add `CreditEntryType.ComplimentaryGrant`:

| EntryType | Quantity | When |
|-----------|----------|------|
| `Usage` | 1 | Each successful task create (unchanged semantics). |
| `ComplimentaryGrant` | 1 | Immediately paired with complimentary `Usage` on the complimentary lot. |
| `FreeGrant` / `FreeExpiry` | — | Not written for complimentary tenants during ensure. |

Validation:

- Append-only; paired usage + complimentary grant for complimentary imports share the same `ImportRunId` / task reference on the usage row.
- Derived balance across all lots never &lt; 0.

## 5. Commercial credit balance snapshot (extension)

Extend successful ensure result:

| Field | Type | Notes |
|-------|------|--------|
| `ComplimentaryUnlimitedAccess` | bool | `true` when tenant is allowlisted. |
| `RemainingCredits` | int | For complimentary: internal 0 after ensure; UI MUST NOT treat as exhaustion when flag is true. |
| `FreeRemaining` / `PaidRemaining` | int | 0 for complimentary ensure path. |
| `GrantApplied` / `ExpiryApplied` | bool | false for complimentary ensure path. |

## 6. Workflow credit snapshot (Web)

Extend `WorkflowCreditBalanceSnapshot` (or parallel fields):

| Field | Notes |
|-------|--------|
| `ComplimentaryUnlimitedAccess` | When true, `InsufficientCredits` forced false; hide insufficient warning and buy CTAs. |
| `RemainingCredits` | Nullable; presenters show “Unlimited” when complimentary flag set. |

## 7. Import execution report

| Field | Notes |
|-------|--------|
| `CreditsUsed` | Integer count of tasks created (unchanged). |
| `RemainingCredits` | Present unlimited presentation when complimentary; not `0` with exhausted framing. |

## State transitions (complimentary tenant)

```text
Sign-in / Preview / Confirm / Execute
  → Policy.IsComplimentary(tenantId)?
       false → existing 011 ensure (grant/expiry/remaining)
       true  → ensure skips grant/expiry; snapshot.ComplimentaryUnlimitedAccess = true

Confirm gate
  → complimentary → never InsufficientCredits

Each successful create
  → Usage + ComplimentaryGrant (pair)
  → quota BeforeCreate → always Allow (unless ledger unavailable)
```
