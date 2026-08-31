# AGENTS.md — Repository agent policy

This file is the single source of repository-wide policy for AI agents, regardless of
which product loads it. GitHub Copilot auto-loads
`.github/copilot-instructions.md`; that file is a pointer to this one and MUST NOT
hold independent policy.

## Precedence (authoritative)

When repository guidance conflicts, resolve according to this order:

1. `AGENTS.md` (this file) — repository-wide policy, agent registry, and skill mapping
2. `.specify/memory/constitution.md` — Spec Kit architecture governance
3. Other instruction, skill, or agent files (for example, files under
   `.github/instructions/`, `.github/skills/`, `.github/agents/`, `.agents/skills/`,
   or `.specify/`)

This order keeps repo-level policy in one place while allowing Spec Kit and
discipline-specific files to operate.

## Repository overview

This repository contains a single-purpose Blazor app whose sole function is importing
tasks from CSV into Microsoft Planner.

## Purpose

Do not modify third-party-sourced skill or instruction files unless that is the only
way to achieve the desired result or the maintainer has approved the change. Prefer
updating this file to express repo-level overrides so upstream refreshes of
third-party files do not lose local policy.

## Portable vs repository-specific assets

- `.agents/` contains portable, generic skills and related assets. In this repository
  those assets may be imported from upstream tooling, such as the .NET Aspire CLI, so
  they should normally be treated as shared resources that are unlikely to need local
  edits.
- `.github/` contains repository-specific agent customisations. Use `.github/skills/`,
  `.github/agents/`, `.github/instructions/`, and `.github/prompts/` for
  import-to-planner-specific constraints, tighter guidance, and local behaviour changes.
- If a portable skill from `.agents/` needs repository-specific refinement, implement
  that refinement in `.github/` instead of changing the portable source unless the
  change is intentionally meant to update the shared generic skill.

## Primary instruction files

Refer to the following discipline-specific instruction files aligned with work type:

- `.github/instructions/csharp-clean-architecture.instructions.md` — C# organisation,
  design, and Clean Architecture
- `.github/instructions/blazor-csharp.instructions.md` — Blazor component patterns and
  C# conventions

## Language and style

- All internal and end-user-facing documentation, including code comments, must be in
  UK English (colour, behaviour, organisation, and so on).
- Documentation should be concise, friendly, and welcoming to contributors who may want
  to adapt implementations.
- YAML examples must use spaces only (never tabs).

## Microsoft Graph

See `docs-internal/microsoft-graph-guidelines.md` for implementation guidance on using
Microsoft Graph in this repository.

## Testing, coverage, and format

See `tests/README.md` for test-running and coverage guidance. Mandatory testing and
runtime-mode standards that are not architecture-constitutional live in
`docs-internal/engineering-policies.md`.

Before proposing or finalising any code change, agents MUST run:

```bash
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

CI enforces this check in `.github/workflows/ci.yml`, so skipping it will cause
avoidable failures.

## Pull request review comments

See `CONTRIBUTING.md` for PR review reply policy and guidance on handling review threads.

## Agent registry

- **C# coding and architecture tasks**: `.github/agents/CSharpExpert.agent.md` — primary
  agent for C# coding, architecture, refactorings, and test guidance.
- **Tech Writer (future)**: add a new agent file under `.github/agents/` and register it
  here.

Any other work should be handled by the agent already in session, unless specific
delegation is added here.

## Skills and delegation

The following skills are expected to be used by agents handling related tasks. Exact
delegation rules and precedence are defined above.

*Note*: The following list is in alphabetical order and does not indicate precedence:

- `aspire`
- `csharp-async`
- `csharp-docs`
- `csharp-xunit`
- `dotnet-best-practices-repo`
- `end-user-docs`
- `github-issues`
- `microsoft-docs`
- `mudblazor`
- `repo-readme-generator`

Expected delegation:

- Coding, architecture, and tests → C# Expert agent (uses `csharp-async`,
  `csharp-docs`, `csharp-xunit`, `dotnet-best-practices-repo`) for .NET/C#
  implementation, refactoring, async/reliability fixes, documentation updates, and
  unit/integration test work; do not use this path for non-.NET stacks unless
  explicitly requested
- Blazor UI work → C# Expert agent using the `mudblazor` skill for all component
  implementation, layout, theming, dialogs, and troubleshooting; refer to the skill's
  decision order and reference files before writing any custom CSS or HTML
- .NET Aspire projects and distributed application architecture → C# Expert agent
  (uses `aspire`) when tasks involve AppHost/resource orchestration, Aspire CLI
  operations (`aspire start`, `aspire describe`, `aspire logs`, `aspire otel`,
  `aspire add`, `aspire doctor`, `aspire resource rebuild`), integrations, or
  distributed diagnostics; do not use for non-Aspire .NET apps (use `dotnet`),
  container-only workflows (use Docker/Podman), or Azure deployment execution after
  local validation
- Issue / GitHub workflow tasks → `github-issues` skill for issue creation/updates,
  labelling, dependencies, and workflow metadata; do not use it as the default for PR
  code-review implementation or general repository coding tasks
- Microsoft/.NET/Azure documentation research and code-sample lookup →
  `microsoft-docs` skill for authoritative references, API guidance, and official
  examples; do not use it as a replacement for repository-specific policy files or
  local codebase analysis
- Public end-user documentation authoring under `docs/` → `end-user-docs` skill for
  structure, tone, UK English, and contract-aligned coverage; do not use it for
  internal engineering content under `docs-internal/` or for C# implementation work
- Repository (root only) README generation or significant README restructuring →
  `repo-readme-generator` skill for documentation synthesis from repository artefacts;
  do not use it for small targeted content edits where direct manual updates are
  clearer

Implementation workflow expectation:

- During `/speckit.implement`, any coding, architecture, and test implementation tasks
  MUST be delegated to the registered C# Expert agent when available.
- During `/speckit.implement`, public docs authoring or refinement tasks under `docs/`
  MUST apply the `end-user-docs` skill as the default writing guidance.
- If a task is not suitable for C# Expert, the exception and rationale MUST be recorded
  in the plan/PR notes.
- When the AppHost is running: After the C# Expert agent completes coding, testing, and
  validation, it MUST issue `aspire resource <resource> rebuild` for any .NET project
  resource that was modified. This ensures the user receives the latest compiled code
  ready for testing, eliminating manual rebuild steps.

## How to request or change policy or delegation

- Add or modify agent entries by editing `AGENTS.md` and opening a PR.
- Repository-level policy changes (precedence, language rules, format gates, and
  similar) belong in this file. Note the reason in the PR.

## Conflict handling and guidance

If a skill or instruction file under `.github/skills/`, `.agents/skills/`, or
`.github/instructions/` conflicts with higher-priority files, follow the Precedence
list above. Avoid editing third-party files; instead, add an override or clarification
here.

## Non-constitutional repository policies

The constitution focuses on architecture governance. Operational and delivery policies
that still remain mandatory are preserved in `docs-internal/engineering-policies.md`.

All agents and contributors MUST continue to follow those policies, including:

- testing and runtime-mode verification expectations
- UX, accessibility, and UK English wording requirements
- performance evidence and operational safety expectations
- external integration constraints and scope controls

Agent-process requirements defined in this file remain mandatory and continuous
throughout all work phases.

Maintainers: modify this file to reflect approved changes in agent policy, delegation,
or precedence.
