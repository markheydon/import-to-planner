# Public site and application release runbook

This runbook covers SemVer tagging for the Import To Planner repository when the public Hugo
site and/or application ship together.

## Legal counsel sign-off (required before first public site `v*` tag)

Complete this checklist **after** external counsel approves the terms text in
`website/content/terms.md`.

| Field | Value |
| --- | --- |
| Counsel approval date | _Pending — maintainer to complete_ |
| Terms version reference | _Pending — maintainer to complete_ |
| Approver name / role | _Pending — maintainer to complete_ |

Do not publish the first public site `v*` tag until the table above is filled.

## Operational counsel workflow

1. Obtain counsel-approved terms text.
2. Update `website/content/terms.md` with the approved copy.
3. Complete the sign-off table in this runbook.
4. Merge to `main`.
5. Create and push a `vX.Y.Z` tag (see below).

## SemVer tagging and deploy

1. Update `CHANGELOG.md` under a new `## [vX.Y.Z] — YYYY-MM-DD` section.
2. Commit changelog and any final content changes on `main`.
3. Create an annotated tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`.
4. Push the tag: `git push origin vX.Y.Z`.
5. Monitor the **Hugo Deploy** workflow (`.github/workflows/hugo-deploy.yml`).
6. Verify `HUGO_PARAMS_RELEASEVERSION` matches the tag in the workflow log.
7. **SC-004**: Within one hour of a successful deploy, confirm the live site footer shows
   the new version label at `https://docs.importplanner.app`.

Ordinary merges to `main` do **not** update the live public site. PR builds label the site
`unreleased`.

## GitHub Pages settings

- Custom domain: `docs.importplanner.app` (must match `website/static/CNAME`).
- Source: GitHub Actions (not legacy `docs/` folder publishing).

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
**Pass** (counsel sign-off and any **Fail** block the first tag until remediated).

**Review date**: 2026-09-29  
**Reviewer**: Engineering (pre-tag gate)  
**Mandatory scored**: 42  
**Pass**: 41  
**Fail**: 1  
**Pass rate**: 97.6% (meets ≥90% for *documented* obligations; first `v*` tag still blocked on
**PUB-007** until counsel sign-off is recorded per **SC-007**)

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
| PUB-007 | Counsel sign-off recorded in `docs/` before first tag | **Fail** | Runbook table above still pending maintainer completion |

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
| RTE-009 | `/terms` | Pass | Production-ready copy (counsel gate separate) |
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

**Gate**: Do not publish the first public site `v*` tag until **PUB-007** / **SC-007** counsel
sign-off is complete, even though the SC-001 pass rate exceeds 90%.
