# Public Site Contract

## Scope

This contract defines the public product site built from `website/` and published to GitHub
Pages at `https://docs.importplanner.app`. It supersedes the publication source and
automation assumptions in `specs/007-end-user-docs-site/contracts/docs-site-contract.md`
while preserving end-user page coverage from spec 007 unless explicitly extended below.

Internal engineering documentation MUST live under `docs/` and MUST NOT appear in the
`website/public` artefact.

## Publication contract

- Build output MUST be produced from `website/` only.
- The custom domain MUST remain `docs.importplanner.app` (CNAME + Pages settings).
- Live production updates MUST occur only on SemVer release tags (`v*`) or documented
  manual workflow dispatch — not on every merge to `main`.
- Pull requests that change `website/**` MUST pass an automated Hugo build (compile) check.
- Tagged production builds MUST set the visible release version to the tag name; all other
  builds MUST show an explicit non-release label (default: `unreleased`).

## Public route contract

Stable routes MUST remain available (Hugo permalinks or equivalent):

| Route | Purpose | Audience | Mandatory |
| --- | --- | --- | --- |
| `/` | Product landing: purpose, audience, links to guides | Hosted end users | Yes |
| `/getting-started` | Hosted prerequisites, app access, sign-in expectations | Hosted end users | Yes |
| `/csv-format` | CSV fields, examples, priorities, mistakes | Hosted end users | Yes |
| `/import-workflow` | Import walkthrough aligned with current app steps | Hosted end users | Yes |
| `/troubleshooting` | Recovery for common failures | Hosted end users | Yes |
| `/faq` | Concise common questions | End users and administrators | Yes |
| `/privacy-and-security` | Expanded privacy and retention statement | End users and administrators | Yes |
| `/self-hosted` | Secondary self-hosted guidance | Self-hosted operators | Yes |
| `/terms` | Production-ready terms of use (counsel-approved before first site tag) | Hosted users | Yes |
| `/support` | v1.0 support path (GitHub Issues) and v1.1 deferral wording | Hosted users | Yes |
| `/credits-and-billing` | IA placeholder for future billing copy (no Stripe implementation) | Hosted users | Should (stub) |

Route rules:

- URLs MUST stay human-readable and stable across the Jekyll → Hugo cutover.
- The self-hosted route MUST NOT be required for the hosted onboarding path.
- In the route table, **Mandatory** means the route MUST be published and reachable; for
  `/self-hosted` this aligns with FR-004 (secondary page) without placing it in primary
  navigation.
- Engineering runbooks, agent policy, and Graph implementation notes MUST NOT be published
  under these routes.

## Navigation contract

Primary navigation MUST include, in journey order:

1. Home
2. Getting started
3. CSV format
4. Import workflow
5. Troubleshooting
6. FAQ
7. Privacy and security

Secondary navigation MAY include:

1. Self-hosted
2. Credits and billing (stub)

Legal and support pages (terms, support) MUST be reachable from the landing page and site
footer even if not duplicated in the primary bar.

## Page content contract (007 carry-forward)

Content obligations from spec 007 for landing, getting started, CSV format, import
workflow, troubleshooting, FAQ, privacy, and self-hosted remain in force. Additional
obligations for this feature:

### Terms

- MUST be production-ready legal text before the first public site release tag.
- MUST NOT ship as “coming soon” on that tag.
- Counsel sign-off MUST be recorded in `docs/` (checklist/runbook with date and terms
  version reference) before the tag ships.

### Privacy

- MUST expand retention and data-handling detail per issue #134 beyond the 007 baseline.

### Support

- MUST link to this repository’s GitHub Issues entry point with plain UK English scope
  (bugs, documentation corrections, product feedback).
- MUST state that dedicated end-user support is not part of v1.0 and is expected to be
  considered for v1.1 without promising a channel or date.
- MUST distinguish hosted MHCG obligations from self-hosted operator responsibilities.

### Credits and billing stub

- MUST NOT invent pricing, Stripe flows, or ledger behaviour.
- MAY state that commercial billing documentation will expand when related features ship.

## README discoverability

The repository root `README.md` MUST include a prominent link to
`https://docs.importplanner.app` for user documentation.

## Hosted app linkage

See `app-external-links-contract.md` for footer/help link requirements.

## Version display

- Tagged deploys MUST show the release tag in a site-wide visible location (footer and/or
  landing badge).
- Non-tag builds MUST NOT display a SemVer tag as if shipped.

## Language

All public site copy MUST use UK English.
