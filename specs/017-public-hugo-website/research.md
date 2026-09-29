# Research: Public Product Website, Documentation Layout, and Release Versioning

## Decision 1: Hugo extended + Hextra under `website/`, mirroring Solo Dev Board

- Decision: Scaffold the public product site in `website/` using Hugo extended and the
  Hextra theme via Hugo modules (`github.com/imfing/hextra`), matching the reference
  layout in `markheydon/solo-dev-board` (`website/hugo.yaml`, `go.mod`, content tree,
  custom footer partial for release version).
- Rationale: The feature spec and assumptions already select this stack; the reference
  repository proves GitHub Pages, Hextra navigation, UK English locale, and release-version
  injection via `HUGO_PARAMS_RELEASEVERSION` / `params.releaseVersion`.
- Alternatives considered: Keep Jekyll in `docs/` (rejected — spec mandates cutover);
  adopt another SSG (rejected — duplicates proven reference and increases review cost).

## Decision 2: Preserve legacy public URL paths from spec 007

- Decision: Map migrated pages to the same stable paths the Jekyll site exposed
  (`/`, `/getting-started`, `/csv-format`, `/import-workflow`, `/troubleshooting`,
  `/faq`, `/privacy-and-security`, `/self-hosted`) using Hugo `permalink` settings or
  equivalent content paths so existing links and the 007 route contract remain valid.
- Rationale: FR-003 and SC-001 depend on continuity at `docs.importplanner.app`; changing
  slugs would break bookmarks and README links without user benefit.
- Alternatives considered: Nest all guides under `/docs/` like Solo Dev Board (rejected for
  this product — would break published URLs); redirect-only migration (rejected — adds
  complexity without spec requirement).

## Decision 3: Container-first local preview via `scripts/invoke-hugo-site.sh`

- Decision: Port the Solo Dev Board `invoke-hugo-site.sh` pattern (build / serve / preview
  commands, `hugomods/hugo` container image, optional nginx static preview) to this
  repository, with a PowerShell twin if the repo already uses paired scripts elsewhere.
- Rationale: FR-008 and SC-003 require documented container workflows without mandating
  local Hugo/Go/Sass installs; the reference script is already maintained and tested.
- Alternatives considered: Document native Hugo install only (rejected — spec de-prioritises
  it); devcontainer-only preview (rejected — not the primary supported path in spec).

## Decision 4: Tag-only GitHub Pages deploy with PR compile validation

- Decision: Add reusable `hugo-build.yml` plus `hugo-validate.yml` (path-filtered on
  `website/**`) and `hugo-deploy.yml` triggered on `v*` tags and optional
  `workflow_dispatch`, deploying `website/public` to GitHub Pages. Remove or replace any
  publish-on-main Jekyll automation (007) as part of cutover.
- Rationale: FR-009, FR-010, FR-011, and SC-004 require PR compile gates, tag-only live
  publishes, and visible SemVer on tagged builds; Solo Dev Board encodes the same policy
  (DEC-021 pattern).
- Alternatives considered: Continue Pages from `docs/` on main (rejected — contradicts
  spec); manual-only deploys without CI (rejected — FR-009 requires automated PR checks).

## Decision 5: Release version labelling

- Decision: Default `params.releaseVersion` to `unreleased` in `hugo.yaml`; set
  `HUGO_PARAMS_RELEASEVERSION` to `github.ref_name` on tagged deploy builds and
  `unreleased` (or explicit env default) on PR/local builds. Surface tagged versions in
  footer partial and optional landing shortcode (reference:
  `website/layouts/_partials/custom/footer.html`, `release-badge.html`).
- Rationale: FR-011 and user story 4 require visible SemVer on production and explicit
  non-release labels elsewhere; reference project already implements the pattern.
- Alternatives considered: Embed version only in HTML comments (rejected — not visible to
  users); read version from CHANGELOG at build time (rejected — tag name is authoritative
  for Pages deploys).

## Decision 6: Single cutover — `docs/` becomes internal engineering only

- Decision: After content migration, move `docs-internal/` material into `docs/` (standard
  internal folder), retire Jekyll config and end-user Markdown from `docs/`, and update
  AGENTS.md, skills, README, and CI references from `docs-internal/` to `docs/` for
  engineering guidance. Public end-user copy lives only under `website/content/`.
- Rationale: FR-001, FR-002, FR-013, FR-014, and clarification answers require one public
  stack and no dual publish to the custom domain.
- Alternatives considered: Long-running dual stack (rejected in spec); keep
  `docs-internal/` name alongside `docs/` (rejected — spec standardises on `docs/` for
  engineering).

## Decision 7: New public legal and support pages

- Decision: Add Hugo pages for terms of use, expanded privacy (migrate/enhance existing
  privacy copy), and support (GitHub Issues interim path with v1.1 deferral wording).
  Record counsel sign-off in an engineering runbook under `docs/` before first `v*` site
  tag (FR-005, SC-007).
- Rationale: Issue #134 and clarifications require production-ready terms and a concrete
  support path distinct from generic “contact support” copy.
- Alternatives considered: Host terms only in GitHub wiki (rejected — breaks unified public
  site); email support in v1.0 (explicitly out of scope).

## Decision 8: Information architecture slot for billing

- Decision: Add a stub or FAQ-linked placeholder page (for example `/credits-and-billing`
  or a “Coming soon” section on FAQ) wired into navigation as secondary, ready for issues
  #125/#126 without a second publish pipeline.
- Rationale: FR-007 requires a documented slot without implementing Stripe or ledger UI.
- Alternatives considered: No placeholder (rejected — risks ad hoc pages later); hide until
  Stripe ships (acceptable if stub is clearly non-committing — prefer explicit stub).

## Decision 9: Demo mode — application architecture

- Decision: Implement demo mode as an explicit Application-layer session flag (off at
  sign-in, cleared on sign-out) combined with Web-layer operator authorisation (deployment
  allowlist + “demo controls enabled” policy). While active, route import workflow reads
  through synthetic fixtures and stub/no-op Graph and file-processing adapters registered in
  DI; never call live `IPlannerGateway` / upload parsers for import steps.
- Rationale: FR-015, FR-016, and constitution gates VIII (authorisation), VI
  (testability), and X (self-hosted parity) require deterministic synthetic behaviour and
  deployment-configured gates without tenant-admin shortcuts.
- Alternatives considered: UI-only masking with live Graph (rejected in clarification);
  separate demo host only (rejected — self-hosted must support same mechanism); Playwright
 -only fake UI (rejected — spec requires in-app toggle for authors).

## Decision 10: App links to public legal bundle

- Decision: Add a persistent footer or help region in `MainLayout` (or dedicated help
  component) linking to absolute URLs on `https://docs.importplanner.app` for terms,
  privacy, support (GitHub Issues), and user documentation home — configurable base URL for
  self-hosted builds.
- Rationale: FR-006 and SC-002 require two-click discoverability from the hosted app;
  layout currently has no footer links.
- Alternatives considered: In-app modal for terms (explicitly out of scope for v1.0);
  relative links only (rejected — breaks when app and docs hostnames differ in some modes).

## Decision 11: Root CHANGELOG and release runbook

- Decision: Add `CHANGELOG.md` at repository root (Keep a Changelog format) and extend
  internal release runbook in `docs/` with tag cutting, Pages deploy verification, and
  legal sign-off checklist steps.
- Rationale: FR-012 and user story 4 require maintainer-facing release discipline tied to
  public site versioning.
- Alternatives considered: Releases notes only on GitHub (insufficient — spec requires root
  changelog).

## Decision 12: Supersede spec 007 publishing contract, preserve content obligations

- Decision: Replace `specs/007-end-user-docs-site/contracts/docs-site-contract.md` as the
  active public contract with `specs/017-public-hugo-website/contracts/public-site-contract.md`
  while carrying forward page coverage and navigation intent; mark 007 contract as
  historical in implementation tasks only (not duplicated here).
- Rationale: FR-014 and traceability require a single active contract aligned with `website/`.
- Alternatives considered: Patch 007 contract in place (rejected — wrong feature folder and
  Jekyll-specific publication rules).

No `NEEDS CLARIFICATION` items remain in Technical Context after this research pass.
