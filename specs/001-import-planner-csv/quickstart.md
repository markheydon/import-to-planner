# Quickstart: CSV To Planner Import Workflow

## Feature Quality Gates And Traceability

Use this checklist for every feature validation run:

1. Validation gate: malformed CSV rows produce row-level errors and execution remains blocked.
2. Preview gate: preview is dry-run only and never writes to Planner.
3. Matching gate: existing tasks are matched by task name only and shown as `already exists`.
4. Execution gate: explicit confirmation is required and stale preview is blocked.
5. Reporting gate: created, reused/skipped, errors, and manual actions are all present in the final report.
6. Safety gate: user-facing errors are sanitized and do not expose tenant-sensitive values.
7. Runtime parity gate: in-memory and Graph pathways preserve equivalent feature outcomes for preview/execution behaviour.

Traceability mapping:

- FR-001 to FR-004: parser validation and dry-run preview tests.
- FR-005 to FR-008c: orchestrator execution gating, stale-preview, continuation, and retry tests.
- FR-009 to FR-011: manual action/reporting and runtime parity tests.
- NFR-001 to NFR-007: architecture separation, automated tests, responsiveness checks, and CI/AppHost parity evidence below.

## Prerequisites
- .NET 10 SDK installed
- Repository restored successfully
- For Graph mode: tenant/app registration and user secrets configured

## 1. Restore, build, and test
```bash
dotnet restore ImportToPlanner.slnx
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
```

Expected:
- All tests pass.

## 2. Run in in-memory mode (default)
```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Expected:
- Home page loads import workflow UI.
- No tenant credentials required.

## 3. Run in Graph mode (optional)
Configure required user secrets first, then:
```bash
dotnet user-secrets set "PlannerGateway:UseGraph" "true" --project src/ImportToPlanner.Web
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

Expected:
- Auth challenge if not signed in.
- Container/plan list available for current user context.

## 4. Validate clarified behaviour
Use a CSV with mixed valid and invalid rows, plus duplicate task names in the same target plan.

Checkpoints:
1. Validation errors appear for malformed rows and block execution.
2. Preview shows planned actions without writes.
3. Existing task name matches are skipped and reported as `already exists`.
4. Execution continues on independent row failures and reports partial outcomes.
5. Transient row failures retry once then fail if retry also fails.
6. If planner state changes after preview, execution is blocked and requires fresh preview.

Implementation notes:

- Preview metadata includes request and planner-state fingerprints to enforce stale-preview blocking.
- Execution report includes aggregate counters for created/reused/errors/manual actions with partial-success status.
- User-facing Graph failure messages are normalized via user-safe mapping.
- `ImportExecutionOutcomeSummary` tracks `IsPartialSuccess` (some succeed, some fail) and `IsFullFailure` (all fail) independently.
- Retry-once policy applies at the row level: transient failures (HTTP 503) are retried once; on second failure the row is reported as failed and execution continues on remaining rows.
- Task name matching is exact case-insensitive string equality only; no fuzzy matching is applied.

## 5. AppHost/CI parity checks
```bash
dotnet restore apphost.cs
dotnet build apphost.cs --no-restore
```

Expected:
- Solution and AppHost paths both build successfully.

## 6. Preview Performance Protocol (500 rows, p95 < 10s)

Protocol:

1. Set the opt-in environment variable that enables the performance test (it is skipped by default in CI to avoid flaky timing failures):
```bash
export IMPORT_TO_PLANNER_RUN_PERF_TESTS=true
```
2. Run the focused performance test 20 times in a single execution:
```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj \
	--filter "FullyQualifiedName~BuildPreviewAsync_WithFiveHundredRows_MeetsP95UnderTenSeconds" \
	--logger "console;verbosity=detailed"
```
3. Record the printed `Preview p95 for 500 rows` value.
4. Validate p95 is below 10,000 ms.

> **Note**: Without `IMPORT_TO_PLANNER_RUN_PERF_TESTS=true` the test is skipped. This
> variable must be set in any environment (local or CI) where performance validation is
> required. Do not enable it in standard CI runs unless the agent has dedicated compute
> capacity that can reliably meet the 10-second threshold.

Expected: p95 is below 10,000 ms (10 seconds).

## 7. Accessibility And Responsive Validation

Validate in in-memory mode (`PlannerGateway__UseGraph=false`):

1. Desktop (`http://localhost:5126`): import form controls expose visible labels and disabled/enabled states for CTA buttons.
2. Mobile viewport (390x844): layout is functional with all primary controls reachable and visible.
3. Status messaging: preview reset and stale-preview warning states render as Fluent message bars.

Validation outcome recorded:

- Fluent UI components expose semantic HTML labels, ARIA attributes, and focus management by default through the component library.
- CTA buttons (`Validate And Preview`, `Confirm And Execute`) render as `<fluent-button>` with `disabled` attribute propagated correctly on state transitions.
- Status messages use `<FluentMessageBar>` which maps to the ARIA `role="status"` pattern.
- Responsive layout relies on Fluent UI stack/grid defaults; no horizontal scroll introduced at 390 px viewport.

## 8. Single-Tenant Scope Verification

Verify that:

- No multi-tenant feature paths are implemented.
- Single-tenant Entra/Graph configuration remains unchanged.
- Graph/Kiota boundary remains infrastructure-local, with application logic depending on `Microsoft.Graph`-aligned abstractions only.

Verification outcome recorded:

- `Program.cs` registers `GraphPlannerGateway` only; no multi-tenant middleware or tenant-resolution middleware added.
- `AzureAd` configuration section maps to a single `TenantId`; `AllowWebApiToBeAuthorizedByACL` is not set.
- All Graph calls go through `IPlannerGateway`; no `GraphServiceClient` usage outside `ImportToPlanner.Infrastructure.Graph`.
- No claims transformation or external tenant lookup logic added in this feature.

## 9. Optional Aspire run
```bash
aspire start --isolated
aspire describe
aspire logs web
aspire stop
```
