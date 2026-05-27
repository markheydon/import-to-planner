# Quickstart: End-User Documentation Site Verification

## Purpose

Use this guide after implementation to verify that the public docs site covers
the required end-user journeys, stays separate from internal docs, and
publishes correctly to `docs.importplanner.app`.

## Prerequisites

- Repository changes for the feature are available locally.
- GitHub Pages is enabled for the repository or the Pages deployment workflow is
  configured.
- A reviewer can access the generated site in a desktop browser and a mobile
  viewport.

## Verification Checkpoints

| Checkpoint | Command or action | Evidence to capture |
| --- | --- | --- |
| Public page inventory | Review the files under `docs/` | All required public pages exist and the self-hosted page is clearly secondary |
| Public/private boundary | Read `docs/README.md` and spot-check the new pages | Internal/developer guidance remains in `docs-internal/` and public pages stay end-user focused |
| README discoverability | Open the repository root README | A prominent `User Documentation` link points to `https://docs.importplanner.app` |
| CSV/workflow accuracy | Compare docs wording against verified app/test behaviour | CSV headers, priority values, duplicate handling, manual actions, and support guidance match the product |
| Local content review | Use VS Code Markdown preview for each changed page | Headings, tables, and code examples render cleanly in Markdown |
| Deployment automation | Inspect the Pages workflow/configuration after implementation | Publication is automatic on `main` and the custom-domain artefacts are present |
| Published desktop review | Open `https://docs.importplanner.app` in a desktop browser after deployment | Landing page, navigation, and guide content render correctly |
| Published mobile review | View the site at approximately 375 px width | Navigation and page content remain readable without layout breakage |

## Recommended Verification Order

### 1. Review the repository surface first

Suggested checks:

1. Confirm `docs/index.md`, `docs/getting-started.md`, `docs/csv-format.md`,
   `docs/import-workflow.md`, `docs/troubleshooting.md`, `docs/faq.md`, and
   `docs/privacy-and-security.md` exist.
2. If `docs/self-hosted.md` exists, confirm it is linked as secondary guidance.
3. Confirm `docs/README.md` still describes the public-content boundary.

### 2. Review content accuracy before publication

Suggested checks:

1. Confirm the CSV page names `Task Name` as required and lists the accepted
   fields.
2. Confirm priority guidance covers numeric `0-10` and the text values
   `Urgent`, `Important`, `Medium`, and `Low`.
3. Confirm the import workflow page explains the five steps and the created,
   reused, and skipped outcomes.
4. Confirm troubleshooting covers sign-in, admin consent, `No groups found`,
   validation failures, duplicate handling, and temporary service issues.
5. Confirm the privacy page distinguishes imported data from limited operational
   logs or telemetry.

### 3. Verify discoverability and publication configuration

Suggested checks:

1. Confirm the root README includes a `User Documentation` section linking to
   `https://docs.importplanner.app`.
2. Confirm the repository contains the Pages deployment artefacts and custom
   domain declaration.
3. After merge to `main`, confirm an automatic publication occurs without a
   manual upload step.

### 4. Perform final render checks

Suggested checks:

1. Open the published site on desktop and confirm the landing page links to all
   primary guides.
2. Repeat at mobile width and confirm headings, tables, and navigation remain
   legible.
3. Confirm no screenshots are required for the first release and the content is
   still understandable without them.

## Review Checklist

- The public docs live in `docs/` and remain separate from `docs-internal/`.
- The landing page is written for end users rather than contributors.
- Hosted-user guidance is the primary journey.
- Self-hosted guidance, if included, is clearly secondary.
- CSV examples use safe illustrative data only.
- Privacy and security wording is accurate and non-alarmist.
- The README link to `docs.importplanner.app` is present and clear.
- The published site reads well on desktop and mobile.

## Verification Evidence

- Automated verification: not repository-standard for this feature beyond Pages
  deployment success.
- Manual verification: required for public wording, responsive rendering, and
  link discoverability.

Recommended evidence to record:

1. Screenshot-free page previews in Markdown review.
2. Successful Pages deployment run.
3. Desktop and mobile browser review notes for the live site.
