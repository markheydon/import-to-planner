# Testing & Coverage

Quick reference for running tests and collecting coverage for Import To Planner.

Where tests live
----------------
- Unit and integration tests: `tests/ImportToPlanner.Tests/` (xUnit).

Run tests
---------
Run the solution tests:

```bash
dotnet test ImportToPlanner.slnx
```

Coverage (optional)
-------------------
Use `dotnet-coverage` to collect coverage reports locally:

```bash
dotnet tool install -g dotnet-coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test ImportToPlanner.slnx
```

Repository testing notes
------------------------
- Mock `IPlannerGateway` for unit tests covering orchestration logic.
- Prefer `InMemoryPlannerGateway` for integration-style tests that exercise planner behaviour without calling real Graph endpoints.
- See `.specify/memory/constitution.md` for mandatory testing standards (for example: verify compatibility for both configured runtime modes `PlannerGateway:UseGraph` true and false when planner gateway behaviour is affected).

Guidance and skills
-------------------
- Refer to the `csharp-xunit` and `dotnet-best-practices-repo` skills for test patterns and repository-aligned practices.
