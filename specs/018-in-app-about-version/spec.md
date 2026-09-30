# Feature Specification: In-App Release Version and About Surface

**Feature Branch**: `018-in-app-about-version`

**Created**: 2026-09-30

**Status**: Draft

**Input**: User description: "Give hosted and self-hosted users a clear in-app view of which release they are running, aligned with SemVer `vX.Y.Z` tags used for the public user guide (GitHub issue #164). Surface version on an About page or dialog—not only on the public documentation site. Pair production app releases with the same tags as the public site so docs and app do not diverge. Follow-on to public Hugo site work (#123 / spec 017); land before or with the first commercial v1.0 public release."

**Traceability**: GitHub [#164](https://github.com/markheydon/import-to-planner/issues/164) (story), [#123](https://github.com/markheydon/import-to-planner/issues/123) (public site release versioning), [#134](https://github.com/markheydon/import-to-planner/issues/134) (footer/help links to terms, privacy, support).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Signed-In User Checks Their Running Release (Priority: P1)

A hosted or self-hosted user who is signed in needs to answer “What version am I on?” without opening browser developer tools or guessing from deployment URLs. They open an in-app **About** view from familiar shell entry points (for example footer or help) and see a clear release label they can quote in support requests.

**Why this priority**: Support triage, GitHub Issues, and commercial credibility all depend on users and operators sharing an accurate version string.

**Independent Test**: Can be fully tested by signing in, opening About from the documented entry point, and reading the displayed release label on a known deployment.

**Acceptance Scenarios**:

1. **Given** a signed-in user on any supported deployment, **When** they open About from footer or help navigation, **Then** they see the product name and a human-readable release version label in UK English.
2. **Given** a user on a deployment built from an official SemVer release tag, **When** they open About, **Then** the release label matches that tag (for example `v1.0.0` without requiring them to infer from the URL).
3. **Given** a user on a non-release deployment (local build, main-branch staging, or pull-request environment), **When** they open About, **Then** the label clearly indicates pre-release or unreleased status and does not falsely imply a shipped public release unless policy explicitly allows that label for that environment.

---

### User Story 2 - User Confirms Documentation Matches Their Product Release (Priority: P2)

A user reading the public user guide needs to see which product release the documentation describes, in the same spirit as the reference Solo Dev Board pattern, so they can tell whether the guide applies to the app they are using.

**Why this priority**: Issue follow-up comment (2026-09-30) requires obvious doc–product alignment; mismatched version labels erode trust when the public site and app ship on different cadences.

**Independent Test**: Can be tested on a tagged public release by comparing the release label on the public site, the in-app About label, and the published user guide’s stated applicable release—they must agree for that cut.

**Acceptance Scenarios**:

1. **Given** a visitor opens the published public user guide after a SemVer release, **When** they look for version context, **Then** they find a visible indication of which product release the guide applies to (aligned with the same SemVer tag naming used for public releases).
2. **Given** a user on a tagged production app deployment, **When** they compare About with the live public site release badge or footer, **Then** both show the same SemVer release for that cut.
3. **Given** a user on staging or an unreleased build, **When** they read the public site (still showing the last tagged release) and their in-app About label, **Then** copy does not imply the staging build is the same as the currently published guide release unless explicitly described as a preview.

---

### User Story 3 - Maintainer Cuts a Coherent Public Release (Priority: P3)

A maintainer preparing the first commercial v1.0 (and subsequent releases) needs a documented order of operations so the GitHub Release tag, public documentation deploy, and production app deploy all represent one product version.

**Why this priority**: Without aligned tagging discipline, users see one version in the app and another on the public site—exactly the confusion this story prevents.

**Independent Test**: Can be tested by following the internal release runbook for a test tag in a non-production environment and completing the verification checklist (About, public site label, GitHub Release tag).

**Acceptance Scenarios**:

1. **Given** a maintainer creates a SemVer release tag, **When** they complete the documented release sequence, **Then** production (hosted and documented self-hosted release path) is built or deployed from that same tag as the public user guide publish for that release.
2. **Given** a completed tagged release, **When** a reviewer runs the release verification checklist, **Then** they can record pass/fail for matching version labels across GitHub Release, public site, and in-app About.
3. **Given** internal engineering documentation for releases, **When** a new contributor reads how local, continuous integration, and tagged builds derive version strings, **Then** they understand pre-release labels on main or staging versus tagged production releases.

---

### User Story 4 - Support Gathers Diagnostic Context (Priority: P4)

A user reporting a problem via GitHub Issues or email needs to attach enough build context for maintainers to reproduce or narrow the issue, beyond the SemVer string alone.

**Why this priority**: About provides room for build metadata without cluttering every screen; optional context reduces back-and-forth.

**Independent Test**: Can be tested by opening About on a deployment where build metadata is injected and confirming optional fields (for example build timestamp and source revision reference) appear when available and are omitted or honestly labelled when not.

**Acceptance Scenarios**:

1. **Given** a deployment where build metadata is available, **When** a user opens About, **Then** they see practical context such as build time (UTC) and a source revision identifier or link when the build supplies it.
2. **Given** a deployment where revision metadata is not injected, **When** a user opens About, **Then** the release label still appears and missing metadata does not block access or show misleading placeholders.
3. **Given** About is open, **When** a user follows links to public documentation, terms, privacy, or support, **Then** those links match the same destinations established for footer and help (#134).

---

### Edge Cases

- What happens when a self-hosted operator builds from source without creating a git tag? About must show an honest pre-release or local-build label, not a fabricated shipping version.
- How does About behave for signed-out users? Entry points reachable without signing in (if any) either show a reduced About or direct users to sign in—policy must not expose tenant-sensitive data on About.
- What if the public site still shows the previous tag while staging app deploys advance on main? Staging About labels must remain pre-release; public site continues to show the last tagged release until the next tag deploy.
- What if build metadata injection fails in continuous integration? Release version from tag discipline still displays; optional fields are absent rather than incorrect.
- Accessibility: About content must be readable with screen readers and keyboard navigation consistent with the rest of the app shell.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The web application MUST derive its displayed release version from the same SemVer git tag discipline (`vX.Y.Z`) used for public documentation releases, with a single authoritative versioning source configured at build time (not manually duplicated in unrelated files).
- **FR-002**: Signed-in users MUST be able to open a dedicated **About** view from existing shell navigation patterns (footer and/or help), using UK English copy and accessible layout consistent with the product shell.
- **FR-003**: About MUST display at minimum the product name and the current deployment’s release version label.
- **FR-004**: About SHOULD display optional build context when the build provides it: build timestamp (UTC) and source revision reference or link.
- **FR-005**: Deployments built from an official release tag MUST show a release label that matches that tag; deployments not built from such a tag MUST show an explicit pre-release or unreleased label.
- **FR-006**: On tagged production releases, the in-app About release label MUST match the public site release label and the GitHub Release tag for that release.
- **FR-007**: The public user guide MUST visibly indicate which product release its content applies to on tagged publishes, so users can confirm they are reading documentation for their release (aligned with issue #164 follow-up comment).
- **FR-008**: About MUST link to the same public documentation, terms, privacy, and support destinations already required for footer and help (#134), without breaking those contracts.
- **FR-009**: Internal engineering documentation MUST describe how local, continuous integration, staging, and tagged production builds derive version strings, and MUST extend the release runbook (introduced with spec 017 change history and tagging notes) with order of operations and a three-way verification checklist (GitHub Release tag, public site label, in-app About).
- **FR-010**: Where release-version parsing or formatting logic is non-trivial, automated tests MUST cover expected SemVer, pre-release, and missing-tag outcomes so regressions are caught before release.

### Key Entities

- **Release version label**: The SemVer string shown to users (from an official `vX.Y.Z` tag on production releases, or an explicit pre-release/unreleased label otherwise).
- **Build metadata**: Optional timestamp and source revision information attached at build time for diagnostics.
- **About view**: In-app surface combining product identity, release label, optional build metadata, and links to public legal and documentation pages.
- **Release verification checklist**: Maintainer artefact recording that tag, public site, and app About labels match for a given release.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In user testing or structured review, **100%** of signed-in participants can locate and read the release version from About within **three** navigation actions from the primary import journey entry, without developer tools.
- **SC-002**: For every tagged production release cut after this feature ships, reviewers record **pass** on the three-way match check (GitHub Release tag name = public site release label = in-app About release label) before marking the release complete.
- **SC-003**: On non-tag staging and local builds reviewed before v1.0, **zero** instances show an unqualified shipping SemVer (for example bare `v1.0.0`) unless an documented environment policy explicitly permits that label for that environment.
- **SC-004**: Support volunteers can obtain a quotable version string from a user following the About path in under **one minute** in a scripted triage exercise.
- **SC-005**: On tagged public guide publishes, **100%** of sampled primary guide entry pages expose the applicable product release label in a location visible without scrolling past the main hero or title region (or equivalent site-wide badge already satisfying spec 017, extended to make doc applicability obvious per issue comment).

## Assumptions

- Spec **017-public-hugo-website** delivers tag-only public site publish and visible release version on tagged deploys; this feature adds in-app parity and release-runbook alignment rather than replacing Hugo scope.
- Timing: absence of in-app version until the first commercial v1.0 public release remains acceptable; this work lands as a follow-on before or with that release so docs and app do not diverge at launch.
- Hosted production and the documented self-hosted release path both consume the same tagging policy for public releases; ad-hoc self-hosted builds from arbitrary commits remain supported with honest pre-release labels.
- Footer and help link destinations from #134 / spec 017 remain the canonical external URLs About reuses.
- Solo Dev Board provides a reference **shape** (tag-driven assembly versioning, About page, aligned Hugo release parameter); full Solo Dev Board extras (environment matrix tied to cloud deploy, mandatory Playwright doc capture) are not required unless explicitly added later.

## Dependencies

- **Pairs with**: spec 017 / #123 (public site release version and tag-only publish).
- **Related**: #134 (terms, privacy, support links); milestone v1.0; GitHub issue #164 comment thread on workflow screenshot follow-on (tracked separately—see out of scope).

## Out of Scope

- Full Solo Dev Board **About version matrix** or per-environment continuous deployment coupling beyond honest labels for each deployment type.
- Mandatory automated Playwright screenshot pipeline for documentation (including the distinct demo-workflow capture checklist on issue #164—follow-on work tracked on the issue, not part of About acceptance criteria).
- Changing spec 017 Hugo layout, demo mode, or terms gate scope.
- Replacing GitHub Issues as the v1.0 support path (#134 / spec 017 clarifications).

## Related Follow-On (issue tracking only)

The following items remain on GitHub #164 for visibility but are **not** acceptance criteria for this specification:

- Capture five distinct demonstration-mode workflow screenshots aligned with the public import workflow guide and in-app wizard.
- Keep `./scripts/capture-demo-workflow-screenshots.sh` and distinctness verification green once images exist.
- Treat user-guide versus end-to-end journey mismatch as a defect in the same spirit as Solo Dev Board.
