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

## Retention and logging

Operational telemetry (for example request traces, error diagnostics, and hosting platform
logs) may be retained for a limited period to operate and secure the service. Logs are used
for reliability and incident response, not for advertising.

Where commercial mode is enabled, billing-related metadata may be stored according to the
commercial account configuration described in engineering documentation for operators.

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
