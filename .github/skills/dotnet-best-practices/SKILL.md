---
name: dotnet-best-practices
description: 'Apply practical, repository-aligned .NET/C# engineering practices without forcing framework-specific or architecture-specific choices.'
---

# .NET/C# Best Practices (Neutral Baseline)

Your goal is to help produce maintainable, production-ready .NET code while respecting the repository’s existing conventions and technology choices.

## Core Rule

- Follow the repository’s established conventions first.
- If conventions are unclear, prefer standard modern .NET practices.
- Avoid introducing new frameworks, patterns, or architecture unless requested.

## Scope and Change Discipline

- Keep changes focused and minimal for the requested outcome.
- Reuse existing abstractions before creating new ones.
- Do not change target framework, SDK version, or global project structure unless explicitly asked.
- Do not edit generated files.

## API and Design

- Use clear names and cohesive methods.
- Prefer the least required visibility (`private` > `internal` > `protected` > `public`).
- Validate inputs at boundaries.
- Throw precise exceptions with actionable messages.
- Prefer composition and simple designs over speculative abstractions.

## Async and Reliability

- Use async for I/O-bound work end-to-end.
- Avoid blocking async calls (`.Result`, `.Wait()`).
- Propagate `CancellationToken` where appropriate.
- Add timeouts and retries only where they are operationally justified.

## Configuration and Observability

- Prefer strongly-typed configuration and validate required settings.
- Use structured logging with useful context.
- Avoid logging secrets or sensitive data.
- Preserve existing telemetry/logging patterns in the repository.

## Security and Data Handling

- Validate and sanitize untrusted input.
- Use parameterized data access patterns.
- Apply least-privilege principles for credentials and access.
- Keep secrets out of source code and logs.

## Testing Guidance (Framework-Neutral)

- Follow the testing framework already used by the solution.
- Add or update tests for changed behaviour, especially public-facing behaviour.
- Prefer deterministic tests that run independently.
- Use clear Arrange-Act-Assert structure.
- Mock only external dependencies when necessary.

## Documentation

- Document non-obvious behaviour and important design decisions.
- For public APIs, add or maintain XML docs where the repository expects them.
- Keep comments focused on intent and rationale, not restating code.

## Performance

- Prefer simple, readable implementations first.
- Optimize only when justified by measured evidence.
- Avoid unnecessary allocations in hot paths when practical.
- Stream large payloads when possible instead of buffering entire content.

## Decision Priority

When guidance conflicts, use this order:

1. User request
2. Repository conventions
3. This skill
4. General .NET defaults