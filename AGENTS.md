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

*Note*: The following list is in alphabetical order and does not indicate precedence:

- `aspire`
- `csharp-async`
- `csharp-docs`
- `csharp-xunit`
- `dotnet-best-practices-repo`
- `github-issues`
- `microsoft-docs`
- `mudblazor`
- `repo-readme-generator`

Expected delegation:

- Coding, architecture, and tests → C# Expert agent (uses `csharp-async`, `csharp-docs`, `csharp-xunit`, `dotnet-best-practices-repo`) for .NET/C# implementation, refactoring, async/reliability fixes, documentation updates, and unit/integration test work; do not use this path for non-.NET stacks unless explicitly requested
- Blazor UI work → C# Expert agent using the `mudblazor` skill for all component implementation, layout, theming, dialogs, and troubleshooting; refer to the skill's decision order and reference files before writing any custom CSS or HTML
- .NET Aspire projects and distributed application architecture → C# Expert agent (uses `aspire`) when tasks involve AppHost/resource orchestration, Aspire CLI operations (`aspire start`, `aspire describe`, `aspire logs`, `aspire otel`, `aspire add`, `aspire doctor`, `aspire resource rebuild`), integrations, or distributed diagnostics; do not use for non-Aspire .NET apps (use `dotnet`), container-only workflows (use Docker/Podman), or Azure deployment execution after local validation
- Issue / GitHub workflow tasks → `github-issues` skill for issue creation/updates, labelling, dependencies, and workflow metadata; do not use it as the default for PR code-review implementation or general repository coding tasks
- Microsoft/.NET/Azure documentation research and code-sample lookup → `microsoft-docs` skill for authoritative references, API guidance, and official examples; do not use it as a replacement for repository-specific policy files or local codebase analysis
- Repository (root only) README generation or significant README restructuring → `repo-readme-generator` skill for documentation synthesis from repository artefacts; do not use it for small targeted content edits where direct manual updates are clearer

Implementation workflow expectation:

- During `/speckit.implement`, any coding, architecture, and test implementation tasks
	MUST be delegated to the registered C# Expert agent when available.
- If a task is not suitable for C# Expert, the exception and rationale MUST be recorded
	in the plan/PR notes.
- When the AppHost is running: After the C# Expert agent completes coding, testing, and validation,
	it MUST issue `aspire resource <resource> rebuild` for any .NET project resource that was modified.
	This ensures the user receives the latest compiled code ready for testing, eliminating manual rebuild steps.

## How to request or change delegation

- Add or modify agent entries by editing `AGENTS.md` and opening a PR.
- If you need a repository-level policy change (for example, changing precedence or global language rules), update `.github/copilot-instructions.md` (it is the authoritative file) and note the reason in the PR.

## Conflict handling and guidance

If a skill or instruction file under `.github/skills/` or `.github/instructions/` conflicts with higher-priority files, follow the Precedence list above. Avoid editing third-party files; instead, add an override or clarification here or in `.github/copilot-instructions.md`.

## Non-Constitutional Repository Policies

The constitution now focuses on strict architecture governance. Operational and delivery
policies that still remain mandatory are preserved in
`docs-internal/engineering-policies.md`.

All agents and contributors MUST continue to follow those policies, including:

- testing and runtime-mode verification expectations
- UX, accessibility, and UK English wording requirements
- performance evidence and operational safety expectations
- external integration constraints and scope controls

Agent-process requirements defined in this file remain mandatory and continuous
throughout all work phases.

Maintainers: modify this file to reflect approved changes in agent delegation or precedence.
