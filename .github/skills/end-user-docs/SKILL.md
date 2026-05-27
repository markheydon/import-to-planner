---
name: end-user-docs
description: "Write and improve end-user documentation for Import To Planner public docs pages. Use when creating or editing docs/ pages such as index, getting-started, csv-format, import-workflow, troubleshooting, faq, privacy-and-security, or self-hosted; when improving wording and structure for non-technical readers; when enforcing UK English; when validating docs against repository contracts/specs and public-content boundaries."
---

# End-User Docs Writer

Author guidance for public-facing, non-technical documentation in this repository.

---

## Use This Skill When

- The task is writing or refining public user docs under `docs/`.
- The audience is hosted end users and administrators, not developers.
- The request is about wording quality, page structure, readability, or clarity.
- You need to keep docs aligned with verified application behaviour and feature contracts.

Do not use this skill for internal engineering material under `docs-internal/`.

---

## Primary Inputs (Order of Truth)

Use repository sources in this order:

1. Applicable docs contract(s) under `specs/*/contracts/` for the active docs feature
2. Active docs feature specification artefacts under `specs/*/` (`spec.md`, `research.md`, `data-model.md`, `plan.md`)
3. Verified product behaviour from code and tests (for example CSV headers, priorities, outcomes, and consent wording)
4. Existing published docs content under `docs/` to preserve terminology and navigation consistency

If sources conflict, follow the highest item in this list.

---

## Audience and Voice

- Primary audience: non-technical hosted users.
- Secondary audience: administrators reviewing permissions and privacy statements.
- Tone: clear, calm, practical, and honest.
- Style: short sentences, active voice, concrete actions.
- Language: UK English only (for example, "organisation", "behaviour", "colour").

Avoid internal framing such as architecture details, implementation classes, deployment internals, or incident-runbook language.

---

## Content Rules

For every page, ensure:

- Plain-English explanations without jargon.
- Steps are task-oriented and ordered.
- The user can identify what to do next without needing source code.
- Claims are grounded in verified behaviour, not guesses.
- Example CSV data is illustrative and safe for public publication.

Never include:

- Secrets, credentials, tenant-sensitive values, or internal-only troubleshooting notes.
- Instructions that belong in `docs-internal/`.
- Statements that imply fixed deployment timing guarantees when only automatic publication is required.

---

## Required Coverage by Page

When creating or reviewing public docs pages, validate the obligations defined in the active docs contract. For the current site structure, this typically includes:

- `/`: purpose, audience, links to core guides.
- `/getting-started`: hosted prerequisites, hosted app access, sign-in and consent expectations.
- `/csv-format`: required `Task Name`, accepted fields, allowed priority values, safe examples, common mistakes.
- `/import-workflow`: ordered workflow steps, preview versus execution, created/reused/skipped outcomes, manual goal follow-up note.
- `/troubleshooting`: sign-in/consent, no groups found, CSV validation, duplicates, temporary API/throttling issues.
- `/faq`: duplicates, existing plan import, goal handling, supported audience, data storage.
- `/privacy-and-security`: Graph read/write scope at high level, no imported Planner/task data persistence, limited operational logs/telemetry, credential handling statement, delegated permissions summary.
- `/self-hosted` (secondary): clearly labelled as secondary and not part of the hosted primary path.

---

## Writing Workflow

1. Confirm page goal, audience, and required sections.
2. Draft a concise outline with user tasks first.
3. Write content in plain UK English.
4. Cross-check all behaviour statements against repository sources.
5. Run a final quality pass:
   - remove jargon
   - simplify long sentences
   - verify links and navigation
   - confirm public-only content boundary

---

## Done Criteria

Treat a docs update as complete only when all are true:

- Contract obligations for the page are satisfied.
- Wording is understandable to non-technical users.
- UK English is consistent.
- Links are valid and navigation is not broken.
- Content contains no internal-only details.
