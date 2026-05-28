# Credits, Billing, and Usage Model (V1)

This document describes the agreed design for credits, billing, and usage tracking in *Import to Planner*.

The goals of this model are:
- Fair and transparent usage accounting
- Low friction for new users
- Clear separation between free usage and paid usage
- A robust, auditable foundation from day one

---

## Overview

- The app uses a **credit-based model**
- **1 credit = 1 Planner task successfully created**
- Credits are consumed only after a successful import execution
- Usage is always visible to the user via the import summary

There are **two types of credits**:
- Free monthly credits
- Paid credits (purchased via Stripe)

---

## Free Monthly Credits

### Allocation
- Each tenant is entitled to **25 free credits per calendar month**
- Free credits are **allocated lazily**:
  - They are granted **on the first successful login in a new calendar month**
  - No background process allocates credits for dormant tenants
- The ledger entry for free credits is dated as **the 1st of the month**

### Expiry
- Free credits **expire at the end of the same calendar month**
- Free credits **do not roll over**
- Expiry is explicitly recorded in the credit ledger

### Rationale
- Keeps behaviour simple and predictable
- Avoids unnecessary ledger churn for inactive tenants
- Aligns with the UI promise: *“25 free credits per month”*

---

## Paid Credits

### Purchase
- Paid credits are purchased via **Stripe checkout**
- Credits are added to the tenant account only after a successful Stripe webhook confirmation
- Each purchase creates a distinct credit allocation (credit “lot”) in the ledger

### Expiry
- Paid credits **expire 12 months from the purchase date**
- Expiry is enforced via a scheduled background job
- Expiry is recorded as an explicit ledger transaction (no silent balance changes)

### Consumption Order
- Free credits are **always consumed first**
- Paid credits are consumed **FIFO (oldest purchase first)**

### Rationale
- Paying users are never penalised while free credits are available
- FIFO ensures older credits are used before expiry
- 12‑month expiry keeps the model fair but predictable

---

## Credit Consumption Rules

- Credits are consumed **only after a successful import execution**
- The number of credits consumed equals the number of tasks actually created
- If an import partially succeeds, only successfully created tasks consume credits
- No credits are deducted during validation or dry‑run

---

## Credit Ledger (Source of Truth)

The system maintains an **immutable, transaction-based credit ledger** per tenant.

### Ledger Principles
- All credit changes are recorded as transactions
- Balances are derived from the ledger, not stored as mutable counters
- Corrections are handled via compensating transactions, never edits

### Example Ledger Entries
- Free monthly credit grant
- Paid credit purchase
- Import usage
- Free credit expiry
- Paid credit expiry

This provides:
- Full auditability
- Clear support diagnostics
- The ability to expose a transaction history to users later without redesign

---

## Usage Visibility in the UI

### Import Summary
Every successful import displays a **usage breakdown** in the existing import summary UI.

Example:
- Tasks created: 18
- Credits used:
  - 12 free monthly credits
  - 6 paid credits
- Remaining credits shown after execution

There are:
- No hidden deductions
- No special-case messaging
- No silent usage

This makes the credit model self‑explanatory through repeated use.

---

## Stripe Integration Scope (V1)

Stripe is used **only** for:
- Checkout and payment
- Invoicing and receipts
- VAT / tax handling

Stripe is **not** used for usage metering.

Usage accounting, credit balances, and expiry are owned entirely by the application.

---

## Non-Goals (V1)

- No subscriptions
- No automatic recurring charges
- No background usage metering
- No scheduled imports

These may be revisited in later versions once the core model is proven.

---

## Summary

This model prioritises:
- Simplicity for users
- Fairness for paying customers
- Transparency and trust
- Long-term maintainability

It intentionally avoids “quick counters” in favour of a proper ledger, ensuring the system scales without painful rewrites.

