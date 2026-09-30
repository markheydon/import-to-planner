# Quickstart: In-App About and Release Version Validation

Runnable checks after implementation. Details live in
`contracts/about-page-ui-contract.md`, `contracts/release-version-contract.md`, and
`data-model.md`.

## Prerequisites

- Branch `018-in-app-about-version` with implementation complete.
- .NET 10 SDK and Docker/Podman (for Hugo partial validation).
- Local or staging app with Microsoft sign-in configured.
- Optional: tagged test release `v0.0.0-test` in a fork (for three-way checklist dry run).

## 1. Unit tests — release labelling

From repository root:

```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj --filter "FullyQualifiedName~Release"
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --filter "FullyQualifiedName~About"
```

**Expected**: All filtered tests pass; formatter/policy cases cover tag, pre-release, and
missing metadata per `release-version-contract.md`.

## 2. Local non-tag build — honest pre-release label

```bash
dotnet build ImportToPlanner.slnx
# Run web project via Aspire or documented local host
```

**Expected**:

- About (signed in) shows a SemVer string **with** pre-release identifiers, not bare `v1.0.0`.
- Optional build metadata rows absent locally unless MSBuild properties are set.

## 3. About page — auth and content (manual or bUnit-backed)

1. Open `/about` while signed out.
2. **Expected**: Sign-in prompt or redirect; no release label visible.
3. Sign in, open About from **footer** link.
4. **Expected**: Product name, release label, external doc/legal/support links matching footer URLs.
5. Open About from **Help** menu on import home.
6. **Expected**: Same page within three navigation actions from `/` (SC-001).

## 4. Footer and help entry points

On signed-in import workflow view:

**Expected**:

- Footer includes About alongside Documentation, Terms, Privacy, Support.
- Help menu includes About plus documentation/support paths per
  `app-external-links-amendment.md`.

## 5. Public site — guide applicability (FR-007)

Build Hugo site locally:

```bash
./scripts/invoke-hugo-site.sh build
```

Inspect `website/public/index.html` and import workflow page HTML.

**Expected**:

- Footer still shows unreleased (or tag when `HUGO_PARAMS_RELEASEVERSION` set).
- Landing and import workflow show above-fold applicability copy using the same version parameter
  (SC-005 sampling).

Simulate tagged build:

```bash
HUGO_PARAMS_RELEASEVERSION=v9.9.9-test ./scripts/invoke-hugo-site.sh build
```

**Expected**: Footer and hero/import-workflow callouts reference `v9.9.9-test` consistently.

## 6. Tagged release dry run (maintainer)

Follow extended steps in `docs/release-runbook.md`:

1. Create test annotated tag (non-production environment acceptable).
2. Run Hugo deploy workflow / local equivalent with tag name injected.
3. Deploy app from the **same** tag (documented hosted/self-hosted path).
4. Complete three-way checklist: GitHub Release name, public site label, About label.

**Expected**: Checklist Pass with matching strings; Fail documented if any mismatch.

## 7. Format gate

```bash
dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal
```

**Expected**: Exit 0 (AGENTS.md policy).
