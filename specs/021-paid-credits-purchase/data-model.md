# Data Model: Paid Credits Purchase

Extends the tenant credit ledger from spec 011 and profile reads from spec 020. Partition key for every ledger and billing row remains the Entra tenant id. `ActorUserId` is audit data on transactions and checkout requests. It is not a partition or row key.

## Credit pack (configuration)

Not a table entity. Bound from `Stripe:CreditPacks`.

| Field | Rules |
| --- | --- |
| PackId | Stable slug. Required. Unique among published packs. |
| CreditQuantity | Integer greater than zero. This is the ledger grant quantity. |
| StripePriceId | Stripe Price id (`price_...`). Required when published. |
| DisplayName | UK English label for preview and profile. |
| AmountMinor | Minor currency units for display. Must match the Stripe Price. Not sent as a custom amount. |
| Currency | ISO currency for display. |
| Published | Unpublished packs are hidden and rejected at checkout. |

Preview recommendation: among published packs, the one with the smallest `CreditQuantity` that is greater than or equal to `wouldCreate - remaining`. If none covers the shortfall, show the catalogue with no pre-selection.

## Tenant billing metadata

Table: `Storage:CommercialBillingTable` (default `CommercialBilling`).

| Key | Value |
| --- | --- |
| PartitionKey | Tenant id |
| RowKey | `billing` |

| Field | Rules |
| --- | --- |
| StripeCustomerId | Opaque customer id created or reused for Checkout. Not shown in the UI. |
| LastInvoiceOrReceiptId | Optional latest Stripe invoice or receipt reference for support. Not shown in the UI. |
| UpdatedUtc | Last write. |

This row is not on the commercial account entity and not in the credit ledger. Account delete and account purge do not read or write it.

## Paid credit lot

Same `CreditLot` shape as free lots, stored at row key `lot|{lotId}` in the ledger table.

| Field | Paid-lot rules |
| --- | --- |
| LotId | `paid|{externalPaymentId}` |
| LotType | `Paid` |
| GrantedQuantity | Pack `CreditQuantity` |
| RemainingQuantity | Starts at granted quantity. Decremented by usage, expiry, or adjustment. Never negative. |
| GrantedAtUtc | Payment confirmation instant from the verified event, not checkout creation time. |
| ExpiresAtUtc | `GrantedAtUtc` plus twelve calendar months. Exclusive, matching `CreditLotSelector.IsConsumable`. |
| ExternalPaymentId | Opaque payment identifier (Checkout Session payment intent, or the session id if that is the stable payment reference the event carries). Stored on the lot entity. Used to find the lot for a refund. Not a Stripe SDK type in Commercial. |

`CreditLotSelector` order is unchanged: consumable free monthly lots first (`LotType.FreeMonthly`), then other consumable lots by `GrantedAtUtc`, then `LotId` for equal timestamps. Expired or zero-remaining lots are skipped.

## Ledger transactions

Existing row key `tx|{reverseTicks}|{transactionId}`. Append-only. New and newly written entry types:

| EntryType | When | Quantity |
| --- | --- | --- |
| `PaidPurchase` (3) | First confirmed payment for `ExternalPaymentId` | Pack credit quantity |
| `PaidExpiry` (4) | Scheduled expiry of unused remainder | Unused quantity removed (omit the transaction when unused is already zero) |
| `PaidAdjustment` (6, new) | Confirmed refund or lost dispute | Unused quantity removed, or zero when the lot is already empty, so the marker still exists |

`ActorUserId` on `PaidPurchase` is the user who started checkout. Expiry may leave it empty (system). Adjustment may leave it empty (provider event). None of these values authorise the write.

Profile activity maps `PaidPurchase` and `PaidExpiry` as today, and gains a kind for `PaidAdjustment` with UK English labelling in the presenter (for example a refund of unused credits). Customer ids stay out of the activity text.

## Idempotency markers

Same ledger table, tenant partition. Insert-if-absent (`AddEntity`). Conflict means already processed.

| RowKey | Written with | Effect on replay |
| --- | --- | --- |
| `pay|{externalPaymentId}` | Paid lot + `PaidPurchase` | No second lot |
| `evt|{stripeEventId}` | Any handled webhook event | No second side effect for that delivery |
| `adj|{externalAdjustmentId}` | `PaidAdjustment` and lot remaining update | No second reduction |

Grant flow records both `evt|` and `pay|`. A second event id for the same payment (for example `checkout.session.completed` and a later duplicate, or async success after completed) hits the existing `pay|` row and does not grant again. A replay of the same event id hits `evt|`.

## Checkout session (Stripe, not stored as the system of record)

Created in Stripe. Server-written metadata:

| Metadata key | Source |
| --- | --- |
| tenant_id | Session tenant |
| pack_id | Chosen published pack |
| actor_user_id | Session user, audit only |

`client_reference_id` repeats the tenant id. `customer` is the tenant’s `StripeCustomerId`. `mode` is `payment`. One line item: the pack’s `StripePriceId`. No `payment_method_types`. Success and cancel URLs point at the in-app return surface (preview or profile). Optional `invoice_creation.enabled` so Stripe can issue a receipt or invoice. `automatic_tax.enabled` follows `Stripe:AutomaticTaxEnabled`.

The ledger does not change when the session is created.

## Payment confirmation (in-memory contract)

Neutral record produced by the Stripe adapter after signature verification:

- Stripe event id
- Event kind: payment confirmed, payment failed, refund or dispute confirmed, or ignored
- External payment id
- External adjustment id when applicable
- Tenant id, pack id, and actor user id from metadata
- Occurred UTC from the event

Ignored events are acknowledged and not written to the ledger. Failed asynchronous payments are acknowledged and do not grant.

## Paid expiry work item

Not a stored queue. The Azure Functions timer calls `ExpirePaidCreditLotsUseCase`, which loads paid lots whose `ExpiresAtUtc` is at or before `asOfUtc` and whose `RemainingQuantity` is greater than zero, then updates each lot in the shared `tables` ledger. A lot that loses the race with consumption retries with the existing optimistic concurrency pattern (ETag, bounded retries). Failure for one tenant is logged and the sweep continues. No balance change without a `PaidExpiry` transaction. The timer does not depend on a web replica being awake.

Finding lots requires a cross-partition query on `lot|` rows (`LotType = Paid`, remaining greater than zero, expiry due). Acceptable at current volume. Do not add a second storage account for an expiry index in this feature.

## Commercial account record

Unchanged. Delete sets the account deleted and writes an audit event. Purge removes expired account rows and expired audit rows. Neither operation takes a ledger or billing dependency.

## State transitions (paid lot remaining)

```text
checkout created        → no lot
payment confirmed       → lot remaining = granted quantity (one row)
usage (existing FIFO)   → remaining decreases by consumed credits
refund or lost dispute  → remaining decreases by unused quantity only (once)
expiry when unused > 0  → remaining = 0 via PaidExpiry
expiry when unused = 0  → no further transaction
account deleted         → lot unchanged
```
