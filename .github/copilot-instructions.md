---
description: 'Repository-specific guidance for Copilot assistance in the import-to-planner Blazor app.'
applyTo: 'All contributions'
---

# Copilot Instructions for markheydon/import-to-planner

## Repository Overview

This repository contains a single-purpose Blazor app with the sole function of importing tasks from CSV into Microsoft Planner.

## Primary Instruction Files

Refer to the following discipline-specific instruction files aligned with work type:

- [`.github/instructions/csharp-clean-architecture.instructions.md`](.github/instructions/csharp-clean-architecture.instructions.md) - Governs C# code organisation, design, and Clean Architecture principles
- [`.github/instructions/blazor-fluentui.instructions.md`](.github/instructions/blazor-fluentui.instructions.md) - Governs Blazor component patterns and Fluent UI usage

## Applicable Skills

Use the following skills for domain-specific guidance when working on relevant tasks:

- **`csharp-xunit`**: Unit test work and test framework patterns
- **`csharp-docs`**: Public API documentation and XML comments
- **`csharp-async`**: Async/await patterns and best practices
- **`fluentui-blazor`**: Fluent UI component usage and troubleshooting
- **`dotnet-best-practices-repo`**: General .NET and repository-aligned practices
- **`github-issues`**: Issue management, labelling, and GitHub workflows

## Language & Style

- All internal and end-user-facing documentation, including code comments, must be in UK English (colour, behaviour, organisation, etc.).
- Documentation should be concise, friendly, and welcoming to contributors who may want to adapt implementations.
- YAML examples must use spaces only (never tabs).

## Microsoft Graph Guidelines

This app uses Microsoft Graph API to access Microsoft Planner and related assets. Observe these principles:

- Treat `Microsoft.Graph` as the primary contract for domain and application logic.
- Keep Kiota as an internal SDK implementation detail, do not expose Kiota types in domain or application code.
- Remove explicit Kiota usage from your code where practical.
- Accept that `Microsoft.Graph` v5 may bring Kiota transitively; this is acceptable.

## Testing & Coverage

- Unit tests are in `tests/ImportToPlanner.Tests/` using xUnit framework.
- Mock `IPlannerGateway` implementations when testing orchestration logic.
- Prefer integration tests for Graph API interactions using `InMemoryPlannerGateway` in test scenarios.
- Refer to the `csharp-xunit` skill for test patterns and data-driven test guidance.

## PR Review Comment Handling

- When addressing pull request review threads, reply in-thread only.
- Never use top-level PR/issue comments as a substitute for thread replies.
- If tooling cannot post an in-thread reply, stop and report the limitation with draft reply text per thread.
- Do not resolve a review thread unless an in-thread reply has been posted.

## C# Agent Support

For complex C# coding work, defer to the **C# Expert** agent. It has deep expertise in .NET architecture and can handle refactoring, design decisions, and implementation at scale.