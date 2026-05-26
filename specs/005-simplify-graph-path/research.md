# Research: Simplify Graph Runtime Path

## Decision 1: Replace the root file-based Aspire script with a conventional AppHost project

- Decision: Create `ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj`
  referencing `Aspire.AppHost.Sdk`, add a `Program.cs`, add the project to
  `ImportToPlanner.slnx`, and delete the root `apphost.cs` and
  `aspire.config.json`.
- Rationale: The current root script is an experimental outlier that reads
  environment variables and composes resources conditionally. A conventional
  AppHost project matches the repository's normal project layout, supports Aspire
  CLI workflows cleanly, and removes the need for a custom file-based host.
- Alternatives considered: Keep the root script and simplify it in place.
  Rejected because it preserves an unusual entrypoint and does not resolve the
  structural inconsistency or the desire to delete `aspire.config.json`.

## Decision 2: Declare the Aspire resource graph unconditionally

- Decision: The AppHost will always declare `builder.AddAzureStorage("storage")
  .RunAsEmulator()`, then `.AddBlobs("blobs")` and `.AddTables("tables")`, and
  wire `builder.AddProject<Projects.ImportToPlanner_Web>("web")` with both
  references. No `if` statements, environment-variable reads, or conditional
  `WithEnvironment` calls remain in the AppHost.
- Rationale: The feature's target developer experience depends on storage always
  existing locally and in deployment. Aspire already abstracts emulator versus
  deployed resource wiring, so conditional resource composition adds complexity
  without value.
- Alternatives considered: Keep conditional storage creation behind
  `HostedStorage:Enabled`. Rejected because it preserves a hosted-only concept
  that the spec explicitly removes.

## Decision 3: Keep AzureAd configuration entirely in the Web project

- Decision: `AzureAd:TenantId`, `AzureAd:ClientId`, and certificate settings stay
  in Web project configuration and user secrets; the AppHost does not read or
  forward them.
- Rationale: Microsoft.Identity.Web already expects AzureAd settings in the web
  app's own configuration. The current AppHost and `Program.cs` both synthesise
  authority settings, which is the coupling this feature is meant to remove.
- Alternatives considered: Continue forwarding `AzureAd:TenantId` from the
  AppHost. Rejected because it keeps authority resolution split across hosting and
  Web startup.

## Decision 4: Derive auth behaviour from `AzureAd:TenantId` instead of an Application-layer mode record

- Decision: Treat `AzureAd:TenantId == "organizations"` as the retained
  multi-tenant auth-guard path, and treat any other supported tenant value as the
  single-tenant path. This classification is owned by the Web layer.
- Rationale: The library behaviour already depends on tenant authority. Encoding
  the same decision as an Application enum adds a second, leaky source of truth
  and violates the constitution's dependency and neutrality rules.
- Alternatives considered: Keep `DeploymentModeConfiguration` but generate it in
  Web only. Rejected because the inner-layer contract would still represent
  deployment topology.

## Decision 5: Remove in-memory production implementations and replace test usage with boundary doubles

- Decision: Delete `InMemoryPlannerGateway` and
  `InMemoryTenantOperationalMetadataStore` from production code. Register
  `GraphPlannerGateway` and `TableTenantOperationalMetadataStore`
  unconditionally in Infrastructure. Update tests to use explicit doubles at the
  `IPlannerGateway` and `ITenantOperationalMetadataStore` boundaries, or delete
  tests that only exercise removed runtime paths.
- Rationale: The in-memory implementations no longer represent a supported
  runtime. Keeping them in production DI prolongs the obsolete matrix and forces
  configuration branching throughout the codebase.
- Alternatives considered: Keep in-memory implementations for non-production
  runtime selection. Rejected because the spec requires a single supported code
  path with no runtime mode-switching.

## Decision 6: Always configure Data Protection key persistence to blob storage

- Decision: Simplify `HostedDataProtectionConfigurator` so it always configures
  blob-backed Data Protection persistence using Azure Storage connection settings
  injected through Aspire references. Rename configuration from `HostedStorage:*`
  to `Storage:*`, keeping only the container and blob names configurable in the
  Web project.
- Rationale: Storage is no longer a hosted-only concern, and the Web app should
  always have blob storage available through the AppHost. Conditional data
  protection persistence adds no value once storage is unconditional.
- Alternatives considered: Keep the `HostedStorageEnabled` and deployment-mode
  guards. Rejected because they encode the deleted hosted-only branch.

## Decision 7: Validate startup configuration early and fail gracefully

- Decision: Add explicit startup validation for two failure classes: missing or
  blank `AzureAd:TenantId`, and the continued presence of removed
  `PlannerGateway:*`, `HostedStorage:*`, or `DeploymentMode:*` keys. These
  failures stop startup before sign-in is available and surface a human-friendly,
  operator-readable configuration error rather than a raw exception page.
- Rationale: The spec's clarifications require fail-fast behaviour, and the
  constitution requires public-facing failures to be graceful and actionable.
- Alternatives considered: Ignore removed keys or rely on raw `Options`
  exceptions. Rejected because there is no supported upgrade path and raw
  framework errors do not meet the UX requirement.

## Decision 8: Move consent and telemetry dependencies off deployment-mode concepts

- Decision: Remove `DeploymentModeConfiguration` from Application and Web helpers,
  replacing it with smaller Web-owned authority/consent services or options that
  expose only what each consumer actually needs.
- Rationale: Current consumers use the mode record for mixed concerns:
  authority-tenant classification, Graph-vs-in-memory selection, hosted storage
  flags, telemetry labels, and admin consent URI construction. Splitting those
  responsibilities reduces leakage and makes each slice testable.
- Alternatives considered: Keep the aggregate record but rename it. Rejected
  because it would continue to carry deployment topology into inner layers.

## Repository Policy Checkpoints

- Smallest-practical automated tests still come first, but runtime-mode parity
  for `PlannerGateway:UseGraph` is replaced by explicit evidence that the runtime
  mode matrix no longer exists.
- User-facing configuration and startup failures must remain human-friendly and in
  UK English.
- Secrets remain outside source control and are configured through Web project
  user secrets rather than AppHost environment forwarding.
- AppHost scope stays minimal: one web resource and one Azure Storage resource
  graph, with no Key Vault integration added in this feature.
