# Microsoft Graph Guidelines

Repository guidance for using Microsoft Graph in this project.

Purpose
-------
This document provides practical guidance for consuming Microsoft Graph within Import To Planner. It is an implementation-level guidance document; governance-level requirements live in `.specify/memory/constitution.md`.

Principles
----------
- Treat `Microsoft.Graph` as the primary contract for domain and application logic.
- Keep Kiota as an internal SDK implementation detail; do not expose Kiota types in `Application` or `Domain` layers.
- Avoid referencing Kiota-generated types outside `Infrastructure` implementations. Map Graph responses to domain models at the infrastructure boundary.
- Remove explicit Kiota usage from higher-level code where practical; prefer adapter/mapping layers in `ImportToPlanner.Infrastructure.Graph`.
- Accept that `Microsoft.Graph` v5 may bring Kiota transitively; design domain and application code so it does not depend on Kiota types.

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
- See `.specify/memory/constitution.md` for governance-level requirements about Graph integration and observability.
- Use `aspire docs get` or Microsoft Learn for up-to-date Graph API guidance when needed.
