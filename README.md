# Import To Planner

Import To Planner is a single-purpose Blazor web app for importing tasks from CSV into Microsoft Planner. It is designed for safe, operator-driven bulk imports with explicit validation, dry-run preview, confirmation before writes, and execution reporting.

The repository keeps the core import behaviour consistent across two runtime modes:

- In-memory mode for local development and test-friendly runs without tenant credentials.
- Microsoft Graph mode for live Planner execution against a single tenant.

## Key Features

- CSV parsing with row-level validation errors.
- Dry-run preview before any Planner changes are made.
- Explicit confirmation step before execution.
- Existing-task matching by task name, reported as `already exists`.
- Stale-preview protection using request and planner-state fingerprints.
- Partial-success execution reporting with retry-once handling for transient Graph row failures.
- Manual follow-up actions for Planner goal-related work that cannot be automated fully.
- Behavioural parity across in-memory and Graph-backed gateway implementations.

## Technology Stack

- .NET SDK: 10.0.100.
- Language: C#.
- UI: ASP.NET Core Blazor with Microsoft Fluent UI for Blazor.
- CSV parsing: CsvHelper 33.1.0.
- Graph integration: Microsoft.Graph 5.105.0.
- Authentication: Microsoft.Identity.Web 4.9.0.
- Testing: xUnit 2.9.3 and bUnit 2.7.2.
- Observability and service defaults: OpenTelemetry and .NET Aspire service defaults.

## Architecture

The solution follows a layered clean architecture:

- `ImportToPlanner.Domain`: domain models and business rules.
- `ImportToPlanner.Application`: import parsing abstractions, orchestration, and workflow logic.
- `ImportToPlanner.Infrastructure.Graph`: Graph-backed and in-memory planner gateway implementations.
- `ImportToPlanner.Web`: Blazor UI and application shell.
- `ImportToPlanner.ServiceDefaults`: shared operational defaults for hosting and telemetry. The `apphost.cs` file defines a minimal AppHost for local orchestration and future hosted deployment planning.

The application does not persist imported data locally. Microsoft Planner is the system of record, and import state remains transient.

## Getting Started

### Prerequisites

- .NET 10 SDK.
- Optional: a Microsoft 365 tenant and Entra app registration if you need to test Graph mode.
- Optional: Aspire CLI if you want to use the local AppHost workflow.

### Restore, Build, and Test

```bash
dotnet restore ImportToPlanner.slnx
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
```

### Run Locally in In-Memory Mode

In-memory mode is the default and does not require tenant credentials.

```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Expected outcome:

- The home page loads the import workflow UI.
- No sign-in challenge is required.

### Run Locally in Graph Mode

Configure local secrets first, then enable Graph mode:

```bash
dotnet user-secrets set "PlannerGateway:UseGraph" "true" --project src/ImportToPlanner.Web
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Expected outcome:

- Unauthenticated users are redirected to sign in.
- Planner containers and plans are loaded from Microsoft Graph.

For tenant setup and Graph-specific guidance, see [docs-internal/microsoft-graph-guidelines.md](docs-internal/microsoft-graph-guidelines.md) and [docs-internal/README.md](docs-internal/README.md).

### Optional Aspire Workflow

The repository includes a minimal AppHost for local orchestration and hosted deployment planning.

```bash
aspire start --isolated
aspire describe
aspire logs web
aspire stop
```

CI also validates the AppHost build path:

```bash
dotnet restore apphost.cs
dotnet build apphost.cs --no-restore
```

CI also verifies formatting on the solution before build and test, so running the same `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` command locally is a good pre-PR check.

Hosted deployment implementation is not included yet. The current internal documentation records a production-readiness baseline and configuration handoff expectations for future hosted rollout.

### Hosted Aspire Production-Readiness Summary

- Keep `apphost.cs` scoped to the single `web` resource unless explicitly approved scaling requirements are introduced.
- Track hosted handoff for `PlannerGateway:UseGraph`, `AzureAd:*`, certificate credential source, and `OTEL_EXPORTER_OTLP_ENDPOINT`.
- Keep `/health` and `/alive` disabled in non-development environments unless private or authenticated exposure is explicitly implemented.
- Keep CI parity for hosted-readiness planning with solution build/tests and AppHost restore/build.

For the full checklist and environment-specific matrix, see [docs-internal/aspire-production-readiness.md](docs-internal/aspire-production-readiness.md).

## Project Structure

```text
src/
  ImportToPlanner.Application/
  ImportToPlanner.Domain/
  ImportToPlanner.Infrastructure.Graph/
  ImportToPlanner.ServiceDefaults/
  ImportToPlanner.Web/
tests/
  ImportToPlanner.Tests/
  ImportToPlanner.Web.Tests/
docs/
docs-internal/
specs/
apphost.cs
```

- `src/` contains the production code, split by architectural layer.
- `tests/` contains unit, integration-style, and Blazor UI tests.
- `docs/` is reserved for public-facing documentation.
- `docs-internal/` contains contributor and operational guidance, organised by topic.
- `specs/` contains Spec Kit artefacts, including the feature spec, plan, tasks, contracts, and quickstart for the CSV import workflow.

## Development Workflow

- Start from the in-memory mode for most local work.
- Keep behaviour aligned across `PlannerGateway:UseGraph = false` and `true` when planner behaviour changes.
- Preserve clean architecture boundaries: business logic belongs in Domain and Application, UI logic belongs in Web, and Graph details stay in Infrastructure.
- Treat dry-run preview, stale-preview blocking, and user-safe error handling as core safety requirements.
- Keep the AppHost build path healthy alongside the main solution build.

The feature design artefacts for the current workflow live under [specs/001-import-planner-csv/](specs/001-import-planner-csv/).

## Coding Standards

- Use UK English in user-facing copy, contributor documentation, and comments intended for users.
- Follow the repository's clean architecture rules and keep dependencies pointing inward.
- Keep Microsoft Graph implementation details inside infrastructure code.
- Prefer automated tests at the smallest practical level for behaviour changes.
- Do not commit secrets, certificates, tenant identifiers, or other sensitive configuration values.

Repository-specific guidance lives in [.github/copilot-instructions.md](.github/copilot-instructions.md), [AGENTS.md](AGENTS.md), and [.specify/memory/constitution.md](.specify/memory/constitution.md).

## Testing

Primary test projects:

- `tests/ImportToPlanner.Tests/` for unit and integration-style tests.
- `tests/ImportToPlanner.Web.Tests/` for Blazor UI and workflow tests.

Run the full test suite with:

```bash
dotnet test ImportToPlanner.slnx
```

Optional local coverage collection:

```bash
dotnet tool install -g dotnet-coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test ImportToPlanner.slnx
```

When planner gateway behaviour changes, the repository constitution requires verification for both runtime modes unless the change is explicitly scoped to one mode and documented as such. More detail is available in [tests/README.md](tests/README.md).

## Contributing

Contributions are welcome, but the repository is intentionally narrow in scope and changes are reviewed against that scope.

- Start with [CONTRIBUTING.md](CONTRIBUTING.md) for contribution, pull request, and review-thread guidance.
- Open focused pull requests targeting `main`.
- Keep CI green before requesting review.
- Update this README or related contributor docs when local setup or workflow expectations change.

If you are new to the codebase, the recommended first run is:

```bash
dotnet restore ImportToPlanner.slnx
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

GitHub Codespaces is supported through [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json).

## Further Reading

- [specs/001-import-planner-csv/spec.md](specs/001-import-planner-csv/spec.md)
- [specs/001-import-planner-csv/plan.md](specs/001-import-planner-csv/plan.md)
- [specs/001-import-planner-csv/quickstart.md](specs/001-import-planner-csv/quickstart.md)
- [specs/001-import-planner-csv/data-model.md](specs/001-import-planner-csv/data-model.md)
- [specs/001-import-planner-csv/contracts/import-workflow-contract.md](specs/001-import-planner-csv/contracts/import-workflow-contract.md)
- [docs-internal/README.md](docs-internal/README.md)
- [docs-internal/aspire-production-readiness.md](docs-internal/aspire-production-readiness.md)
- [docs-internal/ci-notes.md](docs-internal/ci-notes.md)
- [docs-internal/roadmap-and-limitations.md](docs-internal/roadmap-and-limitations.md)

## Licence

This project is licensed under the [MIT Licence](LICENSE).
