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
- Use explicit boundary doubles for planner and tenant metadata abstractions in integration-style tests.
- See `.specify/memory/constitution.md` for mandatory testing standards and architecture evidence expectations.

Guidance and skills
-------------------
- Refer to the `csharp-xunit` and `dotnet-best-practices-repo` skills for test patterns and repository-aligned practices.
