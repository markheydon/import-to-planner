# Data Model: Isolate Commercial Accounts (Single Hosted Process)

Persisted shapes are unchanged from `specs/008-commercial-user-accounts/data-model.md`. This feature changes **ownership**, not fields. Existing commercial records remain valid.

## Ownership

| Entity | Owner after this feature | Notes |
|--------|--------------------------|-------|
| `CommercialAccount` | `ImportToPlanner.Commercial` | Leave Application. |
| `AccountAuditEvent` | `ImportToPlanner.Commercial` | Leave Application. |
| `CommercialAccessDecision` | `ImportToPlanner.Commercial` | Structured result for Web; no UI text. |
| Commercial store and lifecycle contracts | `ImportToPlanner.Commercial` | Access, profile, delete, restore, purge. |
| Table adapters (accounts, audit, hosted tenant metadata) | `ImportToPlanner.Commercial` | Azure Tables only when commercial mode is on. |
| `SessionIdentityContext` | Application (shared) | Session display and lookup keys; not a commercial account record. |
| `ITenantOperationalMetadataStore` / `TenantOperationalMetadata` | Application (shared) | Implemented by Graph (self-host) or Commercial (table-backed). |
| Self-host tenant-metadata adapter | `ImportToPlanner.Infrastructure.Graph` | No Tables. |
| Unused extra commercial process | Deleted | Not a data entity; removed from topology. |

## 1. CommercialAccount

Unchanged fields and transitions from 008:

- Key: `TenantId` + `UserId` (both required, non-empty).
- Stored: `CreatedUtc`, `Status` (`Active` / `Deleted`), `DeletedUtc`, `RetentionExpiresUtc` (`DeletedUtc` + 6 months), optional `RestoredUtc`, optional `LastSignInOutcomeUtc`.
- Transitions: Missing → Active (first commercial sign-in); Active → Deleted (user delete); Deleted → Active (restore in window); Deleted → Purged (sweep after expiry).

Validation: `DeletedUtc` and `RetentionExpiresUtc` are both null or both set; expiry is later than delete.

## 2. AccountAuditEvent

Unchanged from 008:

- `TenantId`, `UserId`, `OccurredUtc`, `EventType` (`AccountCreated`, `AccountDeleted`, `AccountRestored`, `SignInOutcome`), `Outcome` (stable code, not prose), `RetentionExpiresUtc` (`OccurredUtc` + 12 months).

## 3. SessionIdentityContext

Unchanged from 008. Remains an Application model so Home identity chrome can use it without referencing Commercial. Commercial use cases accept it as input. Email and tenant name stay session-only and are not persisted on `CommercialAccount`.

## 4. TenantOperationalMetadata

Unchanged hosted operational metadata from 004/008. Abstraction stays in Application. **Table** persistence moves to Commercial and is registered only in commercial mode. Self-host continues to use the Graph in-process adapter without Tables.

## 5. Deployment Access Mode

Not persisted. `Features:CommercialMode:Enabled` (from AppHost `enableCommercialMode`) selects:

- On: Commercial module + `tables` on `web` only.
- Off: no Commercial persistence, no `tables`, Graph self-host metadata only.
