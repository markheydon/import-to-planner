# Data Model: In-App Release Version and About Surface

## Scope

Structured models for deployment release labelling, optional build diagnostics shown on About,
the About page view contract, public guide release applicability (FR-007), and maintainer
verification artefacts. No new persistence stores; values come from build metadata and
configuration at runtime.

## Entities

### DeploymentReleaseLabel

- Purpose: Human-readable SemVer string shown in About and quoted in support (FR-003, FR-005).
- Fields:
  - `DisplayValue`: string shown to users (for example `v1.0.0` or `1.0.0-preview.3+abc1234`).
  - `IsOfficialReleaseTag`: whether the deployment was built from an annotated `vX.Y.Z` tag.
  - `NormalisedSemVer`: parsed SemVer without leading `v` where applicable (for comparisons).
- Validation rules:
  - Official tag builds MUST set `DisplayValue` to match the tag name (including `v` prefix if
    that is the repository tag convention).
  - Non-tag builds MUST include pre-release identifiers in `DisplayValue` and MUST NOT present
    an unqualified shipping release unless `ReleaseLabelPolicy` explicitly permits it.
  - Comparisons for three-way verification use tag name ↔ public `releaseVersion` ↔ About
    `DisplayValue` (normalise `v` prefix consistently in runbook).

### ReleaseLabelPolicy

- Purpose: Environment-specific guardrails when a bare shipping label is allowed (edge case in
  FR-005 / SC-003).
- Fields:
  - `AllowUnqualifiedShippingLabel`: bool (default `false`).
  - `EnvironmentName`: optional diagnostic label (for example `Staging`, `Production`).
- Validation rules:
  - Default for local, CI, staging, and production MUST be `false` unless documented exception.
  - Policy MUST be configuration-only, not hard-coded per developer machine.

### BuildMetadata

- Purpose: Optional diagnostics on About (FR-004, user story 4).
- Fields:
  - `BuiltAtUtc`: nullable `DateTimeOffset`.
  - `SourceRevisionId`: optional short SHA or full commit id.
  - `SourceRevisionUrl`: optional link to repository commit (when base URL configured).
- Validation rules:
  - If a field is unknown, omit the UI row — do not show “Unknown” or fabricated values.
  - MUST NOT include tenant ids, tokens, or secrets.

### ReleaseInformation

- Purpose: Aggregate returned across the Application boundary for About rendering.
- Fields:
  - `ProductName`: fixed product string (for example “Import To Planner”).
  - `ReleaseLabel`: `DeploymentReleaseLabel`.
  - `BuildMetadata`: optional `BuildMetadata`.
- Validation rules:
  - `ProductName` wording is supplied to the presenter; interactors return structured data only
    (constitution guardrail on prose).

### AboutPageLinks

- Purpose: External destinations reused from footer/help contracts (FR-008).
- Fields:
  - `DocsHomeUrl`, `TermsUrl`, `PrivacyUrl`, `SupportUrl` (same composition rules as
    `DocsExternalLinksOptions` / spec 017 `LegalAndSupportBundle`).
- Validation rules:
  - URLs MUST match `app-external-links-contract.md` paths for a given `DocsBaseUrl`.
  - About MUST NOT introduce alternate legal/support URLs.

### PublicGuideReleaseContext

- Purpose: Hugo-side applicability of documentation to a product release (FR-007, SC-005).
- Fields:
  - `ReleaseVersionLabel`: mirrors `SitePublicationSettings.ReleaseVersionLabel` from spec 017
    (`vX.Y.Z` or `unreleased`).
  - `ApplicabilityCopy`: UK English sentence template for hero/above-fold partial.
  - `Surfaces`: set including `SiteFooter`, `LandingHero`, `ImportWorkflowIntro` (minimum for
    gap-fill).
- Validation rules:
  - Tagged publish: all surfaces show the same tag string.
  - Non-tag build: surfaces show unreleased / non-production wording, never a fake tag.

### ReleaseVerificationChecklist

- Purpose: Maintainer artefact extended in `docs/release-runbook.md` (FR-009, SC-002).
- Fields:
  - `TagName`: GitHub Release / git tag.
  - `PublicSiteLabel`: observed on live docs site.
  - `InAppAboutLabel`: observed on production (or tagged) app About page.
  - `VerifiedAtUtc`, `Verifier`, `Result`: Pass | Fail.
- Validation rules:
  - Tagged production release cannot be marked complete with any mismatch.

## Relationships

```text
ReleaseInformation
  ├── DeploymentReleaseLabel (1:1)
  └── BuildMetadata (0:1)

AboutPageLinks ──uses──> DocsBaseUrl configuration (spec 017)

PublicGuideReleaseContext ──aligns──> DeploymentReleaseLabel (on tagged releases only)

ReleaseVerificationChecklist ──references──> TagName, PublicSiteLabel, InAppAboutLabel
```

## State transitions

- **Build pipeline**: git state → MSBuild/MinVer → assembly attributes →
  `ReleaseInformation` at runtime (no user-triggered transitions).
- **Release cut**: maintainer creates tag → Hugo deploy sets `releaseVersion` → app deploy
  from tag → checklist Pass when three labels match.
