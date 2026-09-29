# Feature Specification: Public Product Website, Documentation Layout, and Release Versioning

**Feature Branch**: `017-public-hugo-website`

**Created**: 2026-09-29

**Status**: Draft

**Input**: User description: "Migrate Import To Planner documentation to the Solo Dev Board public-site pattern (GitHub issues #123 and #134). Public product site under `website/`; engineering docs in standard `docs/` (not published). Local container-based preview scripts aligned with the Solo Dev Board reference project. GitHub Pages publish only on release tags with visible release version. UK English terms, expanded privacy, and support contact on the public site and linked from the hosted app. Demo mode for capturing documentation screenshots without live or tenant-sensitive data. Supersedes spec 007 publishing stack while preserving its end-user content obligations."

**Traceability**: GitHub [#123](https://github.com/markheydon/import-to-planner/issues/123) (feature), [#134](https://github.com/markheydon/import-to-planner/issues/134) (terms, privacy, support story).

## Clarifications

### Session 2026-09-29

- Q: How should documentation authors enable demo mode for screenshot capture? → A: Maintainer in-app toggle (authorised service operators enable demo explicitly in the hosted app).
- Q: Which rule should define who may use the in-app demo toggle? → A: Deployment-configured allowlist of operator Entra object IDs and/or verified work-account UPNs; demo toggle UI hidden on production unless deployment policy explicitly enables operator demo controls (not Microsoft 365 tenant administrator roles).
- Q: What is the canonical hosted support contact that the public site and app must publish and link to? → A: GitHub Issues in this repository as the v1.0 interim path; dedicated end-user support (for example email or helpdesk) is out of scope here and expected to be considered in a v1.1 release.
- Q: What maturity level should the public terms of use meet at the v1.0 documentation release? → A: Full v1.0 legal text — production-ready terms with counsel sign-off before the first public site release tag.
- Q: What should happen to the legacy Jekyll end-user site under `docs/` once the Hugo site under `website/` is ready to publish on a release tag? → A: Single cutover — migrate end-user content to `website/`, retire Jekyll publishing, and use `docs/` for internal engineering docs only (including migration from the former internal-only path).
- Q: While demo mode is active for an allowlisted operator, should the import journey use only in-memory synthetic data with no Microsoft Graph calls, or may it still call Graph as long as the UI shows synthetic placeholders? → A: Fully synthetic journey only — no Microsoft Graph or real upload processing while demo is on.
- Q: Should self-hosted deployments offer the same operator-gated demo mode (allowlist plus deployment policy) as the hosted service, or is demo mode in scope only for MHCG-hosted environments? → A: Same operator-gated demo mode on self-hosted and hosted (allowlist plus policy; production demo controls off unless explicitly enabled).
- Q: Where should recorded legal counsel sign-off for the terms of use live so reviewers can verify it before the first public site release tag? → A: Internal engineering docs (`docs/`) — checklist or runbook entry with date and terms reference.
- Q: After an allowlisted operator turns demo mode on, when should it turn off automatically? → A: Off at sign-out; while signed in, on until the operator toggles off.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visitor Uses a Trustworthy Public Product Site (Priority: P1)

A prospective or current user opens the public documentation site at the established custom domain. They need accurate getting-started, CSV format, import workflow, troubleshooting, FAQ, and privacy guidance that matches how the hosted app actually behaves, presented as a cohesive product site rather than a thin afterthought.

**Why this priority**: Commercial v1.0 credibility depends on a professional public site; without it, users cannot self-serve before or during import.

**Independent Test**: Can be fully tested by reading the published (or locally previewed) public site navigation and core guides and confirming coverage matches the content obligations from the prior end-user documentation feature, in UK English, with behaviour descriptions that still align with the app.

**Acceptance Scenarios**:

1. **Given** a visitor opens the public site home page, **When** they browse the main guides, **Then** they find landing, getting started, CSV format, import workflow, troubleshooting, FAQ, privacy and security, and a secondary self-hosted page linked appropriately.
2. **Given** a visitor reads workflow and format pages, **When** they compare steps to the hosted app, **Then** descriptions remain accurate for the current import journey (including validation, preview, execution, and report review) without requiring live tenant data in examples.
3. **Given** contributor-facing material previously split between public pages and internal folders, **When** the change is complete, **Then** only the public product site content is intended for end-user publication; engineering policies and runbooks are not mixed into that tree.

---

### User Story 2 - Hosted User Finds Terms, Privacy, and Support (Priority: P2)

A hosted user (including future paying or credit-based users) needs clear terms of use, a plain-language privacy and retention statement, and a visible way to raise feedback or problems before they rely on the service commercially. For v1.0, that path is the public GitHub issue tracker; a dedicated end-user support channel may follow in v1.1.

**Why this priority**: Missing terms or a concrete feedback path makes the product look unfinished; setting expectations about interim GitHub-based support avoids implying a helpdesk that does not exist yet.

**Independent Test**: Can be tested by locating terms, privacy, and support from the public site and from the hosted app footer or help entry points, without using a separate legacy documentation stack.

**Acceptance Scenarios**:

1. **Given** a hosted user opens the public site, **When** they look for legal and support information, **Then** they find a terms page (or equivalent), an expanded privacy page, and a single published support path that opens the repository GitHub Issues entry point with plain UK English scope (for example bugs, documentation corrections, and product feedback).
2. **Given** a hosted user uses the app, **When** they open footer or help links, **Then** those links reach the same public terms, privacy, and support pages (including the GitHub Issues path).
3. **Given** an operator reads self-hosted documentation, **When** they follow guidance about legal or support obligations, **Then** it is clear that hosted commercial terms and MHCG-hosted feedback paths apply to the hosted service only, not as if MHCG operates every self-hosted instance.
4. **Given** future billing pages are added, **When** they are published, **Then** they can link to the same terms without a second legal stack.
5. **Given** a hosted user reads the support guidance, **When** they look for service-level or commercial support commitments, **Then** copy states that dedicated end-user support is not part of v1.0 and is expected to be considered for v1.1 (without promising a specific channel or date).
6. **Given** a maintainer prepares the first SemVer public site release tag, **When** they review the terms page, **Then** the published terms are production-ready and internal engineering documentation contains a checklist or runbook entry recording legal counsel sign-off with approval date and reference to the terms version that will ship (not a draft or “coming soon” stub).

---

### User Story 3 - Contributor Previews the Public Site Locally (Priority: P3)

A contributor needs to build and preview the public product site on their machine without installing a full local documentation toolchain, using the same container-based workflow as the Solo Dev Board reference project (build, serve, and preview entry points).

**Why this priority**: Reliable local preview prevents broken publishes and speeds content review before release tags.

**Independent Test**: Can be tested by running the documented local scripts with an available container engine and confirming the site renders for edit-review without publishing to the internet.

**Acceptance Scenarios**:

1. **Given** a contributor has Docker or Podman available, **When** they run the documented build command, **Then** the public site compiles successfully from the product site source tree.
2. **Given** a contributor wants to review changes in a browser, **When** they run the documented serve or preview command, **Then** they can browse the site locally before any release tag is created.
3. **Given** a pull request changes only the product site source, **When** continuous integration runs, **Then** an automated build check fails if the site does not compile.

---

### User Story 4 - Stakeholder Ships a Versioned Public Release (Priority: P4)

A maintainer cutting v1.0 needs SemVer release tags, a human-readable change history, and a public site that shows which release visitors are reading—without publishing every merge to main.

**Why this priority**: Separating staging app deploys from public documentation releases avoids confusing users with half-finished copy and supports commercial versioning expectations.

**Independent Test**: Can be tested by creating a release tag, verifying the public site deploy runs for that tag only (or an documented manual equivalent), and confirming visitors see the tag version on the published site while local and pull-request previews show an explicit non-release label.

**Acceptance Scenarios**:

1. **Given** changes merge to the main branch without a release tag, **When** users visit the live public site, **Then** the previously released version remains what is published (main merges alone do not replace the live public site).
2. **Given** a maintainer publishes a SemVer tag, **When** the release deploy completes, **Then** the public site displays that release version visibly (for example in a site-wide badge or footer).
3. **Given** a new contributor, **When** they read internal engineering documentation for releases, **Then** they find how to maintain the root change log and tag `vX.Y.Z` releases.

---

### User Story 5 - Author Captures Safe Documentation Screenshots (Priority: P5)

A documentation author or marketer needs to illustrate the import workflow with screenshots that never expose real customer CSV rows, live Microsoft 365 tenant names, or production diagnostics.

**Why this priority**: Spec 007 deferred screenshots until safe demo data existed; privacy concerns block using live hosted data for captures.

**Independent Test**: Can be tested by enabling the documented demo mode, walking the import journey, and confirming all visible names, tasks, and CSV samples are synthetic placeholders with no connection to a real tenant.

**Acceptance Scenarios**:

1. **Given** an allowlisted service operator enables demo mode via the in-app control on a deployment where operator demo controls are permitted, **When** they step through group selection, plan naming, upload, validation, preview, execution, and report review, **Then** every screen shows only synthetic data clearly intended for demonstration and the journey does not invoke Microsoft Graph or process a real uploaded file while demo remains active.
2. **Given** demo mode is active, **When** an author captures screenshots for the public workflow guide, **Then** no real user identifiers, tenant identifiers, or CSV cell contents from production sessions appear in those images.
3. **Given** demo mode is disabled, **When** a normal user signs in on a hosted or self-hosted deployment, **Then** the app behaves as today with real Graph and upload data (demo mode defaults off at sign-in and must not leak into standard sessions).
4. **Given** an allowlisted operator enabled demo mode and then signs out, **When** they sign in again without toggling demo on, **Then** demo mode is off and the normal import journey applies.
5. **Given** an allowlisted operator has demo mode on, **When** they remain signed in, **Then** demo stays active until they toggle it off or sign out.
6. **Given** a signed-in user is not on the operator allowlist, **When** they use the product on any deployment, **Then** they never see a demo-mode toggle (customer Microsoft 365 tenant administrator roles do not grant demo access).
7. **Given** production deployment policy has not enabled operator demo controls, **When** any user signs in on hosted or self-hosted production, **Then** the demo toggle is not shown regardless of allowlist membership.
8. **Given** a self-hosted operator on an allowlisted identity with demo controls permitted by deployment policy, **When** they enable demo mode in-app, **Then** they receive the same fully synthetic import journey as on hosted non-production environments (no separate self-hosted-only demo mechanism).

---

### User Story 6 - Contributor Finds Engineering Guidance in One Place (Priority: P6)

A contributor or agent working in the repository needs engineering policies, runbooks, and Microsoft Graph guidance in the conventional internal documentation folder, with repository readme, agent policy, skills, and pull request templates pointing to the correct public versus internal locations.

**Why this priority**: Wrong paths cause agents and humans to edit unpublished material or publish internal notes by mistake.

**Independent Test**: Can be tested by following links from the root readme and agent policy files and confirming they resolve to the internal engineering tree for operator docs and the product site tree for end-user docs.

**Acceptance Scenarios**:

1. **Given** former internal documentation has moved, **When** a contributor searches the repository, **Then** engineering content lives under the standard internal docs folder and is excluded from the public site artefact.
2. **Given** spec 007 content obligations still apply to end-user pages, **When** agents follow updated policy, **Then** they target the product site for public copy and do not assume legacy publish-on-main Jekyll assumptions.
3. **Given** the Hugo public site is ready for a release tag, **When** the cutover is complete, **Then** legacy Jekyll end-user publishing from `docs/` is removed, end-user content lives under `website/`, and `docs/` contains only internal engineering documentation with no second public doc stack.

---

### Edge Cases

- What happens when CSV format documentation must change because importer features (#127, #130–#133) ship before or after the site migration? Format pages must stay in lockstep with parser behaviour; stubs must not freeze outdated column rules.
- How are credits and billing pages handled before Stripe work lands? Information architecture must reserve a documented slot (stub or FAQ placeholder) so billing copy does not invent a second public site.
- What if a contributor has neither Docker nor Podman? Internal docs should state the requirement; optional native toolchain install may be documented but is not the primary supported path for this feature.
- What if someone deploys from main expecting instant doc updates? Release policy and internal runbooks must state that only tagged releases update the live public site.
- How do pull-request preview builds label version? They show an explicit non-release label (for example “unreleased”) rather than implying a shipped SemVer tag.
- Self-hosted operators reading troubleshooting must not be told to contact hosted-only support as if MHCG runs their instance.
- Users expecting email or SLA-backed support at v1.0 must see explicit copy that GitHub Issues is the interim path until a possible v1.1 dedicated support feature.
- The first public site release tag MUST NOT ship until terms copy has counsel sign-off recorded in `docs/`; privacy and guides may iterate later, but terms cannot launch as draft placeholders.
- How is demo access distinguished from tenant admin consent? Demo mode is gated by a service-operator allowlist and deployment policy, not by Microsoft 365 tenant administrator status; mislabelling either in UI or docs creates a security and support risk.
- What if demo mode accidentally triggers Graph or file processing? Implementation and tests MUST treat any Graph or real upload during active demo as a defect; screenshot and privacy reviews assume zero live integration calls for import steps while demo is on.
- What if an operator forgets demo is on? Demo MUST clear on sign-out; operators who stay signed in MUST toggle demo off before real imports or rely on sign-out before switching to live work.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repository MUST provide a dedicated public product site source tree (`website/`) separate from internal engineering documentation; all end-user pages migrated from legacy Jekyll under `docs/` MUST live in that tree before the first Hugo release tag ships.
- **FR-002**: Internal engineering documentation MUST live in the repository’s standard internal docs folder (`docs/`, migrated from the former internal-only path) and MUST NOT be included in the public site publish artefact. Legacy Jekyll end-user publishing from `docs/` MUST be retired as part of this feature so only the Hugo artefact from `website/` publishes to the custom domain.
- **FR-003**: The public site MUST preserve the custom domain `docs.importplanner.app` for end-user documentation.
- **FR-004**: All migrated and new public pages MUST use UK English and MUST satisfy the end-user content coverage established by the prior end-user documentation feature (landing, getting started, CSV format, workflow, troubleshooting, FAQ, privacy, secondary self-hosted), updated for current app behaviour where importer work has changed rules.
- **FR-005**: The public site MUST include production-ready terms of use (legal counsel sign-off recorded in internal engineering documentation before the first public site release tag), an expanded privacy and retention statement, and a visible v1.0 support path that links to this repository’s GitHub Issues entry point with plain-language scope, plus wording that distinguishes hosted service obligations from self-hosted operator responsibilities. Copy MUST state that dedicated end-user support is deferred beyond v1.0 (expected consideration in v1.1) without implying a helpdesk or response-time commitment in this release.
- **FR-006**: The hosted application MUST link to the public terms, privacy, and support pages from places users actually look (for example footer or help menu), and troubleshooting or FAQ copy MUST link to the same GitHub Issues support path instead of a generic “contact your support team” with no destination.
- **FR-007**: The public site information architecture MUST include a documented place for future credits and billing user copy without requiring a second documentation publishing stack.
- **FR-008**: Contributors MUST be able to build, serve, and preview the public site locally using documented container-based scripts aligned with the repository’s reference implementation (see Assumptions), without requiring a local static-site toolchain install when a container engine is available.
- **FR-009**: Pull requests that change the public site source MUST trigger an automated compile check that blocks merge on failure.
- **FR-010**: The live public site MUST deploy only on SemVer release tags (with an optional documented manual dispatch for maintainers), not on every merge to main.
- **FR-011**: Published public site output MUST display the release tag version on tagged deploys; local and non-tag builds MUST display an explicit non-release version label.
- **FR-012**: The repository MUST include a root change log following Keep a Changelog conventions and internal engineering notes describing how to cut `vX.Y.Z` releases, including where to record legal counsel sign-off for terms before the first public site release tag.
- **FR-013**: Repository entry points (readme, contributing guide, agent policy, relevant skills, pull request template) MUST reference the correct public product site versus internal docs locations after the move.
- **FR-014**: Prior specification assumptions that public pages live in Jekyll under `docs/` with automatic publish on main MUST be superseded in agent-facing policy and removed from automation so future work targets `website/` for end users and `docs/` for engineering guidance only.
- **FR-015**: The product MUST support demo mode on both MHCG-hosted and self-hosted deployments, toggled in-app only by authorised service operators, that presents a fully synthetic import workflow while active and defaults off at sign-in for everyone. Demo mode MUST turn off automatically on sign-out; while the operator remains signed in, it stays active until they toggle it off. While demo is active, the app MUST NOT call Microsoft Graph or process real user uploads for import steps; only in-memory synthetic fixtures drive the journey. Operator authorisation MUST use a deployment-configured allowlist of Entra object IDs and/or verified work-account UPNs matched after sign-in; Microsoft 365 tenant administrator roles MUST NOT grant demo access. The demo toggle UI MUST NOT appear on production deployments (hosted or self-hosted) unless deployment policy explicitly enables operator demo controls.
- **FR-016**: Demo mode MUST be documented for authors (allowlist and deployment requirements for hosted and self-hosted, how to enable the toggle, what is synthetic, privacy guarantees), and public workflow guidance SHOULD be updated to use screenshots captured under demo mode once available.

### Key Entities *(include if feature involves data)*

- **Public product site**: End-user-facing pages, navigation, legal copy, and release-version presentation intended for GitHub Pages at the custom domain.
- **Internal engineering docs**: Operator runbooks, policies, Graph guidelines, release procedures, and billing design notes not published to end users.
- **Release version label**: The SemVer tag name shown on tagged public deploys versus an explicit non-release label on local and pre-release builds.
- **Demo dataset**: Synthetic groups, plans, tasks, and CSV examples used only when demo mode is active for documentation and marketing capture.
- **Demo mode operator**: A service operator whose signed-in identity appears on the deployment allowlist and may enable demo mode where deployment policy permits; distinct from a customer’s Microsoft 365 tenant administrator.
- **Legal and support bundle**: Counsel-approved terms page, privacy page, and v1.0 interim support path (GitHub Issues) treated as one consistent set for hosted users and linkable from future billing pages; a dedicated support channel may be added in a later release (expected v1.1 consideration).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In user testing or structured review, at least 90% of checklist items from the prior end-user documentation feature are still satisfied on the migrated public site (same topics, accurate behaviour descriptions, UK English).
- **SC-002**: A reviewer can locate terms, privacy, and the published GitHub Issues support path from both the public site and the hosted app within two clicks from the app home or footer without encountering a dead or generic support instruction.
- **SC-003**: A contributor with only a container engine can complete local preview of the public site in under 15 minutes using documented scripts on first attempt.
- **SC-004**: Within one hour of publishing a new SemVer tag, the live public site shows the new release version label and content from that tag; merges to main without a tag do not change the live public site within that window.
- **SC-005**: When demo mode is enabled, a privacy review of captured screenshot sets finds zero real tenant identifiers and zero real CSV cell values from production sessions across a full import walkthrough, and verification confirms no Microsoft Graph or real upload processing occurred during the walkthrough.
- **SC-006**: Automated checks on pull requests touching the public site source fail 100% of the time when the site does not compile, and pass when it does (measured over the first month after launch).
- **SC-007**: Before the first SemVer public site release tag is published, reviewers can locate in internal engineering documentation (`docs/`) a checklist or runbook entry recording legal counsel sign-off with approval date and reference to the terms version that will ship with that tag.

## Assumptions

- The Solo Dev Board repository provides the reference shape for the product site folder, container invoke scripts, and tag-only Pages deploy workflows; Import To Planner adopts a slimmer subset (no MinVer assembly matrix, no mandatory Playwright docs-capture pipeline unless planning adds it for demo screenshots).
- Hugo extended with the Hextra theme (via Hugo modules) is the chosen static site generator for the public product site, matching the reference project; planning will detail scaffolding but the business outcome is a maintainable product site.
- Staging application deploy on main may continue independently; only the public documentation site follows release-tag publishing.
- For v1.0, the canonical support path is the repository GitHub Issues entry point, linked consistently from the public site and hosted app; dedicated end-user support is explicitly deferred (expected v1.1 consideration).
- Terms of use for the first public site release tag require legal counsel sign-off recorded in internal engineering documentation (`docs/`); privacy depth follows issue #134 but is not a substitute for counsel-approved terms.
- Credits, Stripe ledger implementation, Excel import, and template library remain out of scope; only information architecture slots and cross-links are in scope.
- Demo mode uses in-memory synthetic fixtures only for the import journey while active (no Microsoft Graph or real upload processing); it does not bypass authentication for sign-in, defaults off at sign-in, clears on sign-out, and when disabled the app uses normal Graph and upload behaviour.
- Demo mode operator allowlists and production demo-control policy are per-deployment configuration (hosted and self-hosted); staging and non-production environments are the expected place for screenshot capture.
- GitHub milestone `v1.0` remains the release bucket; patch-level tag naming does not require milestone renaming.
- Cutover is single-phase within this feature: no long-running dual publish from Jekyll and Hugo to the same custom domain.

## Dependencies

- GitHub issue #123 (feature scope and acceptance criteria).
- GitHub issue #134 (terms, privacy depth, self-hosted distinction); interim GitHub Issues support path for v1.0 fulfils the “contact path” intent while dedicated support is deferred.
- Spec `007-end-user-docs-site` content obligations (contract for page coverage and accuracy); publishing mechanics from that spec are replaced by this feature.
- Importer and CSV documentation features may require concurrent copy updates; format pages must not lag parser behaviour.
- Future billing issues (#125, #126) will link to terms published here.
- Legal counsel review and sign-off for terms of use (gate for the first public site release tag).

## Out of Scope

- Implementing Stripe, credit ledger, or commercial billing UI (beyond IA stubs and link targets).
- Full Solo Dev Board parity (MinVer staging versions, apex-domain runbooks, custom landing shortcodes, automated Playwright screenshot pipelines unless explicitly added during planning).
- Excel import (#55) and template library (#45).
- In-app legal modal on first sign-in (optional later enhancement).
- Dedicated end-user support channels (for example support email, helpdesk, or SLA-backed commercial support); expected to be considered for v1.1, not delivered in this feature.
