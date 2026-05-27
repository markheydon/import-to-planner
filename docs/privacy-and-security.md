---
layout: page
title: Privacy and security
permalink: /privacy-and-security
---

This page explains, in plain language, how Import To Planner handles data and permissions.

## What the app reads

The app reads Microsoft 365 and Planner context needed to let you choose where to import and to validate your request.

## What the app writes

When you confirm an import, the app writes planner task updates to the destination plan in Microsoft Graph.

## Data storage boundaries

Imported Planner/task data is not persisted as application data.

The service may still keep limited operational logs or telemetry for reliability, diagnostics, and support.

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
