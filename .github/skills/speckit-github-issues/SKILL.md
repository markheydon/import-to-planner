---
name: speckit-github-issues
description: >-
  Create or update GitHub issues that kick off Spec Kit (specify → plan → tasks).
  Use when opening a feature or story that will use /speckit.specify and
  /speckit.plan, or when splitting product intent from implementation seeds.
---

# Spec Kit–aligned GitHub issues

Create issues that humans can triage and agents can execute without mixing WHAT
and HOW. Works with any agent that can read the issue and run Spec Kit skills;
Cursor users paste seeds from the issue into chat or slash commands.

## Principles

1. **Issue** = product contract, scope, acceptance criteria, links, dependencies.
2. **`/speckit.specify` seed** = WHAT/WHY only (outcomes, actors, rules, edge cases).
3. **`/speckit.plan` seed** = HOW (stack, boundaries, jobs, storage, tests, research).
4. Do not duplicate full `spec.md` or `plan.md` in the issue.
5. UK English for user-facing text in issues and seeds.

## When to use

- New hosted feature, commercial billing slice, or cross-cutting epic tracked on GitHub.
- Reopening or reframing an issue before `/speckit.specify`.
- Splitting an epic (#N) into implementable stories (child issues link to parent).

## Issue body structure (feature)

Use the github-issues feature template as a base, then add:

| Section | Purpose |
|---------|---------|
| Summary | One paragraph + links to done dependencies |
| Motivation | Why now |
| In scope / Out of scope | Bullet boundaries |
| Acceptance criteria | Testable checkboxes (product level) |
| Dependencies | Issues, specs, docs (`docs/…`, not `docs-internal/…` unless that path exists) |
| Suggested `/speckit.specify` seed | Fenced `text` block, WHAT only |
| Suggested `/speckit.plan` hints | Fenced `text` block, HOW only (optional until specify exists) |

## Issue body structure (story)

Smaller than a feature:

- **Objective** (single user-visible outcome)
- **Depends on** (parent feature issue + spec directory if known)
- **Acceptance criteria** (3–7 bullets)
- **Specify seed** (short WHAT paragraph; no plan block until parent spec exists)

## What goes where (cheat sheet)

| Topic | Issue | Specify | Plan |
|-------|-------|---------|------|
| User can buy credits after shortfall | AC + in scope | User scenario + FR | Checkout/webhook adapter |
| FIFO consumption | In scope (reference ledger feature) | FR referencing behaviour | Store algorithm / tests |
| 12-month paid expiry | AC | FR + success criteria | Scheduler host, ledger entry types |
| Account delete vs balance | AC + narrative | FR (access blocked, ledger retained) | Confirm no ledger purge; Stripe metadata |
| xUnit, format gate, AppHost tests | Optional footnote “repo policy” | **No** | Yes + quickstart |
| Stripe vs “payment provider” | Issue may name Stripe for triage | Prefer behaviour wording | Name Stripe.NET, endpoints |

## Specify seed checklist

Before publishing the seed, verify:

- [ ] No project names, frameworks, or CI commands
- [ ] Success outcomes stated in user/business language
- [ ] Edge cases (idempotency, commercial-off, self-host) included
- [ ] Clarifications limited; flag only decision-breaking ambiguities
- [ ] Points to `docs/credits-billing-usage-model.md` (or correct path) and issue number

## Plan hints checklist

- [ ] Constitution / Clean Architecture boundaries
- [ ] Storage tables and partition keys
- [ ] Background job choice (hosted service vs Worker vs Azure Functions + AppHost)
- [ ] External integrations and secrets pattern
- [ ] Test scope per `docs/engineering-policies.md`

## Cursor-specific notes

- Spec Kit skills: `.cursor/skills/speckit-specify/SKILL.md`, `speckit-plan/SKILL.md`.
- Feature directory is stored in `.specify/feature.json`; branch name is independent.
- Optional git hooks in `.specify/extensions.yml` may run on specify/plan.
- After implementation with AppHost running, repo policy expects resource rebuild for touched projects (see `AGENTS.md`).

## Creating/updating issues

- **Read:** GitHub MCP or `gh issue view`
- **Write:** `gh issue edit` / `gh api` (see `.github/skills/github-issues/SKILL.md`)
- When reshaping seeds, update the fenced blocks in place; do not run `/speckit.specify` in the same task unless the user asked.

## Examples

### Feature (billing)

- Issue: type/feature, size label, AC for webhook idempotency and ledger rules.
- Specify seed: payment confirmation grants lots; consumption order; tenant scope.
- Plan hints: Stripe adapter, expiry scheduler, `TableCreditLedgerStore` extensions.

### Story (UI slice)

- Issue: “Buy credits button on preview shortfall” depends on #125.
- Specify seed: only if story is spec’d alone; otherwise reference parent spec FR IDs.
- Plan hints: MudBlazor flow, navigation port, no new storage.

## Done when

- [ ] Issue AC match specify-ready language
- [ ] Specify seed passes specify skill “no implementation” checklist
- [ ] Plan hints separated and optional until spec exists
- [ ] Dependencies and doc paths verified in repo
