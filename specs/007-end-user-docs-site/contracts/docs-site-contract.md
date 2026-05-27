# Docs Site Contract

## Scope

This contract defines the public documentation site exposed from `docs/` for
end users of Import To Planner. It governs public routes, navigation structure,
page content obligations, README discoverability, and publication behaviour.

## Public Route Contract

The site MUST expose the following public routes:

| Route | Purpose | Audience | Mandatory |
| --- | --- | --- | --- |
| `/` | Landing page summarising the app and linking to core guides | Hosted end users | Yes |
| `/getting-started` | Hosted-user prerequisites, app access, and sign-in expectations | Hosted end users | Yes |
| `/csv-format` | Supported CSV fields, examples, priority values, and formatting mistakes | Hosted end users | Yes |
| `/import-workflow` | Five-step import walkthrough and outcome explanations | Hosted end users | Yes |
| `/troubleshooting` | Recovery guidance for common failures | Hosted end users | Yes |
| `/faq` | Concise answers to common user questions | Hosted end users and administrators | Yes |
| `/privacy-and-security` | Graph data use, limited telemetry/logging, and delegated permissions | End users and administrators | Yes |
| `/self-hosted` | Secondary guidance for self-hosted usage | Secondary/self-hosted users | Should |

Route rules:

- Route names must remain stable, human-readable, and task-oriented.
- The self-hosted route must not be required reading for the hosted-user path.
- Internal runbooks, developer setup instructions, or operational incident notes
  must not be routed under `docs/`.

## Navigation Contract

Primary navigation MUST include:

1. Home
2. Getting started
3. CSV format
4. Import workflow
5. Troubleshooting
6. FAQ
7. Privacy and security

Secondary navigation MAY include:

1. Self-hosted

Navigation rules:

- Primary navigation order should follow a natural user journey.
- Secondary navigation must be visibly separate from the core hosted-user path.
- Every main page must include a route back to the landing page or equivalent
  top-level navigation.

## Page Content Contract

### Landing page

The landing page MUST:

- Explain in plain English what the app does.
- Identify who the app is for.
- Link to all core guide pages.
- Keep developer/internal framing out of the main copy.

### Getting started

The getting-started page MUST:

- Describe hosted-user prerequisites.
- Point users to the hosted app URL.
- Explain expected sign-in and consent behaviour.
- Keep self-hosted content to brief signposting only.

### CSV format

The CSV format page MUST:

- Name the required field `Task Name`.
- List accepted fields `Task Name`, `Description`, `Priority`, `Bucket`, and
  `Goal`.
- Explain that priority accepts 0-10 or the text values `Urgent`, `Important`,
  `Medium`, and `Low`, case-insensitively.
- Provide one minimal CSV example and one fuller example using safe illustrative
  data.
- Explain common mistakes such as missing headers, invalid priority values, and
  unexpected columns.

### Import workflow

The import workflow page MUST:

- Describe the five-step flow in order.
- Explain preview versus confirmation/execution.
- Explain `Created` and `Reused or skipped` outcomes in plain English, including that the latter is a combined bucket.
- Explain that goal-related follow-up may still be required manually.
- Not require screenshots in the initial release.

### Troubleshooting

The troubleshooting page MUST cover:

- Sign-in and permission problems.
- Administrator consent guidance.
- `No groups found` guidance.
- CSV validation failures.
- Duplicate handling expectations.
- Temporary API or throttling issues.

### FAQ

The FAQ page MUST answer the common questions listed in the feature spec,
including duplicate creation, existing-plan imports, goal handling, supported
audience, and data storage.

### Privacy and security

The privacy and security page MUST:

- Describe what the app reads from and writes to Microsoft Graph at a high level.
- State that imported Planner/task data is not persisted as application data.
- State that limited operational logs or telemetry may exist for reliability and
  support.
- State that credentials are not stored in the application or repository.
- Summarise the delegated permissions in plain English.

### Self-hosted

If included, the self-hosted page MUST:

- Be clearly labelled as secondary guidance.
- Avoid replacing the main hosted-user onboarding path.
- Link to internal or developer material only when that material is already safe
  and appropriate to reference publicly.

## README Discoverability Contract

The repository root `README.md` MUST include a `User Documentation` section that
links to `https://docs.importplanner.app`.

The README link must:

- Be easy to discover near the top-level project overview.
- Clearly distinguish public end-user docs from internal developer guidance.

## Publication Contract

- Source files MUST live under `docs/`.
- Publication MUST happen automatically after relevant changes reach `main`.
- The custom domain MUST resolve to `docs.importplanner.app`.
- The published output MUST remain safe for public access with no authentication.
- Repository-owned publication artefacts MUST document the intended Pages setup
  rather than relying entirely on undocumented manual settings.

## Quality Contract

- All public wording must use UK English.
- Public pages must avoid internal implementation jargon where simpler phrasing
  is available.
- Pages must remain readable on desktop and mobile layouts.
- Example data must be illustrative and safe for public publication.
- Broken navigation or dead internal page links are not acceptable.
