# Research: Hosted Multi-Tenant Support

## Decision 1: Use deployment-mode-aware authority selection with work-or-school-only hosted sign-in

- Decision: Keep self-hosted mode single-tenant by configuration and use a hosted
  shared authority that accepts work or school accounts only (`organizations`-style
  multi-tenant authority), rejecting unsupported account types before the import
  workflow begins.
- Rationale: Microsoft Entra guidance distinguishes single-tenant and multi-tenant
  audiences, and the feature spec explicitly excludes personal Microsoft accounts.
  Using a deployment-mode switch avoids branching the product into separate codebases.
- Alternatives considered: Use `common` for all hosted traffic. Rejected because it
  also admits personal Microsoft accounts, which the feature explicitly disallows.

## Decision 2: Establish tenant context from sign-in claims and treat mismatches as new tenant contexts

- Decision: Resolve the active tenant context from the authenticated user's claims
  at sign-in and carry it through the session as a repository-owned tenant context.
  If a returning user's current sign-in resolves to a different tenant, treat that
  as a fresh tenant context and do not reuse previous tenant metadata automatically.
- Rationale: The `tid` claim is the authoritative tenant boundary for the user's
  current session and does not require an extra Graph lookup. This matches the spec's
  tenant-mismatch requirement while keeping tenant resolution inexpensive.
- Alternatives considered: Reuse the most recent stored tenant metadata for the
  same user object ID. Rejected because user identities can appear in multiple
  tenants and would risk cross-tenant leakage.

## Decision 3: Use delegated consent first, then provide an explicit administrator fallback path

- Decision: Preserve delegated Microsoft Graph access using the signed-in user's
  token, allow standard delegated consent when the customer's tenant policy permits
  it, and provide a clear administrator consent path when consent is blocked,
  declined, or otherwise unavailable.
- Rationale: Microsoft Entra guidance for multi-tenant apps recommends handling both
  user consent and admin-consent-required scenarios explicitly. This matches FR-006
  and FR-007 while staying aligned with the app's existing delegated Graph model.
- Alternatives considered: Require administrator consent up front for every hosted
  tenant. Rejected because it adds avoidable friction for tenants that allow user
  consent and conflicts with the feature's first-use goal.

## Decision 4: Persist only tenant-scoped operational metadata, and keep it in Azure Table Storage

- Decision: Persist hosted operational metadata only when needed for tenant-scoped
  configuration, consent state, and support diagnostics, using Azure Table Storage
  keyed by tenant ID. Do not persist per-user usage history, import-content history,
  preview history, or reporting history.
- Rationale: Azure Table Storage is the lowest-cost credible Azure persistence
  option for sparse metadata and fits the feature's minimal-retention requirement.
  It also aligns with the repository's hosted reference guidance, which names Azure
  Table Storage as the approved baseline.
- Alternatives considered: Azure SQL, Redis, or Cosmos DB. Rejected because the
  workload is too small to justify their baseline cost or operational complexity.

## Decision 5: Use Azure Container Apps as the hosted baseline, deployed through Aspire

- Decision: Use Azure Container Apps as the hosted deployment target and keep the
  first rollout on a scale-to-zero-capable, low-cost profile with `minReplicas=0`
  and an initial single active web replica. Use Aspire for local orchestration and
  for publish-time deployment of the required Azure resources.
- Rationale: Azure Container Apps supports scale-to-zero, which directly matches
  the user's cost constraint. The repository already has an Aspire AppHost and
  production-readiness guidance that keeps the first hosted rollout centred on a
  single `web` resource.
- Alternatives considered: Azure App Service, AKS, or a more complex multi-service
  topology. Rejected because they impose higher baseline cost or operational burden
  without a current demand signal.

## Decision 6: Use one Azure Storage account for both hosted metadata and data-protection persistence

- Decision: Add one Azure Storage account to the hosted baseline. Use Table Storage
  for tenant-scoped operational metadata and Blob storage for ASP.NET Core data-
  protection keys so hosted sign-in and anti-forgery behaviour survive restarts and
  future scaling. Keep Azure Key Vault optional rather than baseline.
- Rationale: Microsoft guidance for ASP.NET Core on Azure Container Apps requires
  centralised data-protection persistence when the app can restart or scale.
  Reusing the same storage account keeps the resource footprint and cost low while
  avoiding a second always-on platform dependency.
- Alternatives considered: Keep data-protection keys local to the container.
  Rejected because restart and scale events would invalidate protected state. Add
  Key Vault to the baseline. Rejected because the repository guidance says to add it
  only when certificate or key-protection requirements genuinely justify it.

## Decision 7: Keep the initial hosted rollout on in-memory token caching and design a future cache seam

- Decision: Retain the current in-memory token cache for self-hosted mode and for
  the initial hosted rollout, while introducing deployment-mode-aware configuration
  and a clear seam for a future distributed token cache if multiple active hosted
  replicas are later approved.
- Rationale: Microsoft guidance recommends distributed token caches for web apps
  calling downstream APIs, but the lowest-cost hosted baseline in this repository is
  intentionally a single active web replica. Adding Redis, SQL Server, or Cosmos DB
  now would violate the spec's "no unjustified backing services" constraint.
- Alternatives considered: Add Azure Cache for Redis immediately. Rejected because
  it introduces a material always-on monthly cost before the hosted demand profile is
  known.

## Decision 8: Mirror hosted storage locally through Aspire rather than inventing a separate local path

- Decision: Extend the AppHost so hosted local development also runs through Aspire,
  using a local storage emulator such as Azurite for the storage-account-backed
  concerns while keeping the self-hosted mode able to run through the same AppHost
  without hosted-only resources enabled.
- Rationale: The user explicitly asked for Aspire to be used for both local running
  and deployment of the Azure resources the app needs. Local emulation keeps the
  developer workflow aligned with the hosted topology without incurring Azure cost.
- Alternatives considered: Keep local hosted development outside Aspire. Rejected
  because it would diverge from the repository's deployment orchestration approach.

## Decision 9: Keep hosted telemetry on the existing OpenTelemetry path with tenant-safe enrichment

- Decision: Reuse the existing OTLP-based OpenTelemetry setup and enrich logs,
  metrics, and traces with deployment mode, stable tenant correlation, consent
  outcome, and failure category while excluding tokens, secrets, and unnecessary
  tenant-sensitive values.
- Rationale: The repository already has shared service defaults for telemetry and a
  hosted reference deployment that prefers reusing an existing OTLP collector before
  introducing more paid monitoring services. Tenant-aware supportability can be met
  through carefully chosen telemetry dimensions rather than raw payload logging.
- Alternatives considered: Add a new dedicated monitoring service to the baseline.
  Rejected because it adds cost without being required for the first hosted rollout.

## Repository Policy Checkpoints

- Runtime-mode checkpoint: Any planner-facing behaviour changes still require parity
  evidence in both `PlannerGateway:UseGraph=true` and `PlannerGateway:UseGraph=false`
  modes.
- Hosted rollout checkpoint: The AppHost stays centred on a single `web` resource
  for the first hosted rollout, with staging and production remaining the only
  approved hosted environments.
- Data-retention checkpoint: Hosted persistence is limited to tenant-scoped
  configuration, consent state, and support diagnostics.
- Language checkpoint: New documentation and user guidance remain in UK English.
