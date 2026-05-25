---
description: 'Repository-specific guidance for Copilot assistance in the import-to-planner Blazor app.'
applyTo: 'All contributions'
---

# Copilot Instructions for markheydon/import-to-planner

**Repository precedence:** When guidance or instructions conflict, follow this authoritative order:

1. `.github/copilot-instructions.md` (this file) — repository-wide policy (highest authority)
2. `AGENTS.md` (root) — repo-level agent delegation and skill mapping
3. `.specify/memory/constitution.md` — Spec Kit governance
4. Other instruction, skill, or agent files (for example, files under `.github/instructions/`, `.github/skills/`)

Refer to `AGENTS.md` for the authoritative agent registry and skill delegation. Avoid editing third-party-sourced skill/instruction files directly; prefer adding overrides here or in `AGENTS.md`.

## Repository Overview

This repository contains a single-purpose Blazor app with the sole function of importing tasks from CSV into Microsoft Planner.

## Primary Instruction Files

Refer to the following discipline-specific instruction files aligned with work type:

- `.github/instructions/csharp-clean-architecture.instructions.md` - Governs C# code organisation, design, and Clean Architecture principles
- `.github/instructions/blazor-csharp.instructions.md` - Governs Blazor component patterns and C# conventions

## Applicable skills and agent registry

This file no longer lists skills inline. See `AGENTS.md` at the repository root for the authoritative agent registry and the list of applicable skills and their delegation.

## Language & Style

- All internal and end-user-facing documentation, including code comments, must be in UK English (colour, behaviour, organisation, etc.).
- Documentation should be concise, friendly, and welcoming to contributors who may want to adapt implementations.
- YAML examples must use spaces only (never tabs).

## Microsoft Graph Guidelines

See `docs-internal/microsoft-graph-guidelines.md` for implementation guidance on using Microsoft Graph in this repository.


## Testing & Coverage

See `tests/README.md` for test-running and coverage guidance. Mandatory testing standards (for example, verifying both `PlannerGateway:UseGraph` runtime modes when planner behaviour is affected) remain in `.specify/memory/constitution.md`.


## PR Review Comment Handling

See `CONTRIBUTING.md` for PR review reply policy and guidance on handling review threads.

## C# Agent Support

See `AGENTS.md` for agent delegation. By default, coding, architecture, and test tasks should be delegated to `.github/agents/CSharpExpert.agent.md` unless AGENTS.md indicates otherwise.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read specs/005-simplify-graph-path/plan.md
<!-- SPECKIT END -->
