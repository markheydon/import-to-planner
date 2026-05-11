# docs/ — Public-facing documentation (GitHub Pages)

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
