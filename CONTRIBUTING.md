# Contributing to Import To Planner

Thank you for your interest in contributing! This is a solo-maintained project, so contributions are welcome but will be reviewed carefully to ensure they fit the project's goals and standards.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A **Microsoft 365 account** with access to Microsoft Planner and the Microsoft Graph API (required to test real Graph functionality)
- An **Entra ID app registration** with the appropriate delegated permissions if you are working on auth or Graph integration

### Building and running tests

```bash
dotnet restore
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
```

To run the web app locally:

```bash
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

---

## Reporting Bugs

Please use [GitHub Issues](https://github.com/markheydon/import-to-planner/issues/new/choose) and select the **Bug Report** template. Include as much detail as possible:

- Steps to reproduce
- Expected vs actual behaviour
- .NET SDK version and operating system
- Any relevant CSV input (redact any sensitive data)

---

## Suggesting Features

Open a [GitHub Issue](https://github.com/markheydon/import-to-planner/issues/new/choose) using the **Feature Request** template. Describe the use case clearly — feature requests are evaluated against the project's single-use utility scope.

---

## Pull Request Process

1. **Fork** the repository and create your branch from `main`.
2. **Target `main`** — all PRs must target the `main` branch.
3. **Linear history only** — squash or rebase your commits before opening a PR. Merge commits are not permitted (enforced by the branch ruleset).
4. **Keep PRs focused and small** — one logical change per PR makes review faster and safer.
5. **All CI checks must pass** — the `Build and Test` workflow must be green before a PR can merge.
6. **Copilot code review** — an automated Copilot review will be requested on every PR. Address any findings before requesting a human review.
7. Open the PR and fill in the description clearly explaining what the change does and why.

---

## Coding Standards

### C# conventions

- Follow standard [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Use modern C# language features supported by .NET 10.
- Prefer `async`/`await` end-to-end for I/O work; avoid `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Propagate `CancellationToken` where long-running or cancellable work is involved.

### Clean architecture

This project follows a strict layered clean architecture. Respect the existing layer boundaries:

| Project | Layer | Responsibility |
|---------|-------|----------------|
| `ImportToPlanner.Domain` | Domain | Entities, value objects, business rules |
| `ImportToPlanner.Application` | Application | Use-case orchestration, application interfaces |
| `ImportToPlanner.Infrastructure.Graph` | Infrastructure | Microsoft Graph implementation details |
| `ImportToPlanner.Web` | Presentation | Blazor UI delivery only |

- **No business logic in `ImportToPlanner.Web`** — UI components should delegate to application services.
- Dependencies must point inward: `Web` → `Application` ← `Infrastructure.Graph`; `Application` → `Domain`.
- Do not place framework, Graph SDK, or EF/ORM concerns inside `Application` or `Domain`.

### Tests

- New features and bug fixes should include unit tests in `tests/ImportToPlanner.Tests`.
- Tests use [xUnit](https://xunit.net/). Follow the existing test style in the project.
- Aim for tests that cover the behaviour, not the implementation detail.

---

## What Not to Contribute

- **Changes to authentication or token handling** — this is a security-sensitive area. Please open an issue to discuss the change before writing any code.
- Large-scale refactors that go beyond the requested change — keep PRs focused.

---

## Licence

This project is licensed under the [MIT Licence](LICENSE). By submitting a pull request you confirm that your contribution is your own work and you agree it will be distributed under the same MIT licence. No formal CLA or DCO process is required.
