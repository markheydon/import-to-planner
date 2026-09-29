# Quickstart: Public Product Website and Demo Mode Validation

This guide validates the feature end-to-end after implementation. It references
`contracts/public-site-contract.md`, `contracts/app-external-links-contract.md`, and
`contracts/demo-mode-contract.md` without duplicating full page copy.

## Prerequisites

- Git checkout of branch `017-public-hugo-website` with implementation complete.
- Docker or Podman for Hugo site commands (primary path).
- Optional: .NET 10 SDK for app and test validation.
- For demo mode: a non-production deployment (or local dev host) with demo controls enabled
  and an allowlisted operator account.
- For first production site tag: legal counsel sign-off recorded in `docs/` per
  `public-site-contract.md`.

## 1. Local public site build and preview

From repository root:

```bash
./scripts/invoke-hugo-site.sh build
```

**Expected**: `website/public/index.html` exists; command exits 0.

```bash
./scripts/invoke-hugo-site.sh preview
```

**Expected**: Static site served on documented port (default `8080`); browser shows landing
page with `unreleased` (or equivalent) version label, not a fake SemVer tag.

Optional live reload:

```bash
./scripts/invoke-hugo-site.sh serve
```

**Expected**: Dev server on port `1313`; edits under `website/content/` reload.

## 2. Route and navigation coverage

With preview or serve running, manually verify routes listed in
`public-site-contract.md` resolve (no 404 for mandatory pages).

**Expected**:

- Primary nav order matches contract.
- Terms, privacy, support, and self-hosted pages reachable.
- Credits/billing stub present if implemented as Should.
- UK English spelling on sampled pages.

Compare CSV format and import workflow pages against current app behaviour (priorities,
columns, report outcomes).

## 3. Internal vs public separation

```bash
test ! -f docs/_config.yml
test ! -f docs/CNAME
```

After cutover **Expected**: Jekyll artefacts removed from `docs/`; engineering files present
under `docs/` (formerly `docs-internal/`). Hugo source only under `website/`.

Search built output:

```bash
rg -l "engineering-policies|AppHost" website/public || true
```

**Expected**: No internal runbook content in public artefact.

## 4. Pull request Hugo validate (CI)

Open or simulate a PR changing `website/content/index.md`.

**Expected**: `hugo-validate` job runs and passes; no Pages deploy.

Introduce a deliberate Hugo syntax error.

**Expected**: Job fails and blocks merge (SC-006).

## 5. Tag-only Pages deploy

Create a test SemVer tag in a controlled environment (or dry-run workflow with deploy
inputs).

**Expected**:

- Deploy workflow runs on `v*` tag only.
- Merging to `main` without a tag does not update live site within SC-004 window.
- Published site footer/badge shows tag name (for example `v1.0.0`).

## 6. Root changelog and release runbook

**Expected**:

- `CHANGELOG.md` exists at repository root (Keep a Changelog sections).
- `docs/` contains release runbook steps for tagging and legal sign-off checklist before
  first public site release.

## 7. Repository entry points

Inspect `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, `.github/pull_request_template.md`, and
`.github/skills/end-user-docs/SKILL.md`.

**Expected**: Public docs path is `website/` (or published URL); engineering guidance points
to `docs/`; no instruction to publish end-user content from Jekyll on main.

## 8. Hosted app external links

Run the web app against staging or local configuration with default docs base URL.

**Expected** (within two clicks from home/import view):

- Terms, privacy, support, and documentation links open correct `docs.importplanner.app`
  paths.
- Support page leads to GitHub Issues.

## 9. Demo screenshot privacy review (before publishing workflow images)

Complete this checklist **before** committing or publishing screenshots in
`website/content/import-workflow.md` (or related public guides). Aligns with **SC-005**.

- [ ] Screenshots captured only while demo mode was active for an allowlisted operator on a
  non-production deployment with demo controls enabled.
- [ ] Visual review: no real Microsoft 365 tenant names, user identifiers, or production URLs
  in any image.
- [ ] Visual review: no real CSV filenames, cell values, or task titles from live sessions.
- [ ] Engineering verification: no Microsoft Graph or real upload processing occurred during
  the capture walkthrough (logs, tests, or diagnostics).
- [ ] If any item fails, discard images and re-capture under demo mode; do not publish.

## 10. Demo mode synthetic journey

Configure allowlist with your operator account; enable demo controls on non-production.

1. Sign in — **Expected**: demo off; normal Graph path available.
2. Enable demo toggle — **Expected**: synthetic data; indicator visible; no real tenant
   names in UI.
3. Complete import walkthrough — **Expected**: no Graph/upload calls (verify via logs,
   tests, or diagnostics flag).
4. Sign out and sign in — **Expected**: demo off.
5. Sign in as non-allowlisted user — **Expected**: no toggle.

With demo controls disabled on production-like config — **Expected**: no toggle for any user.

## 11. Legal gate before first public site tag

**Expected**: `docs/` runbook entry documents counsel sign-off date and terms version; terms
page in `website/` is not placeholder text.

## Failure triage

| Symptom | Likely cause |
| --- | --- |
| Hugo build fails on CI | Missing extended Hugo/Sass/Go module fetch; check `hugo-build.yml` |
| Live site updates on main merge | Old Jekyll workflow still enabled |
| 404 on legacy paths | Permalink migration incomplete |
| Demo shows real tenant data | Demo fixtures leaking live session state |
| Graph calls during demo | Adapter guard missing — contract violation |
| `invoke-hugo-site.sh` exits immediately with runtime error | Docker or Podman not installed or not on PATH; pass `--runtime` explicitly |
