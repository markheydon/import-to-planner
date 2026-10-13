---
title: Privacy and security
---

This page explains, in plain language, how Import To Planner handles data and permissions
for the **hosted service** operated by MHCG LTD. For contractual terms, see
[Terms of use](./terms).

## What the app reads

The app reads Microsoft 365 and Planner context needed to let you choose where to import and to validate your request.

## What the app writes

When you confirm an import, the app writes planner task updates to the destination plan in Microsoft Graph.

## Data storage boundaries

Imported Planner/task data is not persisted as application data in the application database.
CSV content you upload is processed in memory for validation and preview; it is not retained
as a long-term file store after your session completes an import or you navigate away.

Planner tasks created or updated during a confirmed import remain in your Microsoft 365 tenant
under your organisation's Microsoft retention policies.

Where the hosted commercial service is enabled, we store **commercial account metadata**
needed to operate your organisation's access (for example tenant and user identifiers and
account lifecycle state). We also maintain a **per-tenant credit ledger** that records
balances, grants, usage, and expiry. The ledger reflects how many import credits your
organisation has used; it does **not** store the contents of your CSV files or the text of
Planner tasks you import.

## Retention and logging

Operational telemetry (for example request traces, error diagnostics, and hosting platform
logs) may be retained for a limited period to operate and secure the service. Logs are used
for reliability and incident response, not for advertising.

If you delete your commercial account, access ends immediately. Your **account** record
(sign-in and profile state) is retained for about **six months** so you can restore it
during that window, after which eligible **account** data may be purged. That six-month
window applies to your account row only.

**Credit balances** belong to your Microsoft 365 **organisation (tenant)**, not to your
individual sign-in. Deleting your account does not reset or remove your organisation's
credit ledger; other users in the same tenant share the same balance.

When paid billing is enabled, we also keep **organisation-scoped payment metadata** (for
example Stripe customer and receipt references) so we can support receipts, refunds,
disputes, and tax. That metadata is not removed when one user deletes their account.

## Subprocessors

The hosted service relies on Microsoft **Entra ID** for sign-in and **Microsoft Azure** for
hosting and operational storage. Where paid credits are offered, **Stripe** processes
payments and issues receipts on our behalf.

## Demonstration mode

Authorised operators may enable an in-app demonstration mode on non-production deployments.
Demonstration mode uses synthetic data only and does not call Microsoft Graph for import
steps. Demonstration mode is off by default at sign-in and is cleared when you sign out.

## Credentials and secrets

Credentials and secrets are not stored in the application repository.

## Delegated permissions summary

The app uses delegated Microsoft Graph permissions so actions run in the context of the signed-in user.

In plain terms, permissions are used to:

- Read your relevant group and planner context.
- Create or update planner tasks only when you confirm an import.

If your tenant requires administrator approval, your administrator must grant consent before users can continue.

## Operational safety

The workflow includes validation and preview before execution so users can confirm changes before write actions are performed.
