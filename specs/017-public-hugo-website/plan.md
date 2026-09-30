# Implementation Plan: Public Product Website, Documentation Layout, and Release Versioning

**Branch**: `017-public-hugo-website` | **Date**: 2026-09-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/017-public-hugo-website/spec.md`

## Summary

Migrate end-user documentation from Jekyll under `docs/` to a Hugo + Hextra public product
site under `website/`, aligned with the Solo Dev Board reference (`scripts/invoke-hugo-site.sh`,
tag-only GitHub Pages deploy, release version labelling). Relocate internal engineering
documentation from `docs-internal/` to `docs/` without publishing it. Add terms, expanded
privacy, and GitHub Issues support pages; link them from the hosted app. Introduce
operator-gated demo mode with a fully synthetic import journey for safe screenshots. Add
root `CHANGELOG.md`, PR Hugo validation, and release runbook steps including director terms
publication record before the first public site tag.

## Technical Context

**Language/Version**: Hugo extended (pinned in CI to match reference, currently 0.164.x line);
Go modules for Hextra; Markdown content; repository application stack remains .NET 10 / C# 14  
**Primary Dependencies**: Hextra via Hugo modules; GitHub Actions (`configure-pages`,
`upload-pages-artifact`, `deploy-pages`); container images `hugomods/hugo` and nginx alpine for
local scripts; existing Blazor/MudBlazor web host for app links and demo toggle  
**Storage**: Static site output in `website/public/`; demo fixtures in-memory only; no new
persistent demo store  
**Testing**: Hugo build in CI on `website/**` changes; xUnit tests for demo authorisation and
adapter non-invocation; manual route/content review per `quickstart.md`; optional Playwright
screenshot capture out of scope unless added in tasks  
**Target Platform**: GitHub Pages at `docs.importplanner.app`; local container preview;
Blazor Server/WASM host for app behaviour  
**Project Type**: Documentation site + repository layout migration + bounded web/application
feature (demo mode) inside existing Clean Architecture solution  
**Performance Goals**: Static pages load without Blazor boot; mobile-readable layouts (375 px+);
local preview setup under 15 minutes (SC-003)  
**Constraints**: UK English; tag-only live publish; director-approved terms publication
record before first site tag; demo mode off at sign-in, off on sign-out, no Graph/upload
during demo; self-hosted
parity for demo policy; retire Jekyll publish-on-main  
**Scale/Scope**: ~10 public routes + billing stub; migrate 9 legacy user pages; move
`docs-internal/` tree; 3 new contracts; app footer/help links; demo mode across import workflow

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Gate | Assessment |
| --- | --- |
| I. Dependency Rule | Pass with demo implementation discipline. Demo policy interfaces live in Application; Graph/upload adapters remain in Infrastructure/Web; Domain stays free of Hugo or demo UI types. |
| II. Technology-Neutral Core | Pass. Import policy unchanged; demo adds a session mode flag and fixture-driven responses, not Graph shapes in Domain. |
| III. Explicit Boundaries | Pass. Demo toggle and footer links are Web/presenter concerns; authorisation decision in Application service; synthetic data provider behind interfaces. |
| IV. Replaceability | Pass. Hugo/Pages and demo fixtures are outer-layer delivery choices. |
| V. Traceability | Pass. Work maps to FR-001–FR-016 and user stories in spec 017. |
| VI. Testable Behaviour | Pass. CI Hugo build; unit/integration tests for demo gating and no-Graph invariant; quickstart scenarios. |
| VII. Explicit Errors | Pass. Demo misconfiguration and unauthorised toggle attempts fail closed (hidden UI / inactive demo). |
| VIII. Security by Design | Pass. Allowlist + deployment flag; no tenant-admin bypass; synthetic-only data during demo; no secrets in static site. |
| IX. Quality Evidence | Pass when PRs include tests and CI checks per engineering policies. |
| X. Self-Hosted Viability | Pass. Same demo configuration model on self-hosted; public docs base URL configurable; no hosted-only doc publish path. |

**Post-design re-check**: No unjustified gate failures. Demo mode is the main architectural
surface; it MUST use adapter substitution or gateway decorators registered in composition root,
not conditional Graph calls scattered in Razor code-behind.

## Project Structure

### Documentation (this feature)

```text
specs/017-public-hugo-website/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── public-site-contract.md
│   ├── app-external-links-contract.md
│   └── demo-mode-contract.md
└── tasks.md                    # Phase 2 — implementation tasks (complete)
```

### Source Code (repository root)

```text
website/                              # NEW — Hugo + Hextra public product site
├── hugo.yaml
├── go.mod / go.sum
├── content/                          # Migrated end-user pages + terms/support/billing stub
├── layouts/                          # Footer/release badge partials (reference solo-dev-board)
├── static/CNAME                      # docs.importplanner.app
└── public/                           # Build output (gitignored)

scripts/
├── invoke-hugo-site.sh               # NEW — port from solo-dev-board
└── Invoke-HugoSite.ps1               # NEW — optional Windows parity

.github/workflows/
├── hugo-build.yml                    # NEW — reusable build (validate + deploy artefact)
├── hugo-validate.yml                 # NEW — PR path filter on website/**
└── hugo-deploy.yml                   # NEW — v* tags + workflow_dispatch

docs/                                 # Engineering docs ONLY after cutover
├── README.md
├── engineering-policies.md           # migrated from docs-internal/
├── microsoft-graph-guidelines.md
├── release-runbook.md                # NEW — tags, Pages, terms publication record
└── …                                 # other former docs-internal content

CHANGELOG.md                          # NEW — Keep a Changelog at repo root

docs/ (legacy end-user Jekyll)        # REMOVED at cutover — index, guides, _config.yml, CNAME

src/ImportToPlanner.Application/      # Demo session policy, interfaces for synthetic journey
src/ImportToPlanner.Web/              # Demo toggle UI, footer/help links, DI registration
src/ImportToPlanner.Infrastructure.Graph/  # Demo-off only; no Graph when demo active
tests/                                # Demo mode + architecture compliance tests

README.md, AGENTS.md, CONTRIBUTING.md, .github/skills/end-user-docs/SKILL.md
                                      # UPDATE paths: website/ public, docs/ internal
```

**Structure Decision**: Split public (`website/`) and internal (`docs/`) trees; port proven
Hugo automation from Solo Dev Board with a slimmer workflow set (no MinVer matrix, no mandatory
Playwright docs pipeline). Implement demo mode inside the existing solution layers rather than
a separate demo app.

Until **US6** executes, the pre-implementation repository may still contain `docs-internal/`
and legacy Jekyll end-user content under `docs/`; that interim layout is expected and is
retired during cutover tasks **T062**–**T063**.

## Complexity Tracking

No constitution violations requiring justification table entries. Demo mode adds configuration
and adapter routing but stays within replaceable outer layers and explicit Application seams.

---

## Phase 0: Outline & Research

Complete — see [research.md](research.md).

Resolved decisions include Hugo/Hextra under `website/`, stable URL preservation, container
invoke scripts, tag-only Pages deploy, release version params, `docs-internal/` → `docs/`
migration, legal/support pages, billing IA stub, demo mode architecture, app external links,
CHANGELOG/runbook, and superseding the spec 007 publishing contract while keeping content
obligations.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design & Contracts

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and
[contracts/](contracts/).

### Design highlights

**Public site**

- Initialise `website/` from Solo Dev Board patterns (Hextra module, UK locale, search, edit
  links pointing to this repo’s `website/content`).
- Migrate Markdown from legacy `docs/*.md` into Hugo content; add terms, support, expanded
  privacy, and credits/billing stub.
- Custom partials display `params.releaseVersion` (`unreleased` vs tag).
- Retire Jekyll `_config.yml` and Pages-from-`docs/` automation.

**Repository layout**

- Move `docs-internal/**` → `docs/**`; update all references in AGENTS.md, skills, specs, and
  templates.
- Add root `CHANGELOG.md` and `docs/release-runbook.md` with terms publication record gate.

**CI/CD**

- `hugo-validate.yml` on PRs touching `website/**`.
- `hugo-deploy.yml` on `v*` only; inject tag into `HUGO_PARAMS_RELEASEVERSION`.
- Remove publish-on-main behaviour from spec 007 era.

**Application — demo mode**

- Configuration: allowlist + `DemoControlsEnabled` (default false in production).
- Session: off at sign-in; toggled by allowlisted operators; forced off on sign-out.
- While active: synthetic fixtures drive import workflow; register no-op or fake adapters so
  Graph and real CSV processing are unreachable for import steps.
- Persistent UI indicator when demo is active.

**Application — external links**

- Configurable `DocsBaseUrl`; footer or help component linking to terms, privacy, support, docs
  home per `app-external-links-contract.md`.

### Architecture impact statement

- **Dependency direction**: Domain unchanged. Application gains demo session + authorisation
  ports. Web gains UI toggle and links. Infrastructure Graph adapters honour demo flag at
  boundary.
- **Boundary changes**: New Application requests/responses for demo state; synthetic read models
  for groups/plans/tasks/CSV preview/execution report.
- **Adapter responsibilities**: Web maps toggle to Application commands; Infrastructure skips
  outbound Graph when demo active; Hugo site has no app runtime coupling.
- **Traceability**: Each implementation task in `/speckit-tasks` should cite FR ids and
  contract sections.
- **Testability**: Hugo CI compile; demo mode tests at Application and adapter spy boundaries;
  optional E2E on staging with allowlisted account.
- **Error handling**: Unauthorised demo activation ignored; misconfigured allowlist logs
  diagnostics only (no user-facing stack traces).
- **Security trust boundaries**: Demo allowlist **values** are deployment configuration
  (environment variables, user secrets, or host-specific settings), not committed to source
  control; repository `appsettings` carry empty templates only. Demo must not expose production
  data paths.
- **Self-hosted**: Document configuration keys in `docs/`; same demo and docs URL settings as
  hosted.
- **Constitution amendment**: Updating `.specify/memory/constitution.md` delivery-path references
  (`docs-internal/` → `docs/`) is an explicit **US6** deliverable via task **T069** (maintainer
  approval on that file at implement time; not edited during spec/plan remediation).

---

## Phase 2: Implementation Tasks

Complete — see [tasks.md](tasks.md) (**77** tasks: setup, foundational CI/CD, user stories US1–US6,
and polish including SC-001 contract review before the first public site `v*` tag).
