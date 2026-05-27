# docs/ — Public-facing documentation (GitHub Pages)

## Start Here

For end-user guidance, start at the public docs landing page:

- https://docs.importplanner.app/

The main user routes live in this folder as page files, including:

- `index.md`
- `getting-started.md`
- `csv-format.md`
- `import-workflow.md`
- `troubleshooting.md`
- `faq.md`
- `privacy-and-security.md`
- `self-hosted.md` (secondary guidance)

This directory is reserved for public-facing end-user documentation that will be served
via GitHub Pages. Do not add internal developer-only guidance, implementation notes,
or operational runbooks to this folder — such content will be published publicly.

Developer and implementation guidance should live in a non-public or clearly internal
folder. Recommended locations for internal developer docs:

- `docs-internal/` — internal developer guidance and implementation notes (recommended)
- `developer-docs/` or `docs-dev/` — alternative internal locations

For automation and AI agents: treat `docs/` as public content. Avoid placing internal-only
instructions or credentials here. If you add project-level policies that affect agents,
update `.github/copilot-instructions.md` or `AGENTS.md` as appropriate.

If you want to contribute to the codebase or debug the application locally, use the internal
developer guidance in `docs-internal/` instead of adding contributor-only setup detail here.

## Deployment Modes

Import To Planner supports two operational modes:

- Self-hosted single-tenant mode for one organisation-owned tenant.
- Hosted shared multi-tenant mode for supported work or school tenants.

Operational setup differences:

- Self-hosted mode keeps tenant authority fixed and does not require hosted metadata storage.
- Hosted mode uses a shared work-or-school sign-in authority and requires tenant-scoped
	operational metadata persistence and data-protection key persistence.
- Hosted rollout readiness, guardrails, and evidence requirements are tracked in
	`docs-internal/aspire-production-readiness.md`.

For people who simply want to run the app for their own organisation, the recommended starting
point is self-hosted single-tenant mode. Hosted shared multi-tenant mode is primarily for a shared
service deployment that admits multiple supported work or school tenants.

## Hosted Consent and Data Handling

For hosted mode, end users may see either delegated-consent prompts or administrator-consent
guidance depending on tenant policy. Operators should ensure support guidance includes:

- A clear administrator-consent path when tenant policy blocks user consent.
- UK-English wording for consent outcomes and recovery actions.
- Privacy-safe diagnostics that identify consent outcome and failure category without exposing
	tokens, secrets, or CSV import payload content.

Hosted retained data scope is intentionally minimal and tenant-scoped only:

- Consent status and related support diagnostics.
- Tenant-scoped configuration required for hosted operation.
- No per-user usage history, CSV payload retention, or import report history.
