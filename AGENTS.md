# AGENTS.md — Repository agent registry and precedence

This file centralises repository-level agent delegation, defines the precedence to use when instruction or skill files disagree, along with some other information that AI agents should be taking note of.

## Precedence (authoritative)

When repository guidance conflicts, resolve according to this order:

1. `.github/copilot-instructions.md` — repository-wide policy (highest authority)
2. `AGENTS.md` (this file) — repo-level agent delegation and skill mapping
3. `.specify/memory/constitution.md` — Spec Kit governance document
4. Other instruction, skill, or agent files (for example, files under `.github/instructions/`, `.github/skills/`, or `.specify/`)

This order preserves the maintainer's ability to set repository-wide overrides while allowing Spec Kit and other documentation to operate.

## Purpose

Do not modify third-party-sourced skill or instruction files unless absolutely the only way to achieve the results desired or where maintainer approval has been given. Prefer updating `AGENTS.md` or `.github/copilot-instructions.md` to express repo-level overrides so upstream refreshes of third-party files do not lose local policy.

## Agent registry

- **C# coding & architecture tasks**: `.github/agents/CSharpExpert.agent.md` — Primary agent for C# coding, architecture, refactorings, and test guidance.
- **Tech Writer (future)**: add a new agent file under `.github/agents/` and register it here.

Any other work, the standard Copilot agent will be expected to handle, unless specific delegation is added here or in `.github/copilot-instructions.md`.

## Skills and delegation

The following skills are expected to be used by agents handling related tasks. Exact delegation rules and precedence are defined above.

- `csharp-xunit`
- `csharp-docs`
- `csharp-async`
- `fluentui-blazor`
- `dotnet-best-practices-repo`
- `github-issues`
- `aspire`

Typical delegation:

- Coding, architecture, and tests → C# Expert agent (uses `csharp-async`, `csharp-docs`, `csharp-xunit`, `dotnet-best-practices-repo`)
- Blazor UI work → `fluentui-blazor` skill (C# Expert remains primary for code-level changes)
- Issue / GitHub workflow tasks → `github-issues` skill

Implementation workflow expectation:

- During `/speckit.implement`, any coding, architecture, and test implementation tasks
	MUST be delegated to the registered C# Expert agent when available.
- If a task is not suitable for C# Expert, the exception and rationale MUST be recorded
	in the plan/PR notes.

## How to request or change delegation

- Add or modify agent entries by editing `AGENTS.md` and opening a PR.
- If you need a repository-level policy change (for example, changing precedence or global language rules), update `.github/copilot-instructions.md` (it is the authoritative file) and note the reason in the PR.

## Conflict handling and guidance

If a skill or instruction file under `.github/skills/` or `.github/instructions/` conflicts with higher-priority files, follow the Precedence list above. Avoid editing third-party files; instead, add an override or clarification here or in `.github/copilot-instructions.md`.

Maintainers: modify this file to reflect approved changes in agent delegation or precedence.
