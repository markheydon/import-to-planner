# Research: In-App Release Version and About Surface

## Decision 1: Authoritative SemVer from git tags via MinVer at build time

- Decision: Add the [MinVer](https://github.com/adamralph/minver) MSBuild package at repository
  root (`Directory.Build.props` or shared `Directory.Build.targets`) so `AssemblyInformationalVersion`
  and related attributes derive from annotated `vX.Y.Z` tags, with pre-release identifiers on
  non-tag commits (for example `1.0.0-preview.5+abc1234`).
- Rationale: FR-001 and FR-005 require one build-time source aligned with GitHub Release tags;
  the public Hugo site already uses `github.ref_name` on tag deploys. MinVer is the reference
  Solo Dev Board pattern cited in spec 017 assumptions, adapted here without a multi-environment
  version matrix.
- Alternatives considered: Manual `Version` in csproj (rejected — duplicates tag discipline);
  reading git at runtime in the web host (rejected — container images may omit `.git`; violates
  single build-time source); Nerdbank.GitVersioning (rejected — heavier setup for a single app
  plus static site).

## Decision 2: Surface release label through Application boundary, render in Web

- Decision: Introduce a small Application-facing port (for example
  `IReleaseInformationQuery` returning structured `ReleaseInformation` with version label,
  optional UTC build timestamp, optional source revision) implemented in Web or a thin
  Infrastructure adapter that reads assembly/custom attributes and environment variables
  injected in CI. A dedicated formatter/normaliser in Application (or Application-owned static
  helper tested in `ImportToPlanner.Tests`) enforces FR-005 rules (tag match vs pre-release
  suffix, never bare shipping SemVer on non-tag builds unless policy flag allows).
- Rationale: Constitution III — Razor assembles UK English presentation strings; Application
  owns “what label is honest for this deployment”. FR-010 requires unit tests on non-trivial
  parsing/formatting.
- Alternatives considered: Razor-only read of `Assembly.GetExecutingAssembly()` (rejected —
  untested policy in UI); Domain entity for release (rejected — not business policy).

## Decision 3: About page as authenticated Blazor route `/about`

- Decision: Add `Features/About/Pages/About.razor` at route `/about` using the same
  sign-in gate pattern as `Profile` (prompt + sign-in button when unauthenticated; optional
  return URL after auth). Do not use a modal dialog (clarification 2026-09-30).
- Rationale: FR-002 and user story 1; existing `AuthorizeRouteView` does not globally require
  auth, so page-level gating matches established commercial profile behaviour.
- Alternatives considered: `[Authorize]` on page only (rejected — inconsistent with profile UX
  and Microsoft Identity Web redirect nuances); public About (rejected — clarification requires
  sign-in).

## Decision 4: Shell entry points — footer link plus new Help menu

- Decision: Add an internal `About` link to `MainLayout.razor` footer alongside existing
  external doc/legal links. Introduce a persistent **Help** affordance in application chrome
  (for example MudMenu in `MainLayout` or shared header partial used on import workflow pages)
  listing Documentation (external), Support (external), and About (internal route). Hide or
  route unsigned users through sign-in when selecting About (FR-002 edge case).
- Rationale: Clarification requires **both** footer and help; repository currently has footer
  external links only and no help menu — planning must add help as part of implementation.
- Alternatives considered: Footer only (rejected — fails clarification); help only (rejected).

## Decision 5: Optional build metadata via CI and assembly attributes

- Decision: Document and implement optional injection of `SourceRevisionId` (git commit SHA)
  and `BuildTimestampUtc` through MSBuild properties (`/p:SourceRevisionId=…`,
  `/p:ContinuousIntegrationBuild=true`) in CI and container publish pipelines; About displays
  fields when present and omits them otherwise (FR-004).
- Rationale: Support story 4 and edge case “metadata injection fails” without blocking About.
- Alternatives considered: Always show placeholder “Unknown” (rejected — spec forbids misleading
  placeholders).

## Decision 6: Public-site FR-007 gap — hero-visible guide applicability

- Decision: Keep site-wide footer release line from spec 017 (`Documentation for release {{ tag }}`).
  Add a compact release applicability callout on primary guide entry surfaces — at minimum the
  landing page (`website/content/_index.md` or a Hugo partial included above the fold) and the
  import workflow page — using the same `site.Params.releaseVersion` value as the footer. On
  `unreleased` builds, copy must state the guide build is unreleased, not a production tag.
- Rationale: SC-005 requires visible applicability without scrolling past the hero/title; footer
  alone may require scrolling on long pages. Spec 018 owns remaining gaps; 017 footer satisfies
  site-wide label only.
- Alternatives considered: Footer-only (insufficient for SC-005 on long pages); duplicate version
  in every content file (rejected — use partial/shortcode).

## Decision 7: Extend release runbook for three-way verification and app deploy

- Decision: Extend `docs/release-runbook.md` with ordered steps: changelog → tag → Hugo deploy
  verification (existing) → **application** deploy from the same tag (hosted ACA release path +
  documented self-hosted image/tag instructions) → checklist row for GitHub Release name, public
  site label, in-app About label (FR-009, SC-002).
- Rationale: User story 3; current runbook covers Hugo only; staging deploy tracks `main`, not
  release tags — runbook must document how production app cuts align with tags even if staging
  stays on main pre-release identifiers.
- Alternatives considered: Separate new doc only (rejected — spec requires extending 017 runbook).

## Decision 8: Testing strategy

- Decision: Unit tests in `ImportToPlanner.Tests` for release label normalisation (tagged,
  pre-release, missing tag, policy edge). bUnit tests in `ImportToPlanner.Web.Tests` for About
  page auth prompt, signed-in content, and link hrefs. Optional Playwright journey for About
  path (SC-001) only if added explicitly in tasks — not mandatory in plan.
- Rationale: FR-010, constitution VI, engineering policies (xUnit v3, NSubstitute, bUnit).
- Alternatives considered: Manual-only verification (rejected — FR-010).
