# Research: End-User Documentation Site

## Decision 1: Use GitHub Pages with minimal repository-owned configuration

- Decision: Publish the public docs from `docs/` using GitHub Pages with
  repository-owned deployment artefacts such as `_config.yml`, `CNAME`, and a
  Pages workflow or equivalent automated deployment path.
- Rationale: The feature needs a publicly reachable docs site with automatic
  publication, but it does not justify introducing a separate docs framework or
  client-side app. GitHub Pages matches the static-content needs and keeps the
  delivery mechanism close to the repository.
- Alternatives considered: Keep deployment as a manual repository setting only,
  or adopt a larger static-site generator stack. Manual-only configuration was
  rejected because too much of the behaviour would live outside the repo;
  a larger generator was rejected because it adds tooling and maintenance that
  the spec does not require.

## Decision 2: Keep `docs/README.md` as a public-folder policy note and create `docs/index.md` for users

- Decision: Leave `docs/README.md` as the repository-facing explanation that
  `docs/` is public, and create a separate `docs/index.md` as the actual user
  landing page.
- Rationale: The existing README in `docs/` already captures an important
  repository constraint about public versus internal content. Reusing it as the
  user landing page would either dilute that policy note or expose contributor
  framing to end users.
- Alternatives considered: Replace `docs/README.md` with the landing page, or
  duplicate the policy warning elsewhere. Rejected because replacing it loses a
  useful guardrail and duplication creates maintenance drift.

## Decision 3: Source end-user wording from verified product behaviour, not issue prose alone

- Decision: Write the public pages from verified app and test behaviour for CSV
  headers, priority values, duplicate handling, manual actions, consent states,
  and "No groups found" support guidance.
- Rationale: The issue gives the page inventory, but the repo already contains
  more precise behaviour contracts. Using the verified wording reduces the risk
  that docs and product diverge.
- Alternatives considered: Write pages from the issue requirements only, or use
  broader implementation details from internal docs. Rejected because the first
  risks behavioural mismatch and the second leaks internal framing into public
  docs.

## Decision 4: Keep the hosted-user path primary and put self-hosted guidance on a secondary page

- Decision: Put self-hosted guidance on a separate secondary page linked from
  the main docs rather than embedding setup instructions in the main
  getting-started journey.
- Rationale: The spec and clarification explicitly prioritise hosted end users.
  A separate page keeps self-hosted information discoverable without crowding
  the primary onboarding path or shifting tone towards developer setup.
- Alternatives considered: Inline self-hosted notes throughout the main guides,
  or exclude self-hosted guidance entirely. Rejected because inline notes would
  confuse the primary audience, while omission would ignore a documented
  secondary scenario.

## Decision 5: Omit screenshots in the initial release and use explicit text-first guidance

- Decision: Do not require screenshots in the initial release. Use explicit step
  descriptions, examples, and outcome explanations instead.
- Rationale: Safe demo data for screenshots is not yet available, and the user
  has ruled out using live data. Text-first guidance keeps the feature moving
  while preserving privacy and operational safety.
- Alternatives considered: Use clearly labelled placeholders or capture live
  screenshots. Rejected because placeholders add visual noise without adding real
  instructional value, and live screenshots would create avoidable data-handling
  risk.

## Decision 6: Use a shallow, task-oriented information architecture

- Decision: Organise the site around one landing page and the core user tasks:
  getting started, CSV format, import workflow, troubleshooting, FAQ, privacy
  and security, plus a secondary self-hosted page.
- Rationale: The feature is a help surface, not a full product manual. Shallow
  navigation minimises cognitive load and aligns with the issue's requested page
  set.
- Alternatives considered: A single long guide or a much deeper knowledge base.
  Rejected because one long guide is harder to scan and a deep hierarchy is not
  warranted by the feature scope.

## Decision 7: Verify through public-docs checks and deployment evidence rather than a new docs test harness

- Decision: Validation should focus on page presence, route coverage, README
  discoverability, render quality on mobile and desktop, and successful Pages
  publication.
- Rationale: The repository has no existing docs-specific automated test stack,
  and this feature does not change application behaviour. Manual and deployment
  verification are the smallest practical evidence set.
- Alternatives considered: Add a dedicated docs test framework or skip explicit
  verification beyond a content review. Rejected because a new framework is too
  much tooling for this slice, while content review alone is too weak for a
  public site deployment.

## Repository Facts That Shaped the Decisions

- `docs/README.md` already establishes that `docs/` is public-only and that
  internal material belongs in `docs-internal/`.
- The current repo has CI and staging deployment workflows, but no GitHub Pages
  workflow yet.
- `README.md` currently lacks a public "User Documentation" section.
- App configuration and tests verify the public-facing Graph scopes, consent
  wording, CSV headers, priority mappings, duplicate handling, and manual action
  behaviour that the public docs must describe.

## Policy Checkpoints

- All public wording must use UK English.
- No internal runbooks, implementation notes, secrets, or tenant-sensitive
  values may move into `docs/`.
- Hosted-user guidance is primary; self-hosted guidance remains secondary.
- Privacy wording must distinguish between non-persisted imported Planner/task
  data and limited operational logs or telemetry.
