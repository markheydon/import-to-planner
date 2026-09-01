# Quickstart: Isolate Commercial Accounts (Single Hosted Process)

Validate this feature without a production deploy. Behaviour journeys match the commercial user-accounts feature; this guide proves topology, isolation, and registration.

## Prerequisites

- .NET SDK from `global.json`
- Restore at the solution root: `dotnet restore ImportToPlanner.slnx`

## 1. Solution and topology (static)

Confirm the unused extra process is gone:

- `ImportToPlanner.slnx` has `ImportToPlanner.Commercial` and does not list `ImportToPlanner.ApiService.Commercial`.
- `src/ImportToPlanner.AppHost/ImportToPlanner.AppHost.csproj` references Web only (plus AppHost packages), not the deleted API project.
- `src/ImportToPlanner.AppHost/AppHost.cs` has no `commercialapiservice`; commercial-mode `tables` are referenced from `web` only.

Architecture tests encode the same rules (see contracts).

## 2. Automated tests

From the repository root, after restore:

```bash
dotnet test ImportToPlanner.slnx --no-restore
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

Expect:

- Commercial access, lifecycle, retention filter/purge, and table-store tests still pass against Commercial (in-memory stubs or table test doubles at the Commercial boundary).
- Self-host registration tests: no `TableServiceClient`, no commercial stores required, self-host tenant metadata still resolved.
- Commercial-mode registration tests: host builder with `AddAzureTableServiceClient("tables")` (or `AddCommercialStorageClients`) yields a DI `TableServiceClient`; Commercial stores/use cases come from Web/Commercial composition, not Graph. Tests that construct `new TableServiceClient("UseDevelopmentStorage=true")` are only for isolated adapter tests that skip the Aspire client integration.
- Architecture tests fail if commercial account types return to Application/Domain, if Commercial references Graph/Kiota, or if `commercialapiservice` reappears.
- Existing Web bUnit commercial gate / retention tests still pass with Commercial test doubles.

Do not add `Aspire.Hosting.Testing` AppHost harnesses.

## 3. Local commercial vs self-host composition

Self-host (`Features:CommercialMode:Enabled` = false):

- App starts without a `tables` connection string.
- Commercial login gate does not appear; automatic Microsoft 365 sign-in remains.

Commercial mode true:

- `tables` connection is required (Azurite via AppHost locally).
- First sign-in, returning user, profile, delete/restore, and audit behave as on current `main`.
- Existing table rows remain usable; no account recreation.

## 4. Operator check (hosted/staging, optional)

With commercial mode on, the published app graph has one user-facing container (`web`) and no second commercial container, compared with today’s stub topology. Self-host publishes still omit `tables`.

## 5. Docs drift

If 008 plan/contracts still mention Application-owned commercial stores or `commercialapiservice`, they are stale relative to this feature. Align those two artefacts only; do not reopen 008 user stories.
