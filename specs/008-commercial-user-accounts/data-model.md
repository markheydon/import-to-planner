# Data Model: Commercial User Accounts

## 1. CommercialAccount

Purpose: The minimal persisted commercial user record that determines whether a
signed-in Microsoft 365 user can access the hosted commercial experience.

Fields:

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `TenantId` | string | Authentication claims | Part of the unique account key. Required. |
| `UserId` | string | Authentication claims | Part of the unique account key. Required. |
| `CreatedUtc` | `DateTimeOffset` | System clock | Required. Immutable after creation. |
| `Status` | enum | Application policy | `Active` or `Deleted`. |
| `DeletedUtc` | `DateTimeOffset?` | System clock | Set when the user deletes their account. |
| `RetentionExpiresUtc` | `DateTimeOffset?` | Application policy | `DeletedUtc + 6 months`. |
| `RestoredUtc` | `DateTimeOffset?` | System clock | Set when a deleted account is restored during the retention window. |
| `LastSignInOutcomeUtc` | `DateTimeOffset?` | Application policy | Optional denormalised aid for support and diagnostics. |

Validation rules:

- `TenantId` and `UserId` must both be non-empty.
- `CreatedUtc` must be set when the record is first persisted.
- `DeletedUtc` and `RetentionExpiresUtc` must either both be null or both be set.
- `RetentionExpiresUtc` must be later than `DeletedUtc`.
- `RestoredUtc` must be null unless the account was previously deleted.

Relationships:

- One `CommercialAccount` has many `AccountAuditEvent` records.
- One current `SessionIdentityContext` resolves to at most one
  `CommercialAccount`.

State transitions:

- `Missing -> Active`: first successful commercial sign-in creates the account.
- `Active -> Deleted`: delete-account confirmation marks the account deleted and
  starts the retention window.
- `Deleted -> Active`: restore during retention reactivates the same record.
- `Deleted -> Purged`: scheduled purge permanently removes the account after the
  retention deadline.

## 2. AccountAuditEvent

Purpose: The retained audit trail for account lifecycle and sign-in outcomes.

Fields:

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `TenantId` | string | Identity context | Required. |
| `UserId` | string | Identity context | Required. |
| `OccurredUtc` | `DateTimeOffset` | System clock | Required. |
| `EventType` | enum | Application policy | `AccountCreated`, `AccountDeleted`, `AccountRestored`, `SignInOutcome`. |
| `Outcome` | string | Application policy | Required short outcome code. |
| `RetentionExpiresUtc` | `DateTimeOffset` | Application policy | `OccurredUtc + 12 months`. |

Validation rules:

- `TenantId`, `UserId`, `OccurredUtc`, `EventType`, and `Outcome` are required.
- `Outcome` must be a stable code or short diagnostic token, not user-facing
  prose.
- `RetentionExpiresUtc` must be later than `OccurredUtc`.

Relationships:

- Many audit events belong to one `CommercialAccount` identity key.

## 3. SessionIdentityContext

Purpose: The current signed-in identity details shown during a session but not
persisted as part of the account record.

Fields:

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `TenantId` | string | Authentication claims | Required for account lookup. |
| `UserId` | string | Authentication claims | Required for account lookup. |
| `EmailAddress` | string? | Claims or Graph | Display-only. Not persisted in account storage. |
| `TenantName` | string? | Claims or Graph | Display-only. Not persisted in account storage. |

Validation rules:

- `TenantId` and `UserId` are required before any commercial access decision is
  evaluated.
- `EmailAddress` and `TenantName` are optional and may be absent without blocking
  the core flow.

## 4. CommercialAccessDecision

Purpose: The repository-owned application response that tells the web layer what
to show after authentication and account lookup.

Fields:

| Field | Type | Notes |
|-------|------|-------|
| `Decision` | enum | `Allow`, `CreateAccount`, `BlockedDeleted`, `OfferRestore`, `SelfHostedBypass` |
| `AccountStatus` | enum? | Mirrors the resolved account status when applicable. |
| `RetentionExpiresUtc` | `DateTimeOffset?` | Used for deleted-account messaging. |
| `ShouldSignOut` | bool | Indicates whether the web layer should end the current session. |

Validation rules:

- `SelfHostedBypass` is only emitted when commercial mode is disabled in the outer
  layer.
- `BlockedDeleted` and `OfferRestore` require `RetentionExpiresUtc`.
- The object must not contain UI-framework types or user-facing message text.

## 5. CommercialModeOptions

Purpose: Outer-layer configuration that determines whether the hosted commercial
account flow is active.

Fields:

| Field | Type | Notes |
|-------|------|-------|
| `Enabled` | bool | Set through Aspire and deployment configuration. |
| `RetentionSweepEnabled` | bool | Optional future flag for scheduled purge execution. |

Validation rules:

- This options type remains in Web or infrastructure-facing configuration only.
- It must not be referenced from Domain or Application models.
