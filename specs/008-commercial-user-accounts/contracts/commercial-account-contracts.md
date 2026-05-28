# Commercial Account Contracts

This document records the planned boundary contracts for feature 008. They are
application-facing and deployment-facing contracts, not final API signatures.

## 1. Deployment Configuration Contract

Purpose: Control whether a deployment runs the hosted commercial account flow or
the existing self-hosted sign-in flow.

Inputs:

| Setting | Source | Required | Notes |
|---------|--------|----------|-------|
| `Parameters:enableCommercialMode` | Aspire parameter | Yes for hosted deployments | Forwarded by AppHost to the web app. |
| `Features:CommercialMode:Enabled` | Web configuration | Yes | Bound from the AppHost environment variable. |
| `Parameters__enableCommercialMode` | GitHub Actions env var | Yes in staging when commercial mode is desired | Enables staged rollout without code changes. |

Rules:

- When `Enabled = false`, the web app must preserve the existing self-hosted sign-in
  behaviour.
- When `Enabled = true`, the web app must present the commercial account flow and
  enforce account lifecycle rules after authentication.

## 2. Current Identity Contract

Purpose: Provide Application with the minimum neutral identity information needed
to resolve or create a commercial account.

Request shape:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `TenantId` | string | Yes | Derived from authentication claims. |
| `UserId` | string | Yes | Derived from authentication claims. |
| `EmailAddress` | string? | No | Display-only session context. |
| `TenantName` | string? | No | Display-only session context. |

Rules:

- Application depends on `TenantId` and `UserId` only for persistence.
- Email address and tenant name are session-only data and must not be required for
  stored account creation.

## 3. Commercial Access Resolution Contract

Purpose: Decide what the web layer should do for an authenticated user.

Request:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `TenantId` | string | Yes | Account lookup key. |
| `UserId` | string | Yes | Account lookup key. |
| `CommercialModeEnabled` | bool | Yes | Outer-layer mode input. |
| `OccurredUtc` | `DateTimeOffset` | Yes | Used for audit events and retention checks. |

Response:

| Field | Type | Meaning |
|-------|------|---------|
| `Decision` | enum | `Allow`, `CreateAccount`, `BlockedDeleted`, `OfferRestore`, `SelfHostedBypass` |
| `RetentionExpiresUtc` | `DateTimeOffset?` | Present for deleted-account responses. |
| `ShouldSignOut` | bool | Indicates whether the current authenticated session should be ended. |
| `AuditEvent` | value object | The sign-in outcome event to persist. |

Rules:

- The response must not contain UI text.
- `SelfHostedBypass` must be emitted only when commercial mode is disabled.
- `CreateAccount` both permits access and causes account creation with an audit
  event.

## 4. Account Lifecycle Contracts

### Delete Account

Request:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `TenantId` | string | Yes | Account key. |
| `UserId` | string | Yes | Account key. |
| `OccurredUtc` | `DateTimeOffset` | Yes | Basis for retention deadline. |

Outcome:

- Mark the account as deleted immediately.
- Compute `RetentionExpiresUtc = OccurredUtc + 6 months`.
- Emit an `AccountDeleted` audit event.
- Tell the web layer to end the current signed-in access.

### Restore Account

Request:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `TenantId` | string | Yes | Account key. |
| `UserId` | string | Yes | Account key. |
| `OccurredUtc` | `DateTimeOffset` | Yes | Restore timestamp. |

Outcome:

- Reactivate the same account record if the retention window is still open.
- Clear deleted-state fields as defined by the storage model.
- Emit an `AccountRestored` audit event.

## 5. Retention Sweep Contract

Purpose: Permanently remove deleted accounts whose retention window has expired.

Request:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `AsOfUtc` | `DateTimeOffset` | Yes | Cut-off for expiry checks. |
| `BatchSize` | int | No | Optional operational limit. |

Outcome:

- Remove expired deleted accounts.
- Optionally remove audit records whose 12-month retention has expired in the same
  scheduled process or a companion job.
- Emit diagnostics suitable for operators, without exposing secrets or user-facing
  prose.

Execution options:

- Baseline: commercial-only hosted service inside the web app.
- Future option: Azure Functions project added to AppHost with explicit host
  storage and references to the existing storage resources.

## 6. Storage Adapter Contract

Purpose: Persist commercial accounts and audit events in the existing Azure
Storage account.

Decisions:

- Use one Aspire `tables` reference and dedicated table names for accounts and
  audits.
- Keep blob storage out of structured account persistence.
- Keep keyed client registration optional, not required.

Suggested adapter responsibilities:

- `ICommercialAccountStore`: get, create, mark deleted, restore, purge expired.
- `ICommercialAuditStore`: append event, query retention candidates, purge expired.

Storage invariants:

- The account identity key is always `TenantId + UserId`.
- Audit events must include `TenantId`, `UserId`, timestamp, and outcome.
- Table schemas must support retention queries without scanning unrelated tenants
  wherever practical.
