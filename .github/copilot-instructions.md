---
description: 'Repository-specific guidance for Copilot assistance in the import-to-planner Blazor app.'
applyTo: 'All contributions'
---

# Copilot Instructions for markheydon/import-to-planner

**Repository precedence:** When guidance or instructions conflict, follow this authoritative order:

1. `.github/copilot-instructions.md` (this file) — repository-wide policy (highest authority)
2. `AGENTS.md` (root) — repo-level agent delegation and skill mapping
3. `.specify/memory/constitution.md` — Spec Kit governance
4. Other instruction, skill, or agent files (for example, files under `.github/instructions/`, `.github/skills/`, or `.agents/skills/`)

Refer to `AGENTS.md` for the authoritative agent registry and skill delegation. Avoid editing third-party-sourced skill/instruction files directly; prefer adding overrides here or in `AGENTS.md`.

## Repository Overview

This repository contains a single-purpose Blazor app with the sole function of importing tasks from CSV into Microsoft Planner.

## Primary Instruction Files

Refer to the following discipline-specific instruction files aligned with work type:

- `.github/instructions/csharp-clean-architecture.instructions.md` - Governs C# code organisation, design, and Clean Architecture principles
- `.github/instructions/blazor-csharp.instructions.md` - Governs Blazor component patterns and C# conventions

## Applicable skills and agent registry

This file no longer lists skills inline. See `AGENTS.md` at the repository root for the authoritative agent registry and the list of applicable skills and their delegation.

## Portable vs Repository-Specific Assets

- Treat assets under `.agents/` as portable, shared customisations. In this repository they primarily hold generic skills imported from upstream tooling, such as the .NET Aspire CLI, and they should usually remain unchanged unless the shared source itself is being updated.
- Treat assets under `.github/` as repository-specific guidance. This includes `.github/skills/`, `.github/agents/`, `.github/instructions/`, and `.github/prompts/`, which are the preferred places for import-to-planner-specific tightening, overrides, and behavioural guidance.
- When a generic skill needs repository-specific behaviour, prefer adding or adjusting `.github/` assets rather than editing the portable copy under `.agents/`.

## Language & Style

- All internal and end-user-facing documentation, including code comments, must be in UK English (colour, behaviour, organisation, etc.).
- Documentation should be concise, friendly, and welcoming to contributors who may want to adapt implementations.
- YAML examples must use spaces only (never tabs).

## Microsoft Graph Guidelines

See `docs-internal/microsoft-graph-guidelines.md` for implementation guidance on using Microsoft Graph in this repository.


## Testing & Coverage

See `tests/README.md` for test-running and coverage guidance. Mandatory testing standards (for example, validating startup configuration and authority-specific auth guards when planner behaviour is affected) remain in `.specify/memory/constitution.md`.

Before proposing or finalising any code change, agents MUST run:

```bash
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

CI enforces this check in `.github/workflows/ci.yml`, so skipping it will cause avoidable failures.


## PR Review Comment Handling

See `CONTRIBUTING.md` for PR review reply policy and guidance on handling review threads.

## C# Agent Support

See `AGENTS.md` for agent delegation. By default, coding, architecture, and test tasks should be delegated to `.github/agents/CSharpExpert.agent.md` unless AGENTS.md indicates otherwise.
For public end-user documentation work under `docs/`, follow AGENTS.md delegation and apply the `.github/skills/end-user-docs/SKILL.md` guidance.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read specs/008-commercial-user-accounts/plan.md
<!-- SPECKIT END -->
