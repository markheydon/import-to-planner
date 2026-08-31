# Research: Isolate Commercial Accounts (Single Hosted Process)

## 1. Delivery shape: in-process library, not a second process

**Decision**: Delete `ImportToPlanner.ApiService.Commercial` and all `commercialapiservice` AppHost, solution, and project-reference wiring. Add `ImportToPlanner.Commercial` as a class library in the same outer layer as `ImportToPlanner.Infrastructure.Graph`. `web` registers it in-process when `Features:CommercialMode:Enabled` is true.

**Rationale**: On current `main` the API project is an empty host (`AddServiceDefaults`, map default endpoints, run). AppHost still provisions it in commercial mode, attaches `tables`, and makes `web` wait on it, so hosted commercial mode pays for a second Azure Container App that does no commercial work. Issue #99 and spec 009 reject reviving the HTTP split from #69 / #72.

**Alternatives considered**:

- Keep the empty API project “for later”: still provisions a useless container and implies a remote commercial API that does not exist.
- Cherry-pick or rewrite #72 (Blazor-to-API hops): rejected by #99; reopen a process split only if import/Graph work starves the UI, independent workers are required, or a second consumer of commercial operations appears.
- Azure Functions retention worker: still deferred; retention stays a commercial-only hosted service inside `web`.

## 2. What moves out of Application and Graph

**Decision**: Move commercial account models, store interfaces, access/lifecycle/purge use cases, Azure Table account/audit adapters, and the **table-backed** tenant-metadata adapter into `ImportToPlanner.Commercial`. Keep in Application: import/planner policy plus shared tenant/session abstractions (`ICurrentTenantContextAccessor`, `ITenantOperationalMetadataStore`, `TenantOperationalMetadata`, `SessionIdentityContext`). Keep in Graph: CSV parsing, Graph planner gateway, and the self-host tenant-metadata adapter.

**Rationale**: Spec FR-005–008 and constitution principles I–III. Commercial account types in Application force every consumer (including self-host tests) to compose commercial stores. Table adapters in Graph couple planner/CSV infrastructure to Azure Tables and commercial mode branching. Engineering policies already name `ImportToPlanner.Commercial` as the outer owner of hosted commercial-account persistence.

**Alternatives considered**:

- Move only Table adapters, leave use cases in Application: still leaks commercial account contracts into import policy (fails FR-007 and architecture tests once inverted).
- Move tenant-metadata **abstraction** into Commercial: self-host would depend on the commercial project, violating self-hosted viability.
- Duplicate tenant-metadata types: unnecessary; both adapters implement the existing Application store contract.

## 3. Registration: omit commercial persistence when commercial mode is off

**Decision**: `web` calls Commercial registration (`AddCommercialStorageClients` / `AddCommercial`) **only** when `Features:CommercialMode:Enabled` is true. When false, do not register table clients, commercial stores, or commercial use cases. Do not keep Application-level no-op commercial stores as the self-host path. Register `CommercialAccountRetentionHostedService` only when commercial mode (and the existing retention-sweep flag) is on.

**Rationale**: Spec FR-003, FR-009, FR-012. Current Graph DI registers `NoOpCommercialAccountStore` / `NoOpCommercialAuditStore` whenever commercial mode is off, so self-host still depends on commercial abstractions at runtime. Current `Program.cs` always registers the retention hosted service; it exits early, but it still couples the host to commercial types. Omitting registration matches “no commercial persistence required at startup or runtime”.

**Alternatives considered**:

- Keep no-op stores in Graph: simpler delta, but Application/Commercial contracts remain required for self-host DI and tests.
- Always reference table clients and no-op when connection strings are missing: risks self-host startup depending on Tables configuration.

## 4. AppHost topology after the split is removed

**Decision**: When commercial mode is on: one `web` project resource; `tables` referenced and waited on from `web` only. When commercial mode is off: no `tables` resource (already true on `main`); no `commercialapiservice`. Drop `minCommercialApiServiceReplicas` and the AppHost project reference to the API project.

**Rationale**: Spec User Story 1 and FR-001–004. `web` already receives `tables` in commercial mode today; the API reference is the leftover duplication from #68 / #71.

**Alternatives considered**: Leave `web.WithReference(commercialApiService)` as a no-op wait: still creates the second ACA app.

## 5. Topology and architecture evidence without AppHost tests

**Decision**: Prove topology with **static source/solution checks** in `ArchitectureComplianceTests` (and related unit tests), not `Aspire.Hosting.Testing`. Prove behaviour with existing xUnit/bUnit commercial and self-host tests moved to the Commercial boundary. Staging/hosted “one fewer container” remains an operator verification in quickstart, not an automated AppHost harness.

**Rationale**: `docs-internal/engineering-policies.md` forbids testing AppHost modelling/orchestration. Issue #99 asked for an “AppHost (or equivalent) check”; equivalent = assert `AppHost.cs`, `AppHost.csproj`, and `ImportToPlanner.slnx` do not contain `commercialapiservice` / `ApiService.Commercial`, and that commercial-mode table wiring in `AppHost.cs` references `web` only.

**Alternatives considered**:

- `Aspire.Hosting.Testing` resource graph tests: policy-forbidden.
- No topology evidence: fails spec FR-016–017 and constitution IX.

## 6. Test conventions

**Decision**: Follow **current** repository standards at implement time: xUnit v3, NSubstitute, built-in Assert, handwritten store stubs where they model real behaviour. Do not treat #72’s Moq-heavy tests as the standard. Do not migrate the whole repo’s remaining test stack in this feature.

**Rationale**: Issue #99, `docs-internal/engineering-policies.md`, spec out of scope. Existing commercial tests already use stubs rather than Moq.

**Alternatives considered**: Port #72 tests wholesale — rejected as cherry-picking the abandoned branch and as a test-stack regression.

## 7. Graph package cleanup

**Decision**: After table adapters leave Graph, remove `Azure.Data.Tables` and `Aspire.Azure.Data.Tables` from `ImportToPlanner.Infrastructure.Graph` if nothing remains. Those packages move to `ImportToPlanner.Commercial`. Graph keeps CsvHelper and Microsoft.Graph.

**Rationale**: Spec FR-008; Graph must not own commercial storage SDKs. Commercial must not reference Graph/Kiota.

**Alternatives considered**: Leave Tables packages on Graph “in case tenant metadata stays”: tenant **table** adapter is moving; self-host metadata does not need Tables.

## 8. Documentation drift for specs/008

**Decision**: Update `specs/008-commercial-user-accounts/plan.md` and `contracts/commercial-account-contracts.md` only as needed so they no longer describe Application-owned commercial stores or `commercialapiservice`. Do not reopen 008 user stories or change 008 `spec.md` behaviour.

**Rationale**: Spec FR-019. 008 plan still documents an optional API service that `main` never used for real work.

**Alternatives considered**: Leave 008 artefacts stale — causes the next implementer to reintroduce the second process.

## 9. Identity mapping stays in Web

**Decision**: Web continues to map claims/session into `SessionIdentityContext` and to own UK English presentation. Commercial use cases return structured decisions and account records without user-facing prose. `SelfHostedBypass` as an Application decision is unnecessary once Web skips Commercial registration when mode is off; commercial access use cases run only in commercial mode.

**Rationale**: Constitution III and spec FR-010. Removes a commercial-mode flag from inner commercial policy’s “self-host” branch.

**Alternatives considered**: Keep `CommercialModeEnabled` on the access request so one use case handles both modes — fights the isolation goal.

## 10. Existing data and behaviour

**Decision**: Keep the 008 table names, account key (Tenant Id + User Id), 6-month deletion retention, 12-month audit retention, and access/profile/delete/restore semantics. No schema rebuild and no account recreation.

**Rationale**: Spec FR-011, FR-018, SC-004–005. Isolation is a module boundary change, not a product change.

## 11. Aspire-owned Azure storage clients (Tables and Blobs)

**Decision**: AppHost remains the source of storage connectivity. Consuming app code uses Aspire **client integrations** and DI — it must not construct its own `TableServiceClient` or `BlobServiceClient` from connection strings or endpoints in production/runtime paths.

Concrete rules:

1. **AppHost**: keep `AddAzureStorage("storage")`, `AddBlobs("blobs")`, `AddBlobContainer("dataprotection", ...)`, and (commercial mode only) `AddTables("tables")`. Wire with `web.WithReference(blobs)` / `WaitFor(blobs)` always, and `web.WithReference(tables)` / `WaitFor(tables)` only when commercial mode is on. Do **not** pass storage connection strings through `WithEnvironment` when `WithReference` already injects `ConnectionStrings__<resourceName>`.
2. **Tables (commercial)**: when commercial mode is on, call `builder.AddAzureTableServiceClient(connectionName: "tables")` (name must match the AppHost tables resource). Package: `Aspire.Azure.Data.Tables` on the project that registers the client (`ImportToPlanner.Commercial` if it exposes `AddCommercialStorageClients`). Adapters resolve `TableServiceClient` from DI, then `GetTableClient(configuredTableName)` for accounts, audit, and tenant metadata. Table names stay in app configuration (`Storage:*`); Aspire does not inject table names.
3. **Blobs (unchanged, must not regress)**: `web` keeps `builder.AddAzureBlobServiceClient(connectionName: "blobs")` and data-protection continues to resolve `BlobServiceClient` from DI. Commercial must not add a second blob client or `new BlobServiceClient(...)`.
4. **Forbidden in app/adapter code**: `new TableServiceClient(...)`, `new BlobServiceClient(...)`, reading `TABLES_CONNECTIONSTRING` / `BLOBS_URI` to hand-build clients, or registering a replacement service client that bypasses the Aspire integration (which also supplies health checks and Table/Blob telemetry).
5. **Tests**: isolated unit tests may construct SDK clients or stubs without the Aspire host (for example `new TableServiceClient("UseDevelopmentStorage=true")` in registration tests that do not call `AddAzureTableServiceClient`). Runtime composition tests that exercise the host builder MUST go through the Aspire client integration, not a hand-built client.

**Rationale**: Aspire 13.4 / aspireify guidance: `WithReference` is how resources inject connection info; C# apps should use the client integration so DI, health checks, and OpenTelemetry are registered once. Official connect docs: [Azure Table Storage](https://aspire.dev/integrations/cloud/azure/azure-storage-tables/azure-storage-tables-connect/) and [Azure Blob Storage](https://aspire.dev/integrations/cloud/azure/azure-storage-blobs/azure-storage-blobs-connect/). Current `main` already follows this for blobs (`AddWebStorageClients`) and tables (`AddInfrastructureStorageClients`); the move to Commercial must preserve it, not invent a second client factory.

**AppHost resource → client map (current `main`, including resources this feature does not change):**

| AppHost resource | Kind | Consuming client on `web` | Same DI rule? |
|------------------|------|---------------------------|---------------|
| `storage` | Azure Storage account | Parent only; no app-level `BlobServiceClient`/`TableServiceClient` from the account resource itself | N/A — consume child `blobs` / `tables` |
| `blobs` | Blob **service** | `AddAzureBlobServiceClient("blobs")` → DI `BlobServiceClient` | Yes — already stated. Data protection and any future blob use derive container/blob clients from this instance. |
| `dataprotection` | Blob **container** (`AddBlobContainer`, `WithReference` + `WaitFor` on `web`) | No separate Aspire container client today. `HostedDataProtectionConfigurator` takes DI `BlobServiceClient` then `GetBlobContainerClient` / `GetBlobClient` using `Storage:DataProtectionContainer` (must stay aligned with AppHost `blobContainerName: "dataprotection"`). | Yes — do **not** `new BlobServiceClient` / `new BlobClient` from a connection string. Do **not** drop the AppHost container reference. Switching to `AddAzureBlobContainerClient("dataprotection")` is optional and **out of scope** for 009. |
| `tables` | Table **service** (commercial mode only) | `AddAzureTableServiceClient("tables")` → DI `TableServiceClient` → `GetTableClient` | Yes — this feature’s move. |
| `web` | Project | User-facing host | N/A |
| `commercialapiservice` | Project | Unused; deleted by this feature | Do not add a client for it |
| `aca-env` | ACA environment | Publish/hosting only | N/A — not a data client |
| Parameters (`azureAd*`, `enableCommercialMode`, certs, optional custom domain) | `AddParameter` + `WithEnvironment` | Configuration / secrets, not SDK clients | Correct as parameters; do not pretend they are `WithReference` storage |
| Service defaults (OTLP, Azure Monitor, health, `HttpClient` + service discovery) | Injected by Aspire/`AddServiceDefaults` | `OTEL_EXPORTER_OTLP_ENDPOINT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, standard `IHttpClientFactory` | Keep using ServiceDefaults; do not stand up a parallel OpenTelemetry pipeline or raw `HttpClient` for Aspire service-to-service calls. There is no second project to discover after `commercialapiservice` is removed. |

**Not AppHost resources (do not force Aspire client integrations):**

- `GraphServiceClient` — Microsoft Graph via Microsoft.Identity.Web in Web; keep constructing through that auth path, not as an Aspire resource.
- Entra / Azure AD — parameters and MSAL, not `AddAzure*` data clients.

**Deferred / explicitly not in AppHost today (same rule if ever added):** Redis, SQL, queues, Key Vault, Azure Functions (008 retention worker). If introduced later: AppHost resource + `WithReference` + matching Aspire client integration; never hand-built SDK clients on runtime paths.

**Alternatives considered**:

- Read `TABLES_CONNECTIONSTRING` / `TABLES_TABLEENDPOINT` and `new TableServiceClient` in Commercial: works from any language but drops Aspire health checks, tracing, and credential handling that the client integration already configures for C#.
- Register Tables from Web `Program.cs` and only inject `TableServiceClient` into Commercial: also valid; if chosen, Web takes the `Aspire.Azure.Data.Tables` package and Commercial only consumes DI. Prefer keeping registration next to the adapters (`AddCommercialStorageClients`) so Graph no longer owns Tables packages — same shape as today’s Graph helper, moved.
