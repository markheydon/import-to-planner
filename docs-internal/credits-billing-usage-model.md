# Credits, Billing, and Usage Model (V1)

This document describes the agreed design for credits, billing, and usage tracking in *Import to Planner*.

The goals of this model are:
- Fair and transparent usage accounting.
- Low friction for new users.
- Clear separation between free usage and paid usage.
- A robust, auditable foundation from day one.

---

## Scope

- Credits apply to the **hosted commercial** product only.
- **Self-hosted** deployments do not use credits or Stripe.
- There is **no separate free-account SKU**. Every commercial tenant is the same kind of account: they receive a monthly free credit allowance, then buy paid credits if they need more.
- The free path is measured in **credits (tasks created)**, never as “one free import”. An import has no natural size cap, so an import-count gift would be unbounded.

---

## Overview

- The app uses a **credit-based model**.
- **1 credit = 1 Planner task successfully created**.
- Credits are consumed only when tasks are actually created during import execution.
- Usage is always visible to the user via preview warnings (when relevant) and the import summary.

There are **two types of credits**:
- Free monthly credits.
- Paid credits (purchased via Stripe).

---

## Free Monthly Credits

### Allocation
- Each commercial tenant is entitled to **25 free credits per calendar month**.
- The grant is **not prorated**. A tenant whose first login in the month is on the 16th still receives the full 25 for that month.
- Free credits are **allocated lazily**: they are granted **on the first successful login in a new calendar month**.
- No background process allocates credits for dormant tenants.
- The ledger entry is dated at the **actual grant instant** (that first successful login), not backdated to the 1st of the month. History must match what the user experienced.

### Expiry
- Free credits **expire at the end of the same calendar month**, regardless of when they were granted.
- Free credits **do not roll over**.
- Expiry is explicitly recorded in the credit ledger.

### Rationale
- A monthly task allowance lets someone try the product or run a small one-off without a card, without inventing a second account type.
- Lazy allocation avoids ledger churn for inactive tenants.
- Dating the grant as the real login avoids a transaction history that says “1 September” when the tenant first appeared on the 16th.
- Aligns with the UI promise: *“25 free credits per month”* (unused credits do not carry over).

The monthly quantity (25) is a starting figure. Raising it later is the right way to make a typical one-off import fit; do not replace it with a free-import count.

---

## Paid Credits

### Purchase
- Paid credits are purchased via **Stripe checkout**.
- Credits are added to the tenant account only after a successful Stripe webhook confirmation.
- Each purchase creates a distinct credit allocation (credit “lot”) in the ledger.

### Expiry
- Paid credits **expire 12 months from the purchase date**.
- Expiry is enforced via a scheduled background job.
- Expiry is recorded as an explicit ledger transaction (no silent balance changes).

### Consumption Order
- Free credits are **always consumed first**.
- Paid credits are consumed **FIFO (oldest purchase first)**.

### Rationale
- Paying users are never penalised while free credits are available.
- FIFO ensures older credits are used before expiry.
- 12‑month expiry keeps the model fair but predictable.

---

## Preview, execute, and insufficient credits

Preview (dry-run) **always runs** and **never consumes credits**.

The credit check uses the preview count of tasks that **would be created**, not CSV row count and not reused or skipped rows.

If that created-count is **greater than the remaining balance**:
- Show a prominent warning on the preview step (would create N, remaining M, shortfall).
- Offer a path to buy credits.
- **Disable Confirm import** until the remaining balance covers N (after purchase and a refreshed balance).

There is **no overage or overdraft** in V1 (including no “within 10%” allowance). Percentage slack is hard to explain, scales with pack size, and fights the “no hidden deductions” rule.

During execution:
- Charge **only tasks actually created**.
- If the run creates fewer than preview, unused credits remain.
- If the balance reaches zero mid-run, **do not start creating further tasks**. Do not finish the remainder at a loss.

---

## Credit Consumption Rules

- Credits are consumed **only during import execution**, and only for tasks that were successfully created.
- If an import partially succeeds, only successfully created tasks consume credits.
- No credits are deducted during validation or preview.
- No silent usage, no hidden deductions, no undocumented grace.

---

## Credit Ledger (Source of Truth)

The system maintains an **immutable, transaction-based credit ledger** per tenant.

### Ledger Principles
- All credit changes are recorded as transactions.
- Balances are derived from the ledger, not stored as mutable counters.
- Corrections are handled via compensating transactions, never edits.

### Example Ledger Entries
- Free monthly credit grant (timestamp = grant instant).
- Paid credit purchase.
- Import usage.
- Free credit expiry (end of calendar month).
- Paid credit expiry.

This provides:
- Full auditability.
- Clear support diagnostics.
- The ability to expose a transaction history to users later without redesign.

---

## Usage Visibility in the UI

### Preview
When the would-create count exceeds remaining credits, the preview step shows a clear warning and blocks confirm, with remaining balance and shortfall visible.

### Import Summary
Every successful import displays a **usage breakdown** in the existing import summary UI.

Example:
- Tasks created: 18
- Credits used:
  - 12 free monthly credits
  - 6 paid credits
- Remaining credits shown after execution

There are:
- No hidden deductions.
- No special-case overage messaging.
- No silent usage.

This makes the credit model self‑explanatory through repeated use.

---

## Stripe Integration Scope (V1)

Stripe is used **only** for:
- Checkout and payment.
- Invoicing and receipts.
- VAT / tax handling.

Stripe is **not** used for usage metering.

Usage accounting, credit balances, and expiry are owned entirely by the application.

---

## Non-Goals (V1)

- No free-account SKU (monthly free credits on every commercial tenant instead).
- No “one free import” allowance.
- No prorating of the monthly free grant.
- No credit overage or overdraft (including percentage slack).
- No subscriptions.
- No automatic recurring charges.
- No background usage metering.
- No scheduled imports.
- No credits or Stripe on self-hosted deployments.

These may be revisited in later versions once the core model is proven.

---

## Summary

This model prioritises:
- Simplicity for users.
- Fairness for paying customers.
- Transparency and trust.
- Long-term maintainability.

It intentionally avoids “quick counters” in favour of a proper ledger, ensuring the system scales without painful rewrites.
