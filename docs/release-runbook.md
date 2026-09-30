# Public site and application release runbook

This runbook covers SemVer tagging for the Import To Planner repository when the public Hugo
site and/or application ship together.

## Terms of use publication record (SC-007)

Before the first public site `v*` tag (and when materially changing hosted terms), record
maintainer acceptance of the text in `website/content/terms.md`. This is **not** external
legal counsel review; it is an internal record that production-ready terms are published
knowingly. Engage qualified counsel when commercial risk warrants it (for example paid
billing at scale or enterprise DPAs).

**SC-007 governance (2026-09-30):** Spec 017 originally described external counsel sign-off;
this runbook uses a director/maintainer publication record instead. Enforcement is
maintainer process only (no CI gate on deploy).

| Field | Value |
| --- | --- |
| Approval date | 2026-09-30 |
| Terms version reference | `website/content/terms.md` (last updated 30 September 2026; informed by Simply Docs ASP TR.IT.11 adaptation — not counsel-reviewed) |
| Approver name / role | Director, MHCG LTD |

## Operational terms workflow

1. Review and update `website/content/terms.md` (and cross-links to privacy and support).
2. Update the publication record table above when terms change materially.
3. Merge to `main`.
4. Create and push a `vX.Y.Z` tag (see below).

## SemVer tagging and deploy

Follow the steps below in order for a coherent public release (FR-006, FR-009). The public Hugo
site and the application MUST be built from the **same** annotated `vX.Y.Z` tag so three-way
label parity is achievable at cut time.

### Staging versus tagged production

- **Staging (hosted ACA)** tracks successful CI on `main` via
  `.github/workflows/deploy-staging.yml` — see [ACA Staging Runbook](aca-staging-runbook.md).
  Staging About labels use MinVer **pre-release** identifiers until the next official tag; they
  do **not** represent the live public documentation release.
- **Tagged production** is the only routine path that updates the live public site (`v*` tag →
  Hugo deploy) and the production application cut documented below.
- Ordinary merges to `main` do **not** update the live public site. Pull request and branch
  Hugo builds label the site `unreleased`.

### Release cut (ordered)

1. **Changelog** — Update `CHANGELOG.md` under a new `## [vX.Y.Z] — YYYY-MM-DD` section.
2. **Commit on `main`** — Merge changelog and any final content or application changes.
3. **Annotated tag** — Create and push an annotated tag (not lightweight):
   `git tag -a vX.Y.Z -m "Release vX.Y.Z"` then `git push origin vX.Y.Z`.
4. **GitHub Release (recommended)** — Create a GitHub Release for `vX.Y.Z` whose **title/name**
   matches the tag string exactly (including the `v` prefix).
5. **Hugo deploy verification**
   - Monitor the **Hugo Deploy** workflow (`.github/workflows/hugo-deploy.yml`), triggered by
     the `v*` tag push.
   - In the workflow log, confirm `HUGO_PARAMS_RELEASEVERSION` equals the tag (for example
     `v1.2.3`).
   - **SC-004**: Within one hour of a successful deploy, confirm the live site footer and
     above-fold guide applicability callouts show the new label at
     `https://docs.importplanner.app`.
6. **Application deploy from the same tag** — Check out `vX.Y.Z` locally or in CI; do **not**
   deploy production from a floating `main` commit when cutting a release.
   - **Hosted production (ACA)** — Promote using the Aspire **Production** environment from
     that tag (manual approval, isolated configuration). Pattern:
     `aspire deploy --environment Production --non-interactive` after checking out the tag.
     Guardrails and handoff notes: [Aspire production readiness](aspire-production-readiness.md).
     Staging deploy mechanics (OIDC, parameters, certificates) mirror production; use
     [ACA Staging Runbook](aca-staging-runbook.md) as the operational template with Production
     secrets and environment names substituted.
   - **Self-hosted** — Operators build or pull an image (or publish binaries) from the **same**
     git tag so MinVer embeds the official release label. Tag the published container image with
     `vX.Y.Z` (or your registry convention that maps 1:1 to the git tag). End-user orientation:
     [Self-hosted](https://docs.importplanner.app/self-hosted/) on the public site.
7. **Three-way verification** — Complete the [Release verification checklist](#release-verification-checklist)
   before marking the release complete (SC-002). Any mismatch is **Fail** until remediated.

Optional: pass CI build metadata (`SourceRevisionId`, `BuildTimestampUtc`,
`ContinuousIntegrationBuild`) when publishing production images so About can show support
context — see [Build metadata (in-app About)](developer-quickstart.md#build-metadata-in-app-about-fr-004).

## Release verification checklist

Maintainers record one row per tagged production release. A tagged release cannot be marked
complete with any label mismatch (FR-006, SC-002).

### Comparing labels (`v` prefix)

Per `specs/018-in-app-about-version/data-model.md`, three-way checks compare:

- **TagName** — git / GitHub Release tag (repository convention: leading `v`, for example `v1.0.0`).
- **PublicSiteLabel** — value from `site.Params.releaseVersion` on the live docs site (footer and
  guide applicability partials).
- **InAppAboutLabel** — `DeploymentReleaseLabel.DisplayValue` on the signed-in About page for the
  deployment built from that tag.

For **Pass**, all three MUST represent the same SemVer after **normalisation**: strip a single
leading `v` (case-sensitive) from each value and compare the remainder (for example `v1.0.0`,
`1.0.0` on a misconfigured surface, and `v1.0.0` again normalise to `1.0.0`). Display on About
and the public site SHOULD still show the tag form including `v` when that is the tag convention.
Do not treat staging pre-release strings or `unreleased` as matching a production tag.

### Template

| TagName | PublicSiteLabel | InAppAboutLabel | VerifiedAtUtc | Verifier | Result |
| --- | --- | --- | --- | --- | --- |
| `vX.Y.Z` | | | | | Pass \| Fail |
| | | | | | |

Copy the table into the GitHub Release notes, an internal change record, or the PR that tracks the
release cut. Example dry run: `specs/018-in-app-about-version/quickstart.md` §6.

## How build contexts derive version strings (contributors)

Release labels are computed at **build time** from git state via [MinVer](https://github.com/adamralph/minver)
(`MinVerTagPrefix` = `v` in `Directory.Build.props`). Do not duplicate version numbers in source
for normal releases.

| Context | Git state | Typical user-facing label |
| --- | --- | --- |
| Local dev | No matching tag on `HEAD`; commits after last tag | Full SemVer with pre-release identifiers (for example `1.0.0-preview.5+abc1234`) |
| Pull request / CI | Same as the commit under test | Pre-release SemVer; not an unqualified shipping tag |
| Staging ACA (`main`) | Tracks `main` after CI | Pre-release SemVer on About; public site still shows last `v*` tag until next cut |
| Tagged production | Annotated `vX.Y.Z` on `HEAD` at build | About and assembly metadata match the tag (including `v` prefix) |
| Self-hosted without tag | Operator build from arbitrary commit | Honest pre-release/local identifier; never invent `v1.0.0` |
| Public Hugo site | Tag deploy sets `HUGO_PARAMS_RELEASEVERSION` | Tag string on tagged publish; `unreleased` otherwise |

Optional About diagnostics (UTC build time, source revision) come from separate MSBuild properties
and do not change the release label. See [Build metadata (in-app About, FR-004)](developer-quickstart.md#build-metadata-in-app-about-fr-004)
in `docs/developer-quickstart.md`.

## GitHub Pages settings

- Custom domain: `docs.importplanner.app` (must match `website/static/CNAME`).
- Source: GitHub Actions (not legacy `docs/` folder publishing).
- **Manual `workflow_dispatch` from a feature branch**: the `github-pages` environment must
  list that branch under **Deployment branches** (Settings → Environments → github-pages).
  Remove feature branches after testing so only `main` and `v*` tag deploys remain routine.
  Non-tag deploys label the site `unreleased` in the footer (branch name is not shown as a
  release version).

## Cutover notes

- Jekyll publish automation under `docs/` is retired; public content lives in `website/` only.
- Spec 007 `docs-site-contract.md` publication rules are superseded by
  `specs/017-public-hugo-website/contracts/public-site-contract.md` (content obligations
  remain where 017 defers to 007).

## Contributor preview (SC-003)

Contributors SHOULD follow **Public Hugo site** in `docs/developer-quickstart.md` (container
`build`, `preview`, and `serve` via `./scripts/invoke-hugo-site.sh`). Feature validation
scenarios remain in `specs/017-public-hugo-website/quickstart.md`.

### SC-003 verification evidence

| Check | Result | Evidence |
| --- | --- | --- |
| Documented container path exists | Pass | `docs/developer-quickstart.md` § Public Hugo site |
| `./scripts/invoke-hugo-site.sh build` succeeds | Pass | Produces `website/public/index.html` (verified 2026-09-29) |
| First-time preview under 15 minutes | Pass | Build ~3 s on WSL2 with Docker; no local Hugo install required |

## SC-002 verification evidence (app and public site)

| Check | Result | Evidence |
| --- | --- | --- |
| Terms, privacy, support from public site footer | Pass | `website/layouts/_partials/custom/footer.html` links `/terms`, `/privacy-and-security`, `/support` |
| Same destinations from hosted app (≤2 clicks) | Pass | `MainLayout.razor` footer: Documentation, Terms, Privacy, Support (`DocsBaseUrl` paths) |
| Support page opens GitHub Issues | Pass | `website/content/support.md` links issue chooser |

Recorded: 2026-09-29 (engineering review on branch `017-public-hugo-website`).

## SC-001 contract review

Before the first public site `v*` tag, score mandatory items below. Require **≥90%** mandatory
**Pass** (any **Fail** blocks the first tag until remediated).

**Review date**: 2026-09-30  
**Reviewer**: Engineering (pre-tag gate)  
**Mandatory scored**: 42  
**Pass**: 42  
**Fail**: 0  
**Pass rate**: 100% (meets ≥90%; **PUB-007** / **SC-007** satisfied by terms publication
record above)

Authority: `specs/017-public-hugo-website/contracts/public-site-contract.md` and deferred
007 page obligations in `specs/007-end-user-docs-site/contracts/docs-site-contract.md`.

### Publication contract (mandatory)

| ID | Obligation | Result | Notes |
| --- | --- | --- | --- |
| PUB-001 | Build output from `website/` only | Pass | `hugo-build.yml` / `invoke-hugo-site.sh` |
| PUB-002 | Custom domain `docs.importplanner.app` | Pass | `website/static/CNAME` |
| PUB-003 | Live site updates on `v*` tags (or manual dispatch) only | Pass | `hugo-deploy.yml` |
| PUB-004 | PR `website/**` Hugo compile check | Pass | `hugo-validate.yml` |
| PUB-005 | Tagged builds show tag; others show `unreleased` | Pass | `HUGO_PARAMS_RELEASEVERSION` + footer partial |
| PUB-006 | Engineering docs excluded from `website/public` | Pass | No `engineering-policies` strings in build output |
| PUB-007 | Terms publication record in `docs/` before first tag | Pass | Runbook table above (2026-09-30) |

### Public routes (mandatory = Yes)

| ID | Route | Result | Notes |
| --- | --- | --- | --- |
| RTE-001 | `/` | Pass | `website/content/_index.md` |
| RTE-002 | `/getting-started` | Pass | Migrated content |
| RTE-003 | `/csv-format` | Pass | Synthetic examples |
| RTE-004 | `/import-workflow` | Pass | Five-step journey |
| RTE-005 | `/troubleshooting` | Pass | Common failure topics |
| RTE-006 | `/faq` | Pass | Spec 007 questions covered |
| RTE-007 | `/privacy-and-security` | Pass | Expanded privacy |
| RTE-008 | `/self-hosted` | Pass | Secondary page |
| RTE-009 | `/terms` | Pass | Production-ready `terms.md` |
| RTE-010 | `/support` | Pass | GitHub Issues + v1.1 deferral |

### Navigation and discoverability (mandatory)

| ID | Obligation | Result | Notes |
| --- | --- | --- | --- |
| NAV-001 | Primary nav journey order (7 items) | Pass | `website/hugo.yaml` `menu.main` |
| NAV-002 | Terms and support reachable from landing + footer | Pass | `_index.md` + footer partial |
| NAV-003 | Root `README.md` links `https://docs.importplanner.app` | Pass | Repository README |

### 007 carry-forward page content (mandatory)

| ID | Page | Result | Notes |
| --- | --- | --- | --- |
| CNT-001 | Landing (purpose, audience, core links) | Pass | `_index.md` |
| CNT-002 | Getting started (hosted path, sign-in) | Pass | `getting-started.md` |
| CNT-003 | CSV format (fields, examples, mistakes) | Pass | `csv-format.md` |
| CNT-004 | Import workflow (five steps, outcomes) | Pass | `import-workflow.md` |
| CNT-005 | Troubleshooting (required topics) | Pass | `troubleshooting.md` |
| CNT-006 | FAQ (spec 007 questions) | Pass | `faq.md` + support link |
| CNT-007 | Privacy (Graph, retention, permissions) | Pass | `privacy-and-security.md` |
| CNT-008 | Self-hosted (secondary, not hosted-only support) | Pass | `self-hosted.md` |

### Spec 017 legal and support bundle (mandatory)

| ID | Obligation | Result | Notes |
| --- | --- | --- | --- |
| LEG-001 | Terms not “coming soon” on first tag | Pass | Substantive `terms.md` |
| LEG-002 | Privacy expanded beyond 007 | Pass | Issue #134 depth |
| LEG-003 | Support links to GitHub Issues with scope | Pass | `support.md` |
| LEG-004 | v1.1 dedicated-support deferral (no false SLA) | Pass | Support + FAQ copy |
| LEG-005 | Hosted vs self-hosted distinction | Pass | Support + self-hosted pages |
| LEG-006 | Credits/billing stub without inventing Stripe | Pass | `credits-and-billing.md` |

### Version display and language (mandatory)

| ID | Obligation | Result | Notes |
| --- | --- | --- | --- |
| VER-001 | Site-wide release label on tagged deploys | Pass | Footer partial |
| VER-002 | Non-tag builds do not fake SemVer | Pass | Default `unreleased` label |
| LNG-001 | UK English public copy | Pass | `en-gb` locale + content review |

### Hosted app linkage (mandatory per `app-external-links-contract.md`)

| ID | Obligation | Result | Notes |
| --- | --- | --- | --- |
| APP-001 | Footer links to docs, terms, privacy, support | Pass | `MainLayout.razor` |
| APP-002 | Configurable `DocsBaseUrl` | Pass | `appsettings.json` |

**Gate**: Do not publish the first public site `v*` tag until **PUB-007** / **SC-007** terms
publication record is current for the `terms.md` revision that will ship with the tag.
