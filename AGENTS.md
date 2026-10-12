# AGENTS.md — Repository agent policy

This file is the single source of repository-wide policy for AI agents, regardless of
which product loads it.

## Precedence (authoritative)

When repository guidance conflicts, resolve according to this order:

1. `AGENTS.md` (this file) — repository-wide policy and skill mapping
2. `.specify/memory/constitution.md` — Spec Kit architecture governance
3. Other skill or workflow files (for example, files under `.agents/skills/`,
   `.cursor/skills/`, or `.specify/`)

This order keeps repo-level policy in one place while allowing Spec Kit and
discipline-specific files to operate.

## Repository overview

This repository contains a single-purpose Blazor app whose sole function is importing
tasks from CSV into Microsoft Planner.

## Purpose

Do not modify third-party-sourced skill files unless that is the only way to achieve
the desired result or the maintainer has approved the change. Prefer updating this file
to express repo-level overrides so upstream refreshes of third-party files do not lose
local policy.

## Agent asset layout

| Location | Role |
|----------|------|
| `AGENTS.md` | Single policy source: precedence and delegation |
| `.agents/skills/` | Skills (upstream Aspire/Playwright/Spec Kit git bundles and repo-specific skills) |
| `.cursor/skills/speckit-*` | Spec Kit workflow skills (Cursor integration; installer-managed) |
| `.cursor/rules/` | Optional glob-scoped pointers (for example C# and Blazor) |
| `.specify/` | Spec Kit templates, scripts, constitution memory, integration manifests |

`.github/` holds CI, issue/PR templates, and repository governance only—not agent skills,
instructions, or prompts. The sole exception is `.github/copilot-instructions.md`, a
required pointer to this file for GitHub Copilot.

Repository-specific skills live under `.agents/skills/` alongside portable upstream
bundles. Do not edit upstream skill bodies for local policy; override here in
`AGENTS.md` instead.

## C# and architecture standards

For .NET/C# coding, architecture, refactorings, and tests, follow:

- `.specify/memory/constitution.md`
- `docs/engineering-policies.md`
- `docs/microsoft-graph-guidelines.md` for Microsoft Graph work
- `.cursor/rules/csharp-and-blazor.mdc` when editing C# or Blazor sources (glob-scoped)

Do not use this path for non-.NET stacks unless explicitly requested.

## Language and style

- All internal and end-user-facing documentation, including code comments, must be in
  UK English (colour, behaviour, organisation, and so on).
- Documentation should be concise, friendly, and welcoming to contributors who may want
  to adapt implementations.
- YAML examples must use spaces only (never tabs).

## Microsoft Graph

See `docs/microsoft-graph-guidelines.md` for implementation guidance on using
Microsoft Graph in this repository.

## Testing, coverage, and format

See `tests/README.md` for test-running and coverage guidance. Mandatory testing and
runtime-mode standards that are not architecture-constitutional live in
`docs/engineering-policies.md`.

Before proposing or finalising any code change, agents MUST run:

```bash
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

CI enforces this check in `.github/workflows/ci.yml`, so skipping it will cause
avoidable failures.

## Pull request review comments

See `CONTRIBUTING.md` for PR review reply policy and guidance on handling review threads.

## Skills and delegation

The following skills are expected to be used by agents handling related tasks. Exact
delegation rules and precedence are defined above.

*Note*: The following list is in alphabetical order and does not indicate precedence:

- `aspire`
- `end-user-docs`
- `github-issue-spec-seeds`
- `github-issues`
- `mudblazor`
- `repo-readme-generator`

Expected delegation:

- Coding, architecture, and tests → constitution, `docs/engineering-policies.md`, and
  the C#/Blazor rule above for .NET/C# implementation, refactoring, async/reliability
  fixes, documentation updates, and unit/integration test work. Generated tests MUST
  follow `docs/engineering-policies.md` (xUnit v3, NSubstitute, built-in Assert; no
  AppHost tests; Playwright only for explicit end-to-end journeys).
- Blazor UI work → `mudblazor` skill for all component implementation, layout, theming,
  dialogs, and troubleshooting; refer to the skill's decision order and reference files
  before writing any custom CSS or HTML.
- .NET Aspire projects and distributed application architecture → `aspire` skill when
  tasks involve AppHost/resource orchestration, Aspire CLI operations (`aspire start`,
  `aspire describe`, `aspire logs`, `aspire otel`, `aspire add`, `aspire doctor`,
  `aspire resource rebuild`), integrations, or distributed diagnostics; do not use for
  non-Aspire .NET apps (use `dotnet`), container-only workflows (use Docker/Podman), or
  Azure deployment execution after local validation.
- Issue / GitHub workflow tasks → `github-issues` skill for issue creation/updates,
  labelling, dependencies, and workflow metadata; do not use it as the default for PR
  code-review implementation or general repository coding tasks.
- GitHub issues that will use Spec Kit (`/speckit.specify`, `/speckit.plan`) →
  `github-issue-spec-seeds` skill for WHAT/HOW seed separation and issue body structure;
  use `github-issues` for the actual GitHub API/MCP writes.
- Public end-user documentation (Hugo site under `website/content/`) → `end-user-docs`
  skill for structure, tone, UK English, and contract-aligned coverage; do not use it
  for internal engineering content under `docs/` or for C# implementation work.
- Repository (root only) README generation or significant README restructuring →
  `repo-readme-generator` skill for documentation synthesis from repository artefacts;
  do not use it for small targeted content edits where direct manual updates are
  clearer.

Implementation workflow expectation:

- During `/speckit.implement`, any coding, architecture, and test implementation tasks
  MUST follow repository C# standards in this file and `docs/engineering-policies.md`.
- During `/speckit.implement`, public docs authoring or refinement under
  `website/content/` MUST apply the `end-user-docs` skill as the default writing
  guidance.
- When the AppHost is running: after coding, testing, and validation, issue
  `aspire resource <resource> rebuild` for any .NET project resource that was modified.
  This ensures the user receives the latest compiled code ready for testing, eliminating
  manual rebuild steps.

## How to request or change policy or delegation

- Repository-level policy changes (precedence, language rules, format gates, skill list,
  and similar) belong in this file. Note the reason in the PR.

## Conflict handling and guidance

If a skill file under `.agents/skills/` conflicts with higher-priority files, follow the
Precedence list above. Avoid editing third-party skill sources; add an override or
clarification here instead.

## Non-constitutional repository policies

The constitution focuses on architecture governance. Operational and delivery policies
that still remain mandatory are preserved in `docs/engineering-policies.md`.

All agents and contributors MUST continue to follow those policies, including:

- testing and runtime-mode verification expectations
- UX, accessibility, and UK English wording requirements
- performance evidence and operational safety expectations
- external integration constraints and scope controls

Agent-process requirements defined in this file remain mandatory and continuous
throughout all work phases.

Maintainers: modify this file to reflect approved changes in agent policy, delegation,
or precedence.
