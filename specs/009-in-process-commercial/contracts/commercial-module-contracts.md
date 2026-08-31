# Commercial Module Contracts

Boundary contracts for isolating commercial accounts in-process. Behaviour of login, create, profile, delete, restore, retention, and audit remains as specified in `specs/008-commercial-user-accounts/contracts/commercial-account-contracts.md` except where that document still places stores in Application or a separate API service (those statements are superseded here).

## 1. Project and dependency contract

| Project | May depend on | Must not depend on |
|---------|---------------|--------------------|
| Domain | (none outer) | Commercial, Graph, Web, Azure Tables, Graph SDK, MudBlazor |
| Application | Domain | Commercial, Graph, Web, Azure Tables, Graph SDK, MudBlazor, commercial account/audit/profile types |
| `ImportToPlanner.Commercial` | Application, Domain | `Infrastructure.Graph`, Microsoft.Graph, Kiota, MudBlazor, CsvHelper, ASP.NET HTTP APIs as a required commercial transport |
| `Infrastructure.Graph` | Application, Domain | Commercial account types; Azure Tables once adapters have moved |
| Web | Application, Domain, Graph, Commercial | Embedding commercial persistence policy in Razor; commercial table types in pages |
| AppHost | Web (project resource) | `ApiService.Commercial` / `commercialapiservice` |

Web maps identity/session into Commercial requests. Commercial returns structured results. Web/presenters own UK English wording.

## 2. Composition contract (`web`)

When `Features:CommercialMode:Enabled` is **true**:

1. Register the Aspire Azure Tables **client integration** with `builder.AddAzureTableServiceClient(connectionName: "tables")`. The connection name MUST match the AppHost tables resource (`AddTables("tables")` + `web.WithReference(tables)`). This registers `TableServiceClient` (health checks and tracing included). Do not `new TableServiceClient(...)` and do not read `TABLES_CONNECTIONSTRING` / `TABLES_TABLEENDPOINT` to build a second client.
2. From that DI `TableServiceClient`, register named `TableClient`s via `GetTableClient` using existing `Storage:*` table name settings (accounts, audit, tenant metadata). Aspire does not inject table names.
3. Register Commercial account/audit stores, table-backed `ITenantOperationalMetadataStore`, and commercial access/lifecycle/purge use cases that consume those clients.
4. Register the commercial retention hosted service (subject to the existing retention-sweep flag).
5. Do not start or wait for any extra commercial process.

Blobs stay on `web`: `AddAzureBlobServiceClient(connectionName: "blobs")` matching AppHost `AddBlobs("blobs")`. Data protection keeps using that DI `BlobServiceClient` to derive the `dataprotection` container/blob client (AppHost still `WithReference` / `WaitFor` the `dataprotection` container). Commercial MUST NOT register or construct a blob service client. Do not change Graph, Entra, or ServiceDefaults client construction in this feature.

When **false**:

1. Do not call `AddAzureTableServiceClient`, and do not register table clients or commercial stores/use cases.
2. Register Graph planner/CSV as today and the self-host `ITenantOperationalMetadataStore`.
3. Do not require `Storage:CommercialAccountsTable`, `Storage:CommercialAuditTable`, or `Storage:TenantMetadataTable` for startup.

Suggested extension names (implementer may refine): `AddCommercialStorageClients` on the host builder and `AddCommercial` on the service collection, called only from `Program.cs` / Web DI when mode is on.

`AddInfrastructure` must stop branching on commercial mode for table clients and commercial stores. Planner + CSV registration stays unconditional.

## 3. AppHost topology contract

| Mode | `web` | `tables` | Extra commercial process |
|------|-------|----------|---------------------------|
| Commercial enabled | Yes | Yes, `WithReference` / `WaitFor` on `web` only | Must not exist |
| Commercial disabled | Yes | Must not be added | Must not exist |

`ImportToPlanner.slnx` and `ImportToPlanner.AppHost.csproj` must not reference `ImportToPlanner.ApiService.Commercial`. `AppHost.cs` must not contain `commercialapiservice` or `ImportToPlanner_ApiService_Commercial`.

## 4. Commercial access and lifecycle (behaviour preserved)

Request/response shapes stay those in 008 sections 2–5 (identity keys, access decision enum without UI text, delete/restore/purge, audit event codes). Differences:

- Contracts live in Commercial, not Application.
- Access use cases are not invoked on the self-host path; Web does not ask Commercial for `SelfHostedBypass`.
- Failures are structured; Web maps them to human-friendly messages. No raw Table/SDK dumps.

## 5. Storage adapter contract

Unchanged table strategy from 008 section 6, with Aspire client-integration rules made explicit:

- AppHost: one `tables` resource; `WithReference` / `WaitFor` on `web` only (commercial mode). Connection info is injected as `ConnectionStrings__tables` (and related properties). Do not duplicate that with `WithEnvironment` connection strings.
- Consuming app: `AddAzureTableServiceClient("tables")` then DI `TableServiceClient`.
- Dedicated table names for accounts, audit, and tenant operational metadata remain existing configuration keys; adapters call `tableServiceClient.GetTableClient(name)`.
- Account key `TenantId` + `UserId`.
- Blobs remain data-protection only, via `AddAzureBlobServiceClient("blobs")` and DI `BlobServiceClient` on `web`.

Adapters:

- Commercial account store: get, create, mark deleted, restore, purge expired.
- Commercial audit store: append, query retention candidates, purge expired.
- Table tenant-metadata store: get/upsert `TenantOperationalMetadata` (implements Application abstraction).

## 6. Architecture evidence contract

Automated checks MUST fail if:

- Application or Domain contain `ICommercialAccountStore`, `ICommercialAuditStore`, `ICommercialAccessUseCase`, `ICommercialProfileUseCase`, `CommercialAccount`, `AccountAuditEvent`, or `CommercialAccessDecision`.
- `ImportToPlanner.Commercial` source or project references Microsoft.Graph, Kiota, MudBlazor, or `ImportToPlanner.Infrastructure.Graph`.
- Solution or AppHost still include `ApiService.Commercial` / `commercialapiservice`.
- Graph infrastructure still registers commercial table stores when those types have moved.
- Commercial or Graph runtime adapters construct `new TableServiceClient` / `new BlobServiceClient` instead of using the Aspire-registered client (test doubles outside the host builder are exempt).

Automated checks MUST pass if:

- Shared `ITenantOperationalMetadataStore` and `SessionIdentityContext` remain in Application.
- Graph still has no Microsoft.Graph types in Application/Domain (existing forbidden-token test).
