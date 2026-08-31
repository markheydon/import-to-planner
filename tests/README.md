# Testing & Coverage

Quick reference for running tests and collecting coverage for Import To Planner.

Where tests live
----------------
- Application and infrastructure unit tests: `tests/ImportToPlanner.Tests/` (xUnit v3).
- Blazor component unit tests: `tests/ImportToPlanner.Web.Tests/` (bUnit + xUnit v3).

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

Repository testing standards
----------------------------
- Unit tests use **xUnit v3**, **NSubstitute** for interface doubles, and built-in
  `Assert` methods only.
- Blazor UI tests use **bUnit** as component unit tests; they are not end-to-end tests.
- **AppHost modelling and orchestration are not tested** in this repository.
- **Playwright** is the approved end-to-end tool when complete user journeys need
  coverage. This repository does not currently include a Playwright suite.
- Do not introduce FluentAssertions, AwesomeAssertions, Shouldly, Moq, NUnit, or MSTest.
- Test projects inherit `TreatWarningsAsErrors`; new test code must compile without
  warnings.

Repository testing notes
------------------------
- Use NSubstitute or explicit boundary doubles for planner and tenant metadata
  abstractions.
- Keep handwritten stateful doubles when they model real behaviour (for example,
  in-memory stores or adapter subclasses).
- See `docs-internal/engineering-policies.md` for mandatory testing standards and
  architecture evidence gates. The constitution states the stack-independent
  testability and quality rules; this repository's named checks and packages
  live in engineering policies.

Guidance and skills
-------------------
- Refer to the `csharp-xunit` and `dotnet-best-practices-repo` skills for test patterns
  and repository-aligned practices.
