# Microsoft Graph Guidelines

Repository guidance for using Microsoft Graph in this project.

Purpose
-------
This document provides practical guidance for consuming Microsoft Graph within
Import To Planner. It is an implementation-level guidance document. Stack-independent
architecture rules live in `.specify/memory/constitution.md`. This repository's
named Graph, Kiota, and layer-map requirements live in
`docs-internal/engineering-policies.md`.

Principles
----------
- Treat `Microsoft.Graph` as the Planner adapter SDK, not as the contract for
  Domain or Application logic. Repository-owned types are the inner-layer contract.
- Keep Kiota as an internal SDK implementation detail; do not expose Kiota types
  in `Application` or `Domain`.
- Do not reference Graph or Kiota-generated types outside
  `ImportToPlanner.Infrastructure.Graph` (and Web only where authentication
  composition requires a Graph client at the host boundary). Map Graph responses
  to domain or application models at the adapter boundary.
- Prefer adapter and mapping layers in `ImportToPlanner.Infrastructure.Graph`
  over leaking SDK types upward.
- Accept that `Microsoft.Graph` may bring Kiota transitively; design Domain and
  Application so they do not depend on those types.

Where to place Graph code
-------------------------
- Implement Graph calls and any Kiota-specific code inside `ImportToPlanner.Infrastructure.Graph`.
- Map Graph DTOs to domain types before passing data into `ImportToPlanner.Application` or `ImportToPlanner.Domain`.
- Keep configuration, authentication, and token handling inside infrastructure code — avoid leaking secrets or SDK types into higher layers.

Testing and safety
------------------
- Use explicit test doubles at `IPlannerGateway` and `ITenantOperationalMetadataStore` boundaries for integration-style tests that should not contact real Graph endpoints.
- Unit-test orchestration and business logic by mocking `IPlannerGateway` implementations.
- Do not commit tenant credentials or secrets; follow the repository's configuration patterns for local dev and CI.

Hosted multi-tenant compatibility
---------------------------------
- Keep Graph operations bound to the active delegated tenant session from the signed-in user.
- Reject unsupported account types before entering planner workflow operations.
- Resolve consent outcomes into repository-owned contracts before presenter mapping; do not surface raw provider exception text.
- Keep hosted telemetry privacy-safe: include authority classification, tenant-safe key, consent status, and failure category only.
- Validate planner behaviour changes through the single supported Graph path plus authority-specific guard scenarios.

Commercial account storage boundary
----------------------------------
- Session-only identity context can include display email and tenant name for UI reassurance.
- Persisted commercial account records must remain minimal (`TenantId`, `UserId`, `CreatedUtc`, lifecycle state).
- Do not persist display email, tenant display name, or other profile text in commercial account storage for this release.
- Audit records should store stable outcome codes and timestamps, not user-facing message prose.

Authority and consent matrix
----------------------------

| Operating mode | Authority configuration | Supported account types in Entra | Consent expectation |
| --- | --- | --- | --- |
| Self-hosted single-tenant | `AzureAd:TenantId=<tenant>` and `AzureAd:HomeTenantId=<tenant>` | Accounts in this organisational directory only | Tenant owner grants or delegates consent inside the same tenant before use |
| Hosted shared multi-tenant | `AzureAd:TenantId=<app-registration-tenant>` and `AzureAd:HomeTenantId=multiple` | Accounts in any organisational directory | Users can complete delegated consent when tenant policy allows it; otherwise the app must present an administrator-consent path |

Operational notes:
- Keep the hosted app registration and the self-hosted app registration separate by default so a tenant-owned self-hosted deployment does not inherit shared hosted consent and redirect-URI requirements.
- The hosted deployment must keep `AzureAd:TenantId` aligned to the app-registration tenant and set `AzureAd:HomeTenantId=multiple` for shared hosted sign-in.
- Keep the delegated Graph scope set aligned with `src/ImportToPlanner.Web/appsettings.json`: `User.Read`, `Group.Read.All`, `GroupMember.Read.All`, and `Tasks.ReadWrite`.

Further reading
---------------
- See `.specify/memory/constitution.md` for stack-independent architecture
  rules (inward dependencies, adapter boundaries, security, and testability).
- See `docs-internal/engineering-policies.md` for this repository's Graph,
  Kiota, and architecture-evidence requirements.
- Use `aspire docs get` or Microsoft Learn for up-to-date Graph API guidance when needed.
