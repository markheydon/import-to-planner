# Data Model: End-User Documentation Site

## Scope

This feature does not add application-domain entities or persistence models. It
introduces a repository-owned documentation model that defines the public pages,
their content sections, navigation relationships, and publication settings.

## Entities

### DocsPage

- Purpose: Represents one public page in the end-user docs site.
- Fields:
  - `Slug`: route segment such as `getting-started` or `csv-format`.
  - `Title`: page heading shown in navigation and browser title.
  - `Audience`: `HostedEndUser`, `Administrator`, or `SelfHostedSecondary`.
  - `PrimaryGoal`: the main question or task the page resolves.
  - `RequiredSections`: ordered list of sections that must appear on the page.
  - `SourceOfTruth`: repository surfaces that the page content must align with,
    such as specs, tests, or configuration.
  - `Published`: whether the page participates in the public navigation.
- Validation rules:
  - Each public route must map to exactly one page.
  - `Slug` values must be stable, human-readable, and suitable for public URLs.
  - Pages for the primary hosted-user journey must not depend on the
    self-hosted page to be understandable.

### DocsSection

- Purpose: Represents one content block within a page.
- Fields:
  - `Heading`: visible section title.
  - `SectionType`: `Overview`, `Prerequisites`, `StepList`, `Table`, `Example`,
    `FAQ`, `Troubleshooting`, `PrivacyNotice`, or `SupportNote`.
  - `Intent`: user-facing purpose of the section.
  - `ContentSource`: the repo behaviour or requirement that anchors the section.
  - `Optional`: whether the section is mandatory for the page contract.
- Validation rules:
  - Sections must remain task-oriented and avoid internal implementation prose.
  - Example sections must use safe illustrative data only.
  - Troubleshooting sections must describe corrective action, not raw exception
    detail.

### NavigationItem

- Purpose: Represents one item in the docs site's primary or secondary
  navigation.
- Fields:
  - `Label`: short navigation label.
  - `TargetSlug`: destination page slug.
  - `Order`: display position.
  - `Group`: `Primary`, `Support`, or `Secondary`.
  - `AudiencePriority`: `Primary` or `Secondary`.
- Validation rules:
  - The landing page and core hosted-user guides must be in the primary group.
  - Self-hosted guidance must remain in the secondary group.
  - Labels must remain short and plain-language.

### PublicationProfile

- Purpose: Captures how the public docs site is published.
- Fields:
  - `SourceDirectory`: `docs/`.
  - `EntryPage`: `index.md`.
  - `CustomDomain`: `docs.importplanner.app`.
  - `DeploymentTrigger`: changes merged to `main`.
  - `AutomationSurface`: GitHub Pages configuration and repository workflow.
  - `PublicBoundaryRule`: `docs/` content must be safe for public publication.
- Validation rules:
  - Publication must not require a manual per-release content upload.
  - The custom domain declaration must match the published site URL.
  - Internal-only material must never be included in the published output.

### SupportScenario

- Purpose: Represents a user problem that must be covered by troubleshooting,
  FAQ, or privacy/security pages.
- Fields:
  - `ScenarioKey`: identifier such as `sign-in-consent` or `no-groups-found`.
  - `UserFacingSymptom`: what the user sees.
  - `LikelyCause`: concise explanation suitable for end users.
  - `RecoveryAction`: next steps the docs should recommend.
  - `CoveragePage`: page responsible for explaining it.
- Validation rules:
  - All issue #11 support scenarios must have at least one mapped coverage page.
  - Recovery actions must be actionable and human-friendly.
  - No scenario description may expose secret, tenant-sensitive, or raw
    diagnostic detail.

## Relationships Overview

- A `DocsPage` contains one or more `DocsSection` entries.
- `NavigationItem` references a `DocsPage` through `TargetSlug`.
- `PublicationProfile` publishes the collection of public `DocsPage` routes.
- `SupportScenario` is documented by one or more `DocsPage` entries,
  especially `troubleshooting`, `faq`, and `privacy-and-security`.

## Coverage Mapping

- `index` covers product purpose, audience, and page discovery.
- `getting-started` covers prerequisites, hosted URL access, and sign-in
  expectations.
- `csv-format` covers field definitions, examples, priority mapping, and common
  formatting errors.
- `import-workflow` covers the five-step flow, preview/execute expectations, and
  created/reused/skipped outcomes.
- `troubleshooting` covers sign-in, permissions, group membership, validation,
  duplicates, and temporary service issues.
- `faq` covers concise repeat questions that do not warrant full guide pages.
- `privacy-and-security` covers Graph data usage, limited telemetry/logging,
  credential handling, and delegated permissions.
- `self-hosted` covers secondary deployment guidance without intruding on the
  primary hosted-user journey.

## State Transitions

### Publication state

1. Markdown content is authored or updated under `docs/`.
2. A change reaches `main` and triggers automatic Pages publication.
3. The published site eventually reflects the committed content at
   `docs.importplanner.app`.

### Content maturity state

1. Core hosted-user pages are required for the first release.
2. Secondary self-hosted guidance is linked but remains clearly out of the main
   onboarding path.
3. Screenshots may be added in a later feature once safe demo data exists.

## Mapping Boundaries

- Public docs must be derived from outward-facing repo behaviour and approved
  requirements, not internal runbooks.
- Internal contributor and operational guidance remains in `docs-internal/`.
- The docs site is a delivery surface only; it does not become a source of truth
  for core application policy.
