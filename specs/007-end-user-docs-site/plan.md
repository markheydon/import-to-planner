# Implementation Plan: End-User Documentation Site

**Branch**: `007-end-user-docs-site` | **Date**: 2026-05-27 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/007-end-user-docs-site/spec.md`

## Summary

Create a public end-user documentation site under `docs/` that prioritises the
hosted user journey, mirrors the app's verified CSV/import/support behaviour,
and publishes automatically to `docs.importplanner.app` through GitHub Pages.
The implementation remains documentation- and workflow-focused: author the
public Markdown pages, add minimal site/navigation configuration, add the Pages
deployment artefacts, introduce a clearly secondary self-hosted page, and add a
prominent root README link to the live documentation.

## Technical Context

**Language/Version**: Markdown plus YAML for GitHub Actions and Pages config; repository context remains .NET 10 / C# 14 for sourcing accurate product behaviour  
**Primary Dependencies**: GitHub Pages, GitHub Actions Pages deployment actions or equivalent Pages branch configuration, existing `docs/` public-content policy, verified app/test wording in `ImportToPlanner.Web` and tests  
**Storage**: N/A for application data; static documentation files published from the repository plus GitHub Pages deployment metadata  
**Testing**: Manual Markdown/render review, GitHub Pages deployment verification, README/docs link verification, mobile/desktop browser review; no existing repository docs test harness  
**Target Platform**: Public static docs site served from GitHub Pages and viewed in desktop/mobile browsers  
**Project Type**: Documentation site and repository automation update inside an existing Blazor application repository  
**Performance Goals**: Static pages should load quickly with no client-side app boot dependency; navigation and content should remain readable on a 375 px mobile viewport and standard desktop widths  
**Constraints**: Public docs only in `docs/`; UK English throughout; hosted end users are the primary audience; self-hosted guidance must live on a separate secondary page; screenshots are out of scope for the initial release; no internal implementation detail, secrets, or tenant-sensitive diagnostics may be published  
**Scale/Scope**: Seven primary public pages from issue #11, one optional secondary self-hosted page, supporting Pages config/deployment artefacts, and a root README update

## Constitution Check

*GATE: Pre-phase assessment passes. Re-checked after Phase 1 design below.*

- **Dependency Direction Gate**: Pass. Planned changes stay in repository docs,
  workflow config, and README surfaces. No Domain/Application/Web dependency
  graph changes are required.
- **Core Policy Neutrality Gate**: Pass. End-user wording is sourced from
  existing outward-facing behaviour without moving provider-specific logic into
  inner layers.
- **Boundary Explicitness Gate**: Pass. The docs site consumes already-exposed
  UI and configuration behaviour as documentation content only; it does not add
  new application-layer contracts.
- **Replaceability Gate**: Pass. GitHub Pages is a delivery mechanism for the
  docs site, not a new architectural dependency for the app runtime.
- **Architecture Evidence Gate**: Pass. Because this feature does not alter app
  code paths, evidence is documentation/publication focused: route coverage,
  public/private content separation, README linkage, and deployment verification.
- **Policy Alignment Gate (Non-Constitutional)**: Pass. The plan preserves UK
  English, keeps internal content in `docs-internal/`, avoids raw operational
  detail in public pages, and maintains responsive/mobile review as an explicit
  acceptance activity.

## Project Structure

### Documentation (this feature)

```text
specs/007-end-user-docs-site/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── docs-site-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
docs/
├── README.md                          ← existing public-content policy note
├── index.md                           ← public landing page
├── getting-started.md                 ← hosted-user onboarding
├── csv-format.md                      ← CSV schema and examples
├── import-workflow.md                 ← step-by-step workflow guide
├── troubleshooting.md                 ← support scenarios and recovery actions
├── faq.md                             ← concise common questions
├── privacy-and-security.md            ← Graph/data/permission summary
├── self-hosted.md                     ← secondary self-hosted guidance
├── _config.yml                        ← minimal GitHub Pages/Jekyll site config
└── CNAME                              ← custom domain declaration for Pages

.github/workflows/
└── docs-pages.yml                     ← GitHub Pages build/deploy automation

README.md                              ← prominent User Documentation link
```

**Structure Decision**: Keep implementation entirely in repository documentation
and Pages automation surfaces. This avoids application-code churn, preserves
clean architecture boundaries, and is the smallest change that can satisfy the
public docs, deployment, and discoverability requirements together.

## Complexity Tracking

No constitution violations are planned. The work stays in documentation and CI
automation surfaces and does not require new runtime services, new application
contracts, or cross-layer refactoring.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Publish the site from `docs/` using GitHub Pages with minimal repository
   configuration and deployment automation rather than introducing a separate
   docs framework.
2. Keep `docs/README.md` as the public-folder policy note and add a dedicated
   `docs/index.md` landing page for end users.
3. Base end-user page content on verified application/test behaviour for CSV
   columns, priority mapping, duplicate handling, manual actions, and consent
   flows so public docs stay aligned with the product.
4. Keep self-hosted guidance on a separate secondary page linked from the main
   docs rather than mixing it into the hosted getting-started flow.
5. Omit screenshots in the initial release and compensate with explicit,
   text-first workflow guidance because safe demo data is not yet available.
6. Verify the feature through documentation route/content checks, Pages
   deployment evidence, README linkage, and desktop/mobile review instead of
   introducing a new docs test harness.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md),
and [contracts/docs-site-contract.md](contracts/docs-site-contract.md).

Key design outcomes:

- The docs site uses one landing page plus task-oriented guide pages that map
  directly to the spec's primary user stories.
- Navigation is intentionally shallow: landing page, core guides, and a clearly
  secondary self-hosted page.
- Content sections are standardised so each page answers one primary user need
  without leaking internal implementation detail.
- Publication configuration is explicit in-repo through Pages artefacts and a
  custom-domain declaration rather than relying on undocumented repository
  settings alone.
- Verification focuses on public-content boundaries, link discoverability,
  render quality, and deployment success rather than application runtime tests.

### Architecture impact statement

- **Dependency direction**: Unchanged. The feature does not alter project
  references or runtime dependency flow.
- **Boundary changes**: None across Domain/Application/Web layers. The only new
  "contract" is documentation-specific and lives in planning artefacts.
- **Adapter responsibilities**: Repository docs continue to own public wording;
  GitHub Pages/GitHub Actions own docs delivery; app runtime adapters remain
  untouched.

### Post-design Constitution Check

- **Dependency Direction Gate**: Pass. No inward dependency changes are planned.
- **Core Policy Neutrality Gate**: Pass. Public wording documents existing app
  behaviour without introducing provider-specific policy into inner layers.
- **Boundary Explicitness Gate**: Pass. Docs artefacts and Pages automation stay
  outside core use-case boundaries.
- **Replaceability Gate**: Pass. Pages automation is isolated to delivery and
  can be swapped later without affecting app architecture.
- **Architecture Evidence Gate**: Pass. Quickstart verification defines the
  required evidence for public/private content separation, route coverage,
  deployment success, and responsive review.
- **Policy Alignment Gate**: Pass. The design preserves UK English, public-docs
  separation, hosted-user primacy, and privacy-safe wording.
