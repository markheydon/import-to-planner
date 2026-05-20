---
agent: 'agent'
model: GPT-5.3-Codex
description: Create a draft or ready pull request for the current feature by reading .specify/feature.json, the active spec folder, and the repository PR template, then preserve the template structure while drafting and optionally opening the PR.
argument-hint: Optional feature directory or code, for example "specs/004-add-multitenant-hosting", "004", or "draft"
---

Create a pull request for the current feature using repository-native context.

## When to use this prompt

- After implementing a feature branch that is backed by a Spec Kit feature folder.
- When you want Copilot to read the active feature context before drafting the PR.
- When you want the PR body to preserve `.github/pull_request_template.md` exactly instead of collapsing into free-form prose.

## What you'll get

- The active feature directory resolved from `.specify/feature.json` first, with branch-name fallback only if needed.
- A PR title derived from the implemented change on the branch.
- A completed PR body that keeps the repository template headings and checklist sections intact.
- A draft or ready PR targeting `main`, unless the user explicitly overrides that.
- A stop-and-confirm step when there is ambiguity, missing evidence, or a mismatch between the active feature metadata and the current branch.

## Instructions

### 1. Resolve the active feature first

1. Read `.specify/feature.json` and treat `feature_directory` as the authoritative current feature path when it is present.
2. If `.specify/feature.json` is missing, empty, or clearly stale, inspect the current git branch and infer the feature directory from the repository's `NNN-description` naming convention.
3. If `.specify/feature.json` and the current branch disagree, stop and ask the user which feature should drive the PR.
4. If neither source resolves a feature directory, stop and ask the user to provide the feature code or spec folder path.

### 2. Gather only the context needed for the PR

Read these sources before drafting anything:

1. `CONTRIBUTING.md`
2. `.github/pull_request_template.md`
3. `.specify/feature.json`
4. `specs/{feature}/spec.md`
5. `specs/{feature}/plan.md`
6. `specs/{feature}/quickstart.md`, if present
7. `specs/{feature}/tasks.md`, if present
8. Current git branch, git status, and a concise diff summary for the branch

Use the feature documents to understand intent and acceptance criteria, but use the actual branch diff and repository state to describe what was really implemented.

### 3. Validate PR readiness before opening anything

1. Check for uncommitted or unpushed changes.
2. If there are uncommitted changes, ask whether to stop, commit first, or continue only if there are already relevant commits on the branch.
3. If the branch is not on the remote, push or publish it before creating the PR.
4. If the implemented diff appears to span more than one feature or spec area, stop and ask for confirmation.
5. If required evidence for testing, runtime-mode verification, UX, or documentation is missing, call that out clearly before creating the PR.

### 4. Draft the PR title and body

1. Derive the PR title from the implemented change on the branch, not just from the feature folder name.
2. Keep the title concise, imperative, and suitable for a GitHub PR.
3. Use `.github/pull_request_template.md` as the exact PR body structure.
4. Preserve every heading and checklist section from the template.
5. Fill the template using the branch changes plus the resolved feature documents.
6. If a section cannot be completed confidently, write `N/A` with a short reason instead of removing the section.
7. Use UK English.
8. Do not rewrite the PR into free-form summary prose.

### 5. Create the PR carefully

1. Target `main` unless the user explicitly overrides it.
2. Default to a draft PR unless the user clearly asks for a ready-for-review PR.
3. If there is ambiguity about the title, missing evidence, feature selection, or draft status, show the drafted title and PR body and ask for confirmation before opening the PR.
4. If everything is clear, create the PR using the normal pull-request workflow.

### 6. Report the outcome

After creating the PR:

1. Return the PR number and URL.
2. State which feature directory was used.
3. Mention any sections that were marked `N/A`.
4. Mention any notable follow-up items, such as missing evidence or checks still to run.

## Rules

- Treat `.specify/feature.json` as the primary current-feature source.
- Use branch-name inference only as a fallback or mismatch check.
- Never silently choose between conflicting feature sources.
- Never drop sections from `.github/pull_request_template.md`.
- Never claim checks or verification that are not supported by the repository state or explicit user confirmation.
- Keep the output concise, practical, and repository-specific.
