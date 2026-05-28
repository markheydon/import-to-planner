# Research: Commercial User Accounts

## Decision 1: Model commercial mode as an explicit AppHost parameter and web option

- Decision: Add a non-secret Aspire parameter for commercial mode and forward it
  to the web project as configuration, then pass the same parameter in the
  staging deployment workflow.
- Rationale: The repo already uses AppHost parameters for deployment-time
  behaviour, and the constitution requires hosted-only behaviour to remain
  additive. Keeping commercial mode in AppHost and Web avoids reintroducing a
  cross-layer deployment-mode abstraction that current architecture tests guard
  against.
- Alternatives considered: Infer mode from tenant authority or environment name
  only. Rejected because authority shape is an authentication concern rather than
  a product-mode concern, and environment names are too coarse for feature
  control.

## Decision 2: Keep commercial versus self-hosted branching in the outer layers

- Decision: Treat mode selection as a Web/AppHost concern, and let Application
  receive only neutral account-lifecycle and access-decision contracts.
- Rationale: This preserves clean architecture dependency direction and keeps
  hosted/self-hosted differences from leaking into Domain or Application policy.
  It also matches the current pattern where tenant and consent context are
  exposed through inward-facing interfaces.
- Alternatives considered: Add a general deployment-mode enum to shared models.
  Rejected because the repo already contains tests forbidding reintroduction of
  removed runtime-mode concepts.

## Decision 3: Use Azure Table Storage for commercial account records

- Decision: Store commercial account records in Azure Table Storage, using the
  existing Aspire-managed `tables` resource and a dedicated account table.
- Rationale: Account records are structured, keyed by `TenantId + UserId`, and
  need efficient point lookups plus simple retention queries. Aspire already
  injects `TableServiceClient` for the app, and the repo already uses Azure
  Tables for tenant operational metadata.
- Alternatives considered: Store accounts in Blob storage. Rejected because blob
  objects are a poor fit for keyed, queryable account state and would complicate
  retention and restore logic.

## Decision 4: Use Azure Table Storage for audit records as well

- Decision: Persist account lifecycle and sign-in outcome audit events in a
  separate Azure Table Storage table on the same storage account.
- Rationale: Audit events are structured, append-heavy, retained for 12 months,
  and naturally partition by tenant. Keeping them in Tables avoids introducing a
  second storage technology for the same feature and keeps retention operations
  consistent.
- Alternatives considered: Store audits in Blob storage or application logs
  only. Rejected because the spec requires retained audit records, not just
  transient diagnostics, and Tables better support targeted retrieval.

## Decision 5: Do not require keyed storage clients in the first implementation

- Decision: Continue using one referenced `tables` resource and resolve table
  names inside infrastructure adapters; keep keyed client registration as an
  optional refinement only if multiple independently configured table resources
  are introduced later.
- Rationale: Aspire supports keyed Azure Table and Blob clients, but the current
  app has one storage account and one tables reference. Adding keyed DI now
  increases complexity without solving a real configuration problem.
- Alternatives considered: Switch immediately to keyed Table or Blob clients.
  Rejected because fixed table names under one account are simpler and adequate
  for this feature.

## Decision 6: Express retention policy in Application, but defer separate scheduled compute

- Decision: Model deletion and restore policy in application use cases, and plan
  the first purge execution as commercial-only scheduled work inside the web host
  if required for the first release. Keep Azure Functions as an optional future
  AppHost resource once retention and credits workflows justify separate compute.
- Rationale: The user asked that Azure Functions be the only possible new Aspire
  resource to consider. Aspire supports Functions projects with explicit host
  storage and referenced resources, but adding a new project and compute surface
  now would expand scope beyond the minimum viable commercial account release.
  The underlying policy and storage model can remain stable either way.
- Alternatives considered: Add Azure Functions immediately. Rejected for the
  baseline because the first feature slice only needs low-volume scheduled purge
  work and the repo currently has no other scheduled compute surface.

## Decision 7: Keep Blob storage scoped to data-protection and opaque blobs

- Decision: Continue using Blob storage only for ASP.NET Core data-protection key
  persistence and other opaque blob scenarios, not for account or audit records.
- Rationale: The current web host already depends on Blob storage for data
  protection through the existing storage configuration. Expanding blob usage for
  structured account data would weaken queryability with no compensating benefit.
- Alternatives considered: Move all commercial persistence into blobs to avoid
  two Table adapters. Rejected because the app already has Table integration and
  structured records are the dominant storage shape here.
