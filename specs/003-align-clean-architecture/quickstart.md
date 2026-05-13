# Quickstart: Clean Architecture Alignment Verification

## Purpose

Use this guide after implementation to verify that the refactor preserved workflow behaviour
while restoring architectural boundary ownership.

## Prerequisites

- .NET 10 SDK installed.
- Repository dependencies restored.
- Test environment available for both planner runtime modes:
  - `PlannerGateway:UseGraph=false`
  - `PlannerGateway:UseGraph=true` for any changed planner-facing behaviour slice

## Recommended Verification Order

### 1. Run focused automated tests first

```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj
```

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj
```

Expected evidence:
- Planning-use-case tests cover neutral result shaping, preview fingerprint rules, and validation behaviour.
- Execution-use-case tests cover created/reused/failed/manual-action outcomes without UI prose.
- Parser tests continue to pass after the implementation moves to Infrastructure.
- Web tests cover step progression, stale-preview invalidation, confirmation gating, and presenter-driven wording.

### 2. Verify architecture compliance evidence

Examples of expected checks:

```bash
rg -n "Graph|Kiota|MudBlazor|CsvHelper|ToUserSafe" src/ImportToPlanner.Application src/ImportToPlanner.Domain
```

```bash
rg -n "ContainerUrl|RawContainerType" src/ImportToPlanner.Domain
```

Expected evidence:
- No provider, framework, or UI-specific residue remains in Domain/Application except where intentionally documented at the boundary contract level.
- Retired fields and Graph-branded exception concepts are absent from inner layers.

### 3. Verify runtime-mode parity where planner behaviour changed

Run the affected planner-facing test slice in both modes or execute equivalent targeted checks with configuration varied per mode.

Expected evidence:
- For the same approved import scenario, both modes produce equivalent presenter-visible outcomes.
- Provider-specific translation remains confined to adapters.

### 4. Perform manual workflow verification

Check the main import page behaviour end to end:

1. Select a container.
2. Select or define a target plan.
3. Upload a CSV file and review validation feedback.
4. Build preview.
5. Confirm import.
6. Execute import.
7. Revisit an earlier step and confirm stale preview/execution gating still works.

Expected evidence:
- The same workflow safeguards remain in place.
- End-user wording remains in UK English.
- `Home.razor` acts as a composition surface rather than the owner of business or provider logic.

## Review Checklist

- Dependency direction still reads `Web/Infrastructure -> Application -> Domain`.
- Planning and execution use cases have explicit request, response, and output-boundary seams.
- Presenters own UI text.
- Domain entities carry only business concepts.
- Application outcomes are structured and neutral.
- Both runtime modes are verified when planner-facing behaviour changes.

## Verification Evidence (2026-05-13)

### Commands run

```bash
dotnet build ImportToPlanner.slnx
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj
```

### Outcomes

- `dotnet build ImportToPlanner.slnx`: passed.
- `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj`: passed (`33` passed, `0` failed).
- `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj`: passed (`8` passed, `0` failed).

### Runtime-mode parity evidence

- Runtime-mode parity hooks are parameterised in `HomePageTestContext(bool useGraphGateway = false)` and can be exercised in either mode.
- Planning/execution parity coverage for planner behaviour is exercised in `ImportExecutionUseCaseTests.HandleAsync_RuntimeModeParity_InMemoryAndFakeGatewayReturnEquivalentOutcomeCounts`.

### Architecture evidence

- Forbidden-reference regression check added in `ArchitectureComplianceTests.DomainAndApplication_DoNotReferenceProviderOrUiPackages`.
- Domain residue check added in `PlannerPlanTests.PlannerPlan_ContainsOnlyNeutralDomainProperties`.
