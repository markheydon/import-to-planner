---
description: "Task list for Public Product Website, Documentation Layout, and Release Versioning"
---

# Tasks: Public Product Website, Documentation Layout, and Release Versioning

**Input**: Design documents from `/specs/017-public-hugo-website/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/

**Tests**: Demo mode automated tests are required per `contracts/demo-mode-contract.md` and plan.md. Hugo validation is CI-only unless extended in polish.

**Organization**: Tasks grouped by user story (P1–P6) for independent delivery; setup and foundational phases unblock all stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US6) for story phases only

## Path Conventions

- Public site: `website/`
- Internal engineering docs: `docs/` (after cutover from `docs-internal/`)
- Application: `src/ImportToPlanner.Application/`, `src/ImportToPlanner.Web/`, `src/ImportToPlanner.Infrastructure.Graph/`
- Tests: `tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialise Hugo product site skeleton (`website/`, **FR-001**) and local container scripts (**FR-008**).

- [X] T001 Create `website/` tree per plan.md (`hugo.yaml`, `content/`, `layouts/`, `static/`)
- [X] T002 Initialise Hugo modules in `website/go.mod` and `website/go.sum` with Hextra (`github.com/imfing/hextra`) aligned to Solo Dev Board reference
- [X] T003 Configure `website/hugo.yaml` base URL `https://docs.importplanner.app/`, UK locale, search, and edit links to `website/content` in this repository
- [X] T004 Add `website/.gitignore` entry for `public/` build output
- [X] T005 [P] Port container workflow to `scripts/invoke-hugo-site.sh` (build, serve, preview) using `hugomods/hugo` image per research.md
- [X] T006 [P] Add Windows parity script `scripts/Invoke-HugoSite.ps1` matching `scripts/invoke-hugo-site.sh` commands

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: CI/CD, release labelling, and repository layout prerequisites that MUST complete before story work converges on cutover.

**⚠️ CRITICAL**: Hugo CI and version partials block US3/US4 validation; do not retire Jekyll from `docs/` until US1 content exists under `website/`.

- [X] T007 Create reusable workflow `.github/workflows/hugo-build.yml` (extended Hugo, module fetch, output `website/public/`)
- [X] T008 Create PR workflow `.github/workflows/hugo-validate.yml` path-filtered on `website/**` calling hugo-build (FR-009, SC-006)
- [X] T009 Create deploy workflow `.github/workflows/hugo-deploy.yml` on `v*` tags and `workflow_dispatch` using `configure-pages`, `upload-pages-artifact`, `deploy-pages` (FR-010)
- [X] T010 Wire tagged deploy job in `.github/workflows/hugo-deploy.yml` to set `HUGO_PARAMS_RELEASEVERSION` from `github.ref_name`; non-tag builds use `unreleased` (FR-011)
- [X] T011 [P] Add `website/static/CNAME` with value `docs.importplanner.app` (FR-003)
- [X] T012 [P] Set default `params.releaseVersion: unreleased` in `website/hugo.yaml` per `SitePublicationSettings` in data-model.md
- [X] T013 [P] Add `website/layouts/_partials/custom/footer.html` (and optional `release-badge.html`) displaying `SitePublicationSettings.ReleaseVersionLabel` / Hugo `params.releaseVersion`
- [X] T014 Audit and remove or disable legacy Jekyll/GitHub Pages **publish-on-main automation** targeting `docs/` only (FR-014), including repository **GitHub Pages** source branch/folder settings if still pointed at `docs/`; legacy end-user **content** under `docs/` is removed in US6 T063 after US1 migration — do not conflate automation retirement with content cutover; document replacement in `docs/release-runbook.md` stub if workflow removed before runbook is complete

**Checkpoint**: `hugo-validate` passes on an empty/minimal `website/` build; deploy workflow ready for tag dry-run.

---

## Phase 3: User Story 1 - Visitor Uses a Trustworthy Public Product Site (Priority: P1) 🎯 MVP

**Goal**: Published-quality public guides at stable URLs with primary navigation and 007 content obligations (FR-001, FR-004, public-site-contract routes).

**Independent Test**: Locally preview `website/` and confirm mandatory routes, primary nav order, UK English, and workflow/CSV copy aligned with current app behaviour per quickstart.md §2.

### Implementation for User Story 1

- [X] T015 [P] [US1] Migrate `docs/index.md` to `website/content/_index.md` preserving `/` permalink (public-site-contract.md)
- [X] T016 [P] [US1] Migrate `docs/getting-started.md` to `website/content/getting-started.md` preserving `/getting-started`
- [X] T017 [P] [US1] Migrate `docs/csv-format.md` to `website/content/csv-format.md` preserving `/csv-format`; examples MUST use synthetic CSV/tasks only (data-model `PublicSiteSection` validation)
- [X] T018 [P] [US1] Migrate `docs/import-workflow.md` to `website/content/import-workflow.md` preserving `/import-workflow`; align steps with current import journey (validation, preview, execution, report)
- [X] T019 [P] [US1] Migrate `docs/troubleshooting.md` to `website/content/troubleshooting.md` preserving `/troubleshooting`
- [X] T020 [P] [US1] Migrate `docs/faq.md` to `website/content/faq.md` preserving `/faq`
- [X] T021 [P] [US1] Migrate `docs/privacy-and-security.md` to `website/content/privacy-and-security.md` preserving `/privacy-and-security` (baseline before US2 expansion)
- [X] T022 [P] [US1] Migrate `docs/self-hosted.md` to `website/content/self-hosted.md` preserving `/self-hosted`; copy MUST NOT instruct hosted-only support as if MHCG operates self-hosted deployments
- [X] T023 [US1] Configure Hextra `menu.main` in `website/hugo.yaml` for primary nav order: Home → Getting started → CSV format → Import workflow → Troubleshooting → FAQ → Privacy and security (public-site-contract.md)
- [X] T024 [US1] Configure secondary nav entries for self-hosted (and placeholder weight for billing stub added in US2) in `website/hugo.yaml`
- [X] T025 [US1] Add landing cross-links in `website/content/_index.md` to legal/support destinations per public-site-contract.md footer expectations
- [X] T026 [US1] Update repository root `README.md` with prominent link to `https://docs.importplanner.app` (public-site-contract.md README discoverability)
- [X] T027 [US1] Run `./scripts/invoke-hugo-site.sh build` and fix any Hugo/content errors until `website/public/index.html` exists

**Checkpoint**: US1 routes render locally with correct nav; no engineering content under `website/content/`.

---

## Phase 4: User Story 2 - Hosted User Finds Terms, Privacy, and Support (Priority: P2)

**Goal**: Production-ready terms path with director publication record, expanded privacy, GitHub Issues support page, billing IA stub, and app links (FR-005, FR-006, FR-007, SC-002).

**Independent Test**: From local preview and app chrome, reach terms, privacy, and support within two clicks; support page links to repository GitHub Issues with v1.1 deferral wording.

### Implementation for User Story 2

- [X] T028 [P] [US2] Add production-ready terms page `website/content/terms.md` at `/terms` (MUST NOT ship as “coming soon” on first `v*` tag; blocked until T035 publication record exists)
- [X] T029 [P] [US2] Expand privacy copy in `website/content/privacy-and-security.md` per issue #134 / FR-005 beyond 007 baseline
- [X] T030 [P] [US2] Add support page `website/content/support.md` at `/support` linking to this repository GitHub Issues with UK English scope (bugs, documentation corrections, product feedback) and v1.1 dedicated-support deferral without promising dates
- [X] T031 [P] [US2] Add credits/billing IA stub `website/content/credits-and-billing.md` at `/credits-and-billing` (MUST NOT invent pricing, Stripe, or ledger behaviour; MAY reference future billing docs)
- [X] T032 [US2] Link terms and support from `website/layouts/_partials/custom/footer.html` and landing page per public-site-contract.md
- [X] T033 [US2] Add `DocsBaseUrl` configuration (default `https://docs.importplanner.app`) in `src/ImportToPlanner.Web/appsettings.json` and environment-specific overrides as needed per `app-external-links-contract.md`
- [X] T034 [US2] Implement footer or persistent help component in `src/ImportToPlanner.Web/Components/Layout/MainLayout.razor` and `MainLayout.razor.cs` linking to `{DocsBaseUrl}/`, `/terms`, `/privacy-and-security`, `/support` with `https` in production
- [X] T035 [US2] Add director terms publication **record** in `docs/release-runbook.md` with fields: approval date, approver role, and terms version reference (`LegalAndSupportBundle.TermsPublicationRecord` in data-model.md; satisfies **SC-007** when filled)
- [X] T036 [US2] Audit in-app troubleshooting/FAQ strings for generic “contact your support team” without destination; update to link published support path per `app-external-links-contract.md`

**Checkpoint**: Legal bundle URLs consistent between site and app configuration; terms publish gated on current T035 publication record in `docs/release-runbook.md`.

---

## Phase 5: User Story 3 - Contributor Previews the Public Site Locally (Priority: P3)

**Goal**: Documented container build/serve/preview under 15 minutes for first-time contributors (FR-008, SC-003). Final contributor path lives in `docs/` only after **US6** (**T062**); until then interim pointers are acceptable per **T037**.

**Depends on**: Phase 1 scripts; **T037** blocked on US6 for permanent `docs/developer-quickstart.md` path (interim README acceptable).

**Independent Test**: Follow documented commands on a machine with Docker or Podman only; browse site locally with `unreleased` label.

### Implementation for User Story 3

- [X] T037 [US3] Document `build`, `serve`, and `preview` usage in `docs/developer-quickstart.md` after US6 completes (`docs-internal/` → `docs/` per T062); until then use interim `README.md` pointing to `scripts/invoke-hugo-site.sh` (depends on US6 for final `docs/` path)
- [X] T038 [US3] State container engine requirement (Docker or Podman) and optional native Hugo install de-prioritised in `docs/` contributor guidance per spec edge cases
- [X] T039 [US3] Verify default preview port (`8080`) and serve port (`1313`) match script behaviour and quickstart.md §1
- [X] T040 [US3] Add troubleshooting row for missing container engine to `specs/017-public-hugo-website/quickstart.md` Failure triage if script errors are opaque

**Checkpoint**: SC-003 script path documented and verified on clean contributor machine.

---

## Phase 6: User Story 4 - Stakeholder Ships a Versioned Public Release (Priority: P4)

**Goal**: Tag-only live publish, visible SemVer on production, root changelog, and maintainer runbook (FR-010, FR-011, FR-012, SC-004).

**Independent Test**: Merge to main without tag leaves live site unchanged; tag deploy shows tag in footer; `CHANGELOG.md` and runbook describe `vX.Y.Z` process.

### Implementation for User Story 4

- [X] T041 [US4] Add root `CHANGELOG.md` in Keep a Changelog format with initial Unreleased section per data-model `ChangeLogEntry`
- [X] T042 [US4] Complete `docs/release-runbook.md` with SemVer tagging, Pages deploy verification, `HUGO_PARAMS_RELEASEVERSION` check, and terms publication record before first public site tag; include **operational terms workflow** (review `website/content/terms.md` → update publication record per T035 → tag); define **SC-004** verification as timed check from successful `hugo-deploy` workflow completion for the tag until live site shows new version label (target within one hour); add cutover checklist reinforcing T014 automation vs T063 content removal; cross-link `specs/017-public-hugo-website/quickstart.md` §1 for interim **SC-003** evidence until T037 lands in `docs/developer-quickstart.md`
- [X] T043 [US4] Confirm `.github/workflows/hugo-deploy.yml` does not run on ordinary `push` to `main` (tag-only or manual dispatch only)
- [X] T044 [US4] Document PR/non-tag build labelling (`unreleased`) in `docs/release-runbook.md` matching quickstart.md §5
- [X] T045 [US4] Add maintainer steps to update `CHANGELOG.md` when cutting `vX.Y.Z` releases affecting app and/or public site

**Checkpoint**: Maintainer can follow runbook from tag creation through visible version on Pages.

---

## Phase 7: User Story 5 - Author Captures Safe Documentation Screenshots (Priority: P5)

**Goal**: Operator-gated demo mode with fully synthetic import journey and no Graph/upload during demo (FR-015, FR-016, demo-mode-contract.md).

**Independent Test**: Allowlisted operator on non-production with `DemoControlsEnabled` walks import workflow; logs/tests show no Graph/upload; sign-out clears demo.

### Tests for User Story 5

- [X] T046 [P] [US5] Add unit tests for allowlist match vs non-match (toggle hidden) in `tests/` Application layer per demo-mode-contract.md Testing obligations
- [X] T047 [P] [US5] Add tests for demo off at sign-in and forced off on sign-out in `tests/`
- [X] T048 [P] [US5] Add test that demo persists across navigation until toggle off or sign-out in `tests/`
- [X] T049 [P] [US5] Add adapter spy tests proving Graph gateway and upload/parser dependencies are not invoked for import steps when demo is active in `tests/` (Infrastructure.Graph boundary)

### Implementation for User Story 5

- [X] T050 [US5] Define `IDemoModeSession` / demo state port and commands in `src/ImportToPlanner.Application/` (`DemoModeSession.IsActive` defaults false at sign-in; forced false on sign-out per data-model.md)
- [X] T051 [US5] Implement `DemoModeDeploymentPolicy` options (`OperatorAllowlist`, `DemoControlsEnabled` default false) in `src/ImportToPlanner.Web/` configuration binding matching `DemoMode:OperatorAllowlist` and `DemoMode:DemoControlsEnabled` contract names; committed `appsettings.json` (and Development template) MUST ship empty allowlist and safe defaults only — real Entra object IDs and/or operator UPNs MUST load from environment variables, .NET user secrets, or deployment-specific configuration, not from source control (per plan.md security note)
- [X] T052 [US5] Implement authorisation service in Application verifying allowlist (Entra object ID and/or normalised UPN); tenant administrator roles MUST NOT grant access
- [X] T053 [US5] Register in-memory `DemoDataset` fixtures (synthetic groups, plans, tasks, CSV sample, report) in `src/ImportToPlanner.Application/` or dedicated fixtures project folder
- [X] T054 [US5] Register demo-aware adapter routing in `src/ImportToPlanner.Web/Program.cs` composition root: synthetic read models when `DemoModeSession.IsActive`; no live `IPlannerGateway`/Graph for import steps
- [X] T055 [US5] Guard `src/ImportToPlanner.Infrastructure.Graph/` adapters to skip outbound Graph when demo is active (fail closed at boundary, not in Razor)
- [X] T056 [US5] Stub or no-op real CSV upload processing for import steps while demo active in Web/Application import workflow path
- [X] T057 [US5] Add operator demo toggle UI (visible only when allowlisted AND `DemoControlsEnabled`) in `src/ImportToPlanner.Web/Components/Layout/MainLayout.razor` with clear demonstration labelling
- [X] T058 [US5] Add persistent indicator when demo is active warning data is synthetic and Graph is not used for import steps
- [X] T059 [US5] Clear demo session on sign-out in authentication pipeline (`src/ImportToPlanner.Web/Features/Authentication/` or session handler)
- [X] T060 [US5] Document demo enablement (allowlist deployment pattern — empty committed template, operators configure IDs/UPNs via secrets or environment; policy, environments, privacy guarantees) in `docs/` engineering guidance per demo-mode-contract.md Documentation obligations
- [X] T061 [US5] Update `website/content/import-workflow.md` with screenshots captured under demo mode when available (FR-016 SHOULD)

**Checkpoint**: SC-005 privacy walkthrough possible; any Graph/upload during active demo is treated as defect.

---

## Phase 8: User Story 6 - Contributor Finds Engineering Guidance in One Place (Priority: P6)

**Goal**: Single internal `docs/` tree, retired Jekyll public stack, updated agent/readme/skills paths (FR-002, FR-013, FR-014).

**Independent Test**: Links from `README.md` and `AGENTS.md` resolve: end-user → `website/` or published URL; engineering → `docs/`; `docs/_config.yml` and `docs/CNAME` absent after cutover.

### Implementation for User Story 6

- [X] T062 [US6] Move all files from `docs-internal/` to `docs/` preserving relative structure; resolve naming conflicts with legacy Jekyll user files by completing US1 migration first
- [X] T063 [US6] Remove legacy end-user Jekyll artefacts from `docs/` (`docs/_config.yml`, `docs/CNAME`, migrated `*.md` user pages) after equivalent `website/content/` pages exist
- [X] T064 [P] [US6] Update `AGENTS.md` references from `docs-internal/` to `docs/` for Microsoft Graph and engineering policies (FR-013)
- [X] T065 [P] [US6] Update `README.md` and `CONTRIBUTING.md` public vs internal documentation paths (FR-013)
- [X] T066 [P] [US6] Update `.github/pull_request_template.md` documentation path guidance
- [X] T067 [P] [US6] Update `.github/skills/end-user-docs/SKILL.md` to target `website/` and tag-only publish assumptions (FR-014)
- [X] T068 [P] [US6] Update `.github/instructions/blazor-csharp.instructions.md` and other instruction files still pointing at `docs-internal/`
- [X] T069 [US6] Search repository for remaining `docs-internal/` string references and update to `docs/` (`rg docs-internal` cleanup), including `.specify/memory/constitution.md` Delivery Workflow path to `docs/engineering-policies.md` (constitution amendment per governance section — maintainer approval on that file change)
- [X] T070 [US6] Add `docs/README.md` clarifying engineering-only scope and exclusion from Hugo artefact (FR-002)
- [X] T071 [US6] Note supersession of `specs/007-end-user-docs-site/contracts/docs-site-contract.md` by `specs/017-public-hugo-website/contracts/public-site-contract.md` in `docs/release-runbook.md` or engineering README

**Checkpoint**: No dual public publish path; agents and contributors have one canonical internal docs folder.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Validation, formatting, and end-to-end quickstart execution.

- [X] T072 [P] Run full `specs/017-public-hugo-website/quickstart.md` scenarios on a clean contributor machine and record gaps fixed in tasks or docs; record **SC-002** pass/fail (terms, privacy, support reachable within two clicks from app home/footer and public site per quickstart §8); record **SC-003** timing for §1 container preview; after US5, run quickstart.md §9 **SC-005** screenshot privacy checklist before publishing workflow images
- [X] T073 [P] Verify built `website/public/` does not contain engineering-only strings (`engineering-policies`, `AppHost`) per quickstart.md §3
- [X] T074 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` after application changes
- [X] T075 Run affected `dotnet test` projects covering demo mode and existing regression suites per `tests/README.md` (required for PRs touching application/demo code; Hugo-only changes rely on CI `hugo-validate` per plan constitution IX — no `dotnet test` gate for website-only PRs)
- [X] T076 Confirm GitHub Pages repository settings document custom domain `docs.importplanner.app` aligned with `website/static/CNAME`
- [X] T077 Run structured **SC-001** review: score pass/fail per mandatory obligation in `contracts/public-site-contract.md` (007 carry-forward, routes, navigation, **publication/version obligations** including tag-only live publish and explicit non-release labelling) and, where 017 defers, `specs/007-end-user-docs-site/contracts/docs-site-contract.md` page content obligations; record results in release checklist or runbook; require ≥90% mandatory items pass before first public site `v*` tag

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → user story phases may overlap only where noted below
- **US1** should complete Hugo content migration before **US6** deletes legacy `docs/*.md`
- **US2** terms first public tag depends on **T035** publication record and **T028** production-ready copy
- **US4** deploy workflows depend on **Phase 2** CI/CD tasks
- **US5** application work is largely independent of Hugo but **T061** screenshots depend on **US5** UI
- **Polish** after desired stories complete

### User Story Dependencies

| Story | Depends on | Independent test focus |
| --- | --- | --- |
| US1 (P1) | Phase 1–2 | Routes, nav, migrated guides, UK English |
| US2 (P2) | US1 landing/footer shell; T035 before first `v*` terms | Legal bundle + app links |
| US3 (P3) | Phase 1 scripts; Phase 2 optional for CI mention | Local container preview |
| US4 (P4) | Phase 2 workflows | Tag-only publish + changelog/runbook |
| US5 (P5) | Application foundation only | Synthetic journey + tests |
| US6 (P6) | US1 content in `website/` before Jekyll removal | Path updates + single internal `docs/` |

### Parallel Opportunities

- **Phase 1**: T005 and T006 in parallel; T002–T004 after T001
- **Phase 2**: T011, T012, T013 in parallel after T007 baseline
- **US1**: T015–T022 page migrations in parallel
- **US2**: T028–T031 new pages in parallel
- **US5**: T046–T049 tests can be authored alongside T050–T052 if interfaces stubbed
- **US6**: T064–T068 documentation updates in parallel

### Parallel Example: User Story 1

```bash
# Migrate legacy Markdown in parallel (separate files):
# T015–T022 website/content/*.md

# Then sequential nav and build:
# T023–T027
```

### Parallel Example: User Story 5

```bash
# Tests T046–T049 in parallel once demo ports exist
# Implementation T050–T056 before UI T057–T058
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1)
3. **STOP and VALIDATE**: quickstart.md §1–§2 locally
4. Do not cut over `docs/` (US6) or publish tag until US2 terms publication gate satisfied

### Incremental Delivery

1. Setup + Foundational → CI green on `website/**`
2. US1 → public guides MVP locally
3. US2 → legal/support + app links
4. US3 + US4 → contributor preview docs + release discipline
5. US5 → demo screenshots for workflow guide
6. US6 → internal docs consolidation and Jekyll retirement
7. Polish → quickstart full pass

### Suggested MVP Scope

**User Story 1 (P1)** plus **Phase 1–2** delivers a credible public site preview; production tag requires **US2** terms publication record and **US4** deploy verification at minimum.

---

## Notes

- Task IDs are sequential; adjust only when inserting tasks during implementation
- `[P]` tasks must not edit the same file concurrently
- Demo mode: Graph or real upload during active demo is a **defect** (demo-mode-contract.md)
- First public `v*` site tag MUST NOT ship until director terms publication record is current in `docs/` per **SC-007** (terms content obligations remain in **FR-005**)
- **FR-016** / **T061** (workflow screenshots under demo mode) are not required for the first public site `v*` tag; schedule after **US5** when demo capture is available
- Total tasks: **77** (US1: 13, US2: 9, US3: 4, US4: 5, US5: 16 incl. 4 tests, US6: 10, Setup: 6, Foundational: 8, Polish: 6)

## Phase 10: Convergence

- [X] T078 Complete **SC-001** mandatory obligation pass/fail table in `docs/release-runbook.md` (score each item in `contracts/public-site-contract.md` and deferred 007 obligations; confirm ≥90% pass before first `v*` tag) per **SC-001** / **T077** (partial)
- [X] T079 Record **SC-002** and **SC-003** verification evidence (two-click legal/support paths from app; container preview timing) in `docs/release-runbook.md` or `specs/017-public-hugo-website/quickstart.md` per **T072** (partial)
- [X] T080 Update `docs/release-runbook.md` contributor preview section to cite `docs/developer-quickstart.md` § Public Hugo site as the canonical **SC-003** path (remove stale interim-only wording) per **T042** / **US3** (partial)
- [X] T081 Remove obsolete `docs-internal/` path from `.github/skills/repo-readme-generator/SKILL.md` repository layout guidance per **FR-013** / **T069** (partial)
- [X] T082 Add a visible link to the [Support](./support) page or GitHub Issues from `website/content/faq.md` per **FR-006** (partial)
- [X] T083 Add import-workflow screenshots captured under demo mode to `website/content/import-workflow.md` when ready per **FR-016** (withdrawn: identical step images removed; distinct journey capture deferred to GitHub #164)

## Phase 11: Convergence

- [X] T084 Add demonstration-mode workflow screenshots under `website/static/` and embed them in `website/content/import-workflow.md` (replace the placeholder in **Workflow illustrations**); capture on a non-production deployment with an allowlisted operator per `docs/demo-mode.md` per **FR-016** (withdrawn pending #164; `scripts/verify-import-workflow-screenshots.sh` rejects identical files)
