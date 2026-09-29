# Data Model: Public Product Website, Documentation Layout, and Release Versioning

## Scope

This feature combines a static public site model, repository documentation layout,
release metadata, legal/support content, and in-app demo mode state. Demo mode introduces
Application-visible session state and configuration; it does not add commercial billing
entities.

## Entities

### PublicSitePage

- Purpose: One end-user-facing page in the Hugo site under `website/content/`.
- Fields:
  - `Slug`: URL path segment (stable across cutover from Jekyll).
  - `Title`: navigation and `<title>` label.
  - `Audience`: `HostedEndUser`, `Administrator`, `SelfHostedSecondary`, or `Legal`.
  - `PrimaryGoal`: task or question the page answers.
  - `RequiredSections`: ordered section intents (see `PublicSiteSection`).
  - `SourceOfTruth`: app/tests/specs anchoring accuracy (especially CSV and workflow pages).
  - `InPrimaryNav`: whether the page appears in main menu.
  - `InSecondaryNav`: optional (for example self-hosted, billing stub).
- Validation rules:
  - Each mandatory route in the public site contract maps to exactly one page.
  - Legal pages must not ship as placeholders on first `v*` tag (terms require counsel
    sign-off record in engineering docs).
  - UK English throughout.

### PublicSiteSection

- Purpose: Content block within a public page.
- Fields:
  - `Heading`: visible title.
  - `SectionType`: `Overview`, `Prerequisites`, `StepList`, `Table`, `Example`, `FAQ`,
    `Troubleshooting`, `PrivacyNotice`, `LegalTerms`, `SupportPath`, or `BillingStub`.
  - `Intent`: user-facing purpose.
  - `ContentSource`: repository behaviour or requirement reference.
  - `Optional`: whether omission fails the contract.
- Validation rules:
  - Examples use synthetic CSV/tasks only.
  - Support sections must name GitHub Issues as v1.0 path and defer dedicated support to
    v1.1 without promising dates.
  - Self-hosted pages must not instruct users to use hosted-only support as if MHCG operates
    their deployment.

### NavigationItem

- Purpose: Menu entry in Hextra `menu.main` (and optional secondary links).
- Fields:
  - `Label`: short UK English label.
  - `TargetPath`: public URL path.
  - `Tier`: `Primary` or `Secondary`.
  - `Weight`: sort order.
- Validation rules:
  - Primary order follows onboarding journey: home → getting started → format → workflow →
    troubleshooting → FAQ → privacy (terms/support may appear in footer or legal cluster).
  - Secondary items visibly separated (self-hosted, billing stub).

### SitePublicationSettings

- Purpose: Hugo/site configuration governing build and deploy.
- Fields:
  - `BaseURL`: `https://docs.importplanner.app/`.
  - `CustomDomain`: `docs.importplanner.app` (`static/CNAME`).
  - `ReleaseVersionLabel`: SemVer tag string or `unreleased`.
  - `PublishTrigger`: `ReleaseTagOnly` | `ManualDispatch`.
  - `OutputPath`: `website/public/`.
- Validation rules:
  - Live site updates only from tagged deploys (FR-010).
  - Non-tag builds must set `ReleaseVersionLabel` to `unreleased` (FR-011).

### InternalEngineeringDoc

- Purpose: Contributor/operator material under `docs/` (migrated from `docs-internal/`).
- Fields:
  - `Path`: file path under `docs/`.
  - `Category`: `Policy`, `Runbook`, `GraphGuidance`, `Release`, `LegalChecklist`, etc.
  - `PublishedToEndUsers`: always `false`.
- Validation rules:
  - Must not be copied into `website/content/` except where public pages intentionally
    summarise end-user-safe subsets (privacy/terms are authored for the public site, not
    pasted from runbooks).
  - Release runbook must include counsel sign-off checklist entry (SC-007).

### LegalAndSupportBundle

- Purpose: Consistent set of public legal and support destinations linked from site and app.
- Fields:
  - `TermsURL`: public terms page on custom domain.
  - `PrivacyURL`: public privacy page.
  - `SupportURL`: GitHub Issues entry for this repository.
  - `CounselSignOffRecord`: pointer to internal checklist entry (date + terms version id).
- Validation rules:
  - App footer/help links must match bundle URLs (modulo configurable docs base for
    self-hosted).
  - Terms page blocked from first public release tag until sign-off record exists.

### ReleaseVersionLabel

- Purpose: Human-readable version shown on the public site.
- Fields:
  - `Value`: SemVer tag (for example `v1.0.0`) or `unreleased`.
  - `Source`: `GitTag` | `LocalDefault`.
- State transitions:
  - PR/local build → `unreleased`.
  - Tag deploy workflow → tag name.
- Validation rules:
  - Tagged deploys must show non-empty SemVer in footer or badge.
  - Must not imply a shipped tag when `Value` is `unreleased`.

### DemoModeSession

- Purpose: Per signed-in session flag controlling synthetic import journey.
- Fields:
  - `IsActive`: boolean.
  - `ActivatedAt`: timestamp (diagnostics only, not user-facing).
  - `OperatorIdentity`: resolved Entra object id and/or UPN after sign-in.
- State transitions:
  - Sign-in → `IsActive = false` (default).
  - Allowlisted operator toggles on (if deployment permits) → `IsActive = true`.
  - Toggle off → `IsActive = false`.
  - Sign-out → `IsActive = false` (forced).
- Validation rules:
  - When `IsActive`, import workflow MUST NOT invoke Microsoft Graph or real upload
    processing for import steps.
  - Non-allowlisted users never receive toggle UI.
  - Production deployments hide toggle unless policy explicitly enables operator demo
    controls.

### DemoModeDeploymentPolicy

- Purpose: Configuration bound at host startup (hosted and self-hosted).
- Fields:
  - `OperatorAllowlist`: set of Entra object IDs and/or normalised UPNs.
  - `DemoControlsEnabled`: boolean (false on production by default).
  - `PublicDocsBaseUrl`: base for footer links (default production docs URL).
- Validation rules:
  - Tenant administrator roles MUST NOT substitute for allowlist membership.
  - Self-hosted operators configure the same keys as hosted (constitution X).

### DemoDataset

- Purpose: In-memory synthetic fixtures while demo mode is active.
- Fields:
  - `Groups`, `Plans`, `Buckets`, `Tasks`, `CsvSample`: illustrative names and rows.
  - `Labels`: markers that UI copy identifies data as demonstration-only.
- Validation rules:
  - No production tenant names, object IDs, or real CSV uploads.
  - Fixtures must drive the full import journey screens (group selection through report).

### ChangeLogEntry

- Purpose: Root `CHANGELOG.md` record for SemVer releases affecting app and/or public site.
- Fields:
  - `Version`, `Date`, `Sections` (`Added`, `Changed`, `Fixed`, etc.).
- Validation rules:
  - Keep a Changelog format; site release tags should correspond to documented versions.

## Relationships

- `PublicSitePage` contains many `PublicSiteSection`.
- `NavigationItem` references one `PublicSitePage` by `TargetPath`.
- `SitePublicationSettings` supplies `ReleaseVersionLabel` to all built pages via Hugo params.
- `LegalAndSupportBundle` URLs are referenced by `PublicSitePage` (legal/support) and app
  external link configuration.
- `DemoModeSession` consumes `DemoModeDeploymentPolicy` for authorisation and selects
  `DemoDataset` instead of live adapters when active.
- `InternalEngineeringDoc` entries may reference `LegalAndSupportBundle` sign-off but are
  not published to the Hugo artefact.

## Migration notes (007 → 017)

| Former location | New location | Notes |
| --- | --- | --- |
| End-user Markdown in `docs/*.md` | `website/content/**` | Preserve public URLs |
| Jekyll `_config.yml`, `CNAME` in `docs/` | `website/hugo.yaml`, `website/static/CNAME` | Retire Jekyll |
| `docs-internal/**` | `docs/**` | Engineering only |
| Spec 007 docs contract | Spec 017 `public-site-contract.md` | Publishing rules change |
