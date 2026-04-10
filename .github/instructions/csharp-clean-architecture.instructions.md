---
description: 'C# and project-file guidance with strict Uncle Bob style clean architecture requirements.'
applyTo: '**/*.cs, **/*.csproj'
---

# C# Clean Architecture Instructions

Use this file as baseline guidance for C# and project-file work that should follow strict Clean Architecture discipline.

## Intent

- Keep behaviour predictable, maintainable, and aligned with repository conventions.
- Prefer minimal, focused changes for the requested outcome.
- Apply Clean Architecture discipline strictly for C# design decisions.

## Scope and Change Discipline

- Follow repository conventions first unless they directly conflict with explicit user instructions.
- Keep changes small and scoped to the user request.
- Reuse existing abstractions before introducing new ones.
- Do not change SDK, target framework, or generated files unless explicitly requested.
- Do not perform opportunistic refactors outside the requested scope.
- This file applies to `*.razor.cs`, but Blazor component-facing decisions in those files are governed first by `blazor-mudblazor.instructions.md`.
- In `*.razor.cs`, apply these Clean Architecture rules primarily to non-UI policy boundaries and dependency direction.

## C# Version and Language Features

- Use modern C# features that are supported by the repository's target framework and SDK.
- Do not force or set a newer language version than the project supports unless explicitly requested.
- If language-version support is unclear, prefer the project defaults.

## C# Design and Reliability

- Use clear naming and least required visibility.
- Validate inputs at boundaries and throw precise exceptions.
- Prefer async end-to-end for I/O work.
- Avoid blocking async calls such as `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()`.
- Propagate `CancellationToken` when long-running or cancellable work is involved.

## Comments and Documentation

- Prefer self-documenting code and comments that explain intent or trade-offs.
- Avoid boilerplate comments that restate obvious code.
- For public APIs, include XML docs (`<summary>`, and when relevant `<param>`, `<returns>`, `<exception>`, `<example>`).
- For internal/private methods, add comments only when behaviour is non-obvious.

## Clean Architecture Requirements

- Organise code around policy and business rules, not frameworks, UI concerns, or data access details.
- Keep source-code dependencies pointing inward toward higher-level policy.
- Domain and application logic must not depend on UI, persistence, HTTP, database, or framework implementation details.
- Infrastructure concerns must implement interfaces required by inner layers instead of pulling inner layers outward.
- Do not place business rules in controllers, Razor components, EF entities, repositories, or other framework-facing types.
- Use boundaries explicitly: outer layers may depend on inner layers, but inner layers must not reference outer layers.
- Prefer use-case-oriented application services over generic manager or helper classes.
- Keep entities and value-carrying domain types focused on business meaning and invariants.
- Introduce interfaces only at architectural boundaries where they protect inner policy from outer details, not as speculative abstractions.
- Avoid static coupling to infrastructure concerns such as file systems, clocks, network clients, configuration providers, or ORM APIs inside business logic.
- When a requested change crosses layers, preserve or improve the dependency direction instead of taking a shortcut through the UI or infrastructure layer.
- If the existing code violates Clean Architecture, make the smallest change that moves the code toward proper boundaries without expanding scope into a broad rewrite.

## What Strict Uncle Bob Means Here

- Favour independence of frameworks over convenience of framework-driven design.
- Separate use-case orchestration from business rules and from delivery mechanisms.
- Keep data access as an implementation detail behind inward-facing contracts.
- Refuse layer leakage even when a shortcut would be faster to code.
- Avoid god classes, feature dumping, and mixed-responsibility services.

## Decision Priority

When guidance conflicts, apply this order:

1. User request
2. Repository conventions
3. This instruction file
4. General C#/.NET defaults

For `*.razor.cs`, Blazor component-facing concerns are governed first by `blazor-mudblazor.instructions.md`, while Clean Architecture governs non-UI policy boundaries and dependency direction.