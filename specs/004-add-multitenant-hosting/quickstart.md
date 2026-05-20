# Quickstart: Hosted Multi-Tenant Support Verification

## Purpose

Use this guide after implementation to verify that the app supports both
self-hosted single-tenant and hosted shared multi-tenant operation without
regressing the current import workflow.

## Prerequisites

- .NET 10 SDK installed.
- Aspire CLI available for local orchestration and deployment workflows.
- Repository dependencies restored.
- Two configuration sets available:
  - Self-hosted single-tenant settings.
  - Hosted shared multi-tenant settings.
- For hosted-mode local verification, the AppHost must provide the storage-backed
  support resource path required by the implementation, using local emulation when
  Azure resources are not being used directly.

## Recommended Verification Order

## Verification checkpoints

Use this checklist to capture explicit hosted and self-hosted evidence before sign-off.

| Checkpoint | Command or action | Evidence to capture |
| --- | --- | --- |
| Automated regression baseline | `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj` and `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj` | Passing test output for tenant context, consent handling, and workflow behaviour |
| Architecture boundary check | `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj --filter FullyQualifiedName~ArchitectureComplianceTests` | Passing architecture-boundary assertion output |
| Self-hosted runtime-mode verification | Run focused planner-behaviour tests with `PlannerGateway__UseGraph=false` and `PlannerGateway__UseGraph=true` | Matching workflow semantics and no runtime-mode regressions |
| Hosted sign-in and tenant isolation | Exercise hosted sign-in with users from at least two supported tenants | Captured evidence that tenant metadata and workflow context remain isolated |
| Hosted consent and admin path | Trigger both user-consent and admin-consent-required flows | Captured guidance output with a clear administrator path and no unhandled failure |
| Hosted telemetry and privacy | Inspect logs/traces exported by OTLP pipeline | Evidence of tenant-safe dimensions and absence of secret values |

### 1. Run focused automated tests first

```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj
```

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj
```

Expected evidence:
- Deployment-mode tests verify self-hosted and hosted authority selection.
- Tenant-isolation tests verify one tenant cannot view or mutate another tenant's
  metadata.
- Consent-handling tests verify user-consent success, administrator-consent
  guidance, and decline/failure behaviour.
- Existing import workflow tests still verify validation, preview, confirmation,
  and execution semantics.

### 2. Verify architecture and boundary evidence

Examples of expected checks:

```bash
rg -n "Microsoft\.Identity|Microsoft\.Graph|Azure\.|MudBlazor" src/ImportToPlanner.Application src/ImportToPlanner.Domain
```

```bash
rg -n "TenantId|ConsentState|DeploymentMode" src/ImportToPlanner.Application src/ImportToPlanner.Web src/ImportToPlanner.Infrastructure.Graph
```

Expected evidence:
- Microsoft identity, Graph SDK, Azure SDK, and UI-library specifics stay out of
  Domain/Application except through explicit repository-owned abstractions.
- Deployment-mode, tenant-context, and consent contracts are explicit and mapped
  at outer-layer seams.

### 3. Verify self-hosted single-tenant mode through Aspire

Suggested flow:

```bash
aspire start --isolated
aspire describe
```

Configure self-hosted mode with a concrete tenant ID and the expected Graph-mode
settings, then verify:

1. Sign-in still targets the configured single tenant.
2. Existing validation, preview, confirmation, and execution flow still behaves as
   before.
3. No hosted-only metadata or unsupported-account messaging appears in the normal
   self-hosted path.

### 4. Verify hosted shared mode through Aspire

Suggested flow:

```bash
aspire start --isolated
aspire describe
```

Configure hosted mode with the shared work-or-school authority and the hosted
storage path, then verify:

1. A supported work or school user from tenant A can sign in and reach the import
   workflow.
2. A supported work or school user from tenant B can do the same without separate
   deployment configuration.
3. Unsupported account types are rejected before workflow entry with clear UK-
   English guidance.
4. A tenant with user consent enabled can satisfy delegated consent and continue.
5. A tenant requiring administrator approval sees a clear administrator path rather
   than an unhandled failure.
6. Returning under a different tenant context does not reuse prior tenant metadata.

### 5. Verify planner runtime-mode parity where planner behaviour changed

Run the affected planner-facing test slice, or equivalent narrow checks, with:

- `PlannerGateway:UseGraph=false`
- `PlannerGateway:UseGraph=true`

Expected evidence:
- Hosted and self-hosted workflow behaviour remains consistent.
- Provider-specific translation stays in adapters.
- Consent and tenant-context guidance does not break in-memory runtime-mode tests.

### 6. Verify hosted telemetry and retained-data boundaries

Check logs, traces, or metrics from the configured OTLP path and confirm:

1. Deployment mode, tenant-safe correlation, consent outcome, and failure category
   are visible.
2. No raw access tokens, certificate secrets, preview payloads, or unnecessary
   tenant-sensitive values are emitted.
3. Persisted hosted metadata contains only tenant-scoped configuration, consent
   state, and support diagnostics.

## Review Checklist

- Self-hosted single-tenant behaviour remains intact.
- Hosted shared mode admits supported work or school tenants on first use.
- Unsupported account types are blocked before workflow access.
- Tenant metadata remains isolated by tenant boundary.
- Consent handling supports both user-consent and administrator-consent-required
  paths.
- Validation, preview, confirmation, and execution semantics remain unchanged.
- Runtime-mode parity is verified when planner-facing behaviour changes.
- New wording and documentation remain in UK English.

## Deployment Readiness Notes

- Keep the first hosted rollout to `Staging` and `Production` only, following the
  repository's Aspire production-readiness guidance.
- Keep the first hosted rollout on the approved low-cost baseline: Azure Container
  Apps plus one Azure Storage account.
- Add further always-on resources only when measurable operational demand justifies
  them.
