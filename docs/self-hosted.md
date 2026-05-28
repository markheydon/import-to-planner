---
layout: page
title: Self-hosted
permalink: /self-hosted
---

This is secondary guidance for teams running their own Import To Planner deployment.

If you are using the hosted service, go to [Getting started](./getting-started).

## Who this page is for

Use this page if your organisation plans to deploy and operate its own instance.

## What to expect

Self-hosted setup involves environment configuration, identity setup, and operational ownership by your team.

Self-hosting is a permanent supported path for this project. Commercial or hosted-only
capabilities may be added over time, but they are intended to remain additive. If a hosted
service introduces workflows that do not apply to self-hosters, self-hosted deployments
must continue to have a supported route that does not depend on those hosted-only steps.

For self-hosted deployments, keep authentication tenant-specific:

- Create or use an Entra app registration owned by your tenant.
- Keep the supported account type single-tenant for that registration.
- Set `AzureAd:TenantId` to your tenant ID or verified domain.
- Add the exact redirect URI for your deployed app, always ending with `/signin-oidc`.
- Grant the delegated Microsoft Graph permissions required by the app before your users start importing.

This route is intentionally different from the hosted shared service. Do not point a self-hosted deployment at the hosted multitenant app registration.

## Where to continue

- Start with the public repository README for prerequisites and run options: [import-to-planner README](https://github.com/markheydon/import-to-planner/blob/main/README.md).
- Use your own tenant-owned app registration rather than the hosted shared registration.
- If your team is self-hosting, use your own deployment runbook for environment-specific operations and ownership.

This self-hosted route is intentionally separate so the main docs path stays focused on hosted end users without making self-hosting a second-class option.
