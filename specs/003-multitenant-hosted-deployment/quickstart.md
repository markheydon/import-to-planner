# Quickstart: Hosted Deployment Strategy Verification

**Feature**: 003-multitenant-hosted-deployment  
**Date**: 2026-05-16

## Purpose

Use this guide to review the planned hosted deployment strategy before implementation work
changes the AppHost, deployment workflows, or tenant-metadata storage.

## Review Checklist

### 1. Confirm the two deployment modes

- Self-hosted single-tenant remains supported for organisation-run deployments.
- Shared hosted multi-tenant becomes the Azure reference deployment model.

### 2. Confirm the hosted baseline architecture

- Aspire continues to model a single `web` resource initially.
- Azure Container Apps is the planned public hosted compute target.
- Azure Table Storage is the planned store for minimal tenant metadata only.
- Platform-managed settings hold configuration and secrets.
- Key Vault is deferred unless certificate handling requires it.
- Existing OTLP routing is reused before adding paid monitoring services.

### 3. Confirm the environment model

- `beta` is the single shared non-production environment.
- `production` is a separate manually promoted environment.
- `beta` and `production` keep separate secrets, identity settings, and telemetry routing.

### 4. Confirm the promotion flow

1. CI validates restore, format, build, AppHost build, and tests.
2. GitHub Actions deploys automatically to `beta`.
3. `beta` smoke checks pass.
4. A manual approval promotes the release to `production`.

### 5. Confirm hosted operational expectations

- Smoke checks cover sign-in, validation, preview, execution reporting, consent guidance,
  and telemetry flow.
- Telemetry uses pseudonymous tenant correlation.
- Support resolution back to a tenant is access-controlled.
- Imported task content is not part of hosted persistent storage.

## Document Review Evidence

Reviewers should be able to answer yes to each of the following:

- Is the hosted public endpoint owner clear?
- Is the minimal tenant metadata location clear?
- Is the first hosted environment strategy intentionally small?
- Is the production promotion path separated from automatic deployment?
- Are cost controls explicit enough to prevent premature infrastructure growth?
- Are privacy and tenant-correlation expectations explicit enough for operations planning?
