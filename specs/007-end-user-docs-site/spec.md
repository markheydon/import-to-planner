# Feature Specification: End-User Documentation Site

**Feature Branch**: `007-end-user-docs-site`

**Created**: 2026-05-27

**Status**: Draft

**Input**: User description: "Create end-user documentation site published at docs.importplanner.app covering onboarding, CSV format, import workflow, troubleshooting, FAQ, and privacy/security. Self-hosted deployments are a secondary concern. Developer/internal docs remain in docs-internal/."

## Clarifications

### Session 2026-05-27

- Q: How should the privacy page describe data storage for the hosted service? -> A: The app does not persist imported Planner/task data as application data, but limited operational logs/telemetry may exist for reliability and support.
- Q: How should self-hosted guidance be presented within the docs site? -> A: Put self-hosted guidance on a separate secondary page linked from the main docs.
- Q: What publication timing target should the spec require after docs changes are pushed to main? -> A: No fixed timing target; publication is best effort, but it must happen automatically.
- Q: What should the initial release require for workflow screenshots? -> A: Omit screenshots from the initial release and revisit them later once suitable demo data is available.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - New User Gets Started (Priority: P1)

A first-time user has been given the link to the app by a colleague or finds it online. They need to quickly understand what the app does, whether it applies to them, and how to sign in and reach a point where they are ready to import tasks.

**Why this priority**: Without onboarding guidance, new users cannot proceed at all. A clear landing page and getting-started guide are the minimum viable documentation.

**Independent Test**: Can be fully tested by visiting the docs site home page, reading the landing page and the getting-started guide, and confirming a user with no prior knowledge can describe what the app does and successfully sign in to the app.

**Acceptance Scenarios**:

1. **Given** a user arrives at docs.importplanner.app with no prior knowledge of the app, **When** they read the landing page, **Then** they can explain in plain English what the app does, who it is for, and how to navigate to the relevant guide.
2. **Given** a user wants to know what they need before using the app, **When** they read the getting-started guide, **Then** they find a clear list of prerequisites (Microsoft 365 account, appropriate plan membership or Planner access) and know how to access the hosted app URL.
3. **Given** a user follows the getting-started guide, **When** they reach the sign-in step, **Then** the guide sets accurate expectations about the consent prompt and permissions requested.

---

### User Story 2 - User Prepares a CSV File (Priority: P2)

A user already familiar with the app wants to prepare a CSV file for import. They need to know exactly which columns are required, which are optional, what values are accepted, and how to avoid common mistakes.

**Why this priority**: An invalid CSV is the most common reason an import fails. Clear format documentation with examples prevents errors before they occur and reduces frustration.

**Independent Test**: Can be fully tested by reading only the CSV format page and confirming a user can produce a valid minimal CSV and a valid full-featured CSV from scratch.

**Acceptance Scenarios**:

1. **Given** a user opens the CSV format guide, **When** they read the required-columns section, **Then** they can identify which column headings are mandatory and what values each accepts.
2. **Given** a user opens the CSV format guide, **When** they look for examples, **Then** they find at least one minimal example and one full-featured example that they can copy and adapt.
3. **Given** a user is unsure about priority values, **When** they consult the CSV format guide, **Then** they find a complete list of accepted priority values (e.g., "Urgent", "Important", "Medium", "Low") and what happens if an unrecognised value is supplied.
4. **Given** a user makes a common formatting error (wrong delimiter, missing header row, extra whitespace), **When** they consult the guide, **Then** the "common mistakes" section explicitly covers that scenario.

---

### User Story 3 - User Completes an Import (Priority: P3)

A user has prepared their CSV and wants to perform an import. They need a clear walkthrough of every step in the import workflow so they understand what each screen does and what to expect at the end.

**Why this priority**: The import workflow has multiple distinct steps; users who skip reading guidance may take the wrong action (e.g., creating a duplicate plan) or not understand the outcome summary.

**Independent Test**: Can be tested by reading the import workflow guide and confirming a user can describe the end-to-end process, including what "create vs reuse vs skipped" means for tasks.

**Acceptance Scenarios**:

1. **Given** a user opens the import workflow guide, **When** they read through it in order, **Then** they find a step-by-step walkthrough that matches the actual app screens: select group → enter plan name → upload CSV → validate & preview → confirm & execute → review report, without requiring screenshots in the initial release.
2. **Given** a user is unsure what "reuse" means in the execution report, **When** they consult the import workflow guide, **Then** they find plain-English explanations for each outcome state: created, reused, and skipped.
3. **Given** a user wants to import to an existing plan, **When** they read the guide, **Then** it is clear that the app can match and reuse an existing plan by name.

---

### User Story 4 - User Resolves a Problem (Priority: P4)

A user encounters an error or unexpected behaviour during sign-in, validation, or import. They need a troubleshooting page and an FAQ that helps them identify the cause and take corrective action without requiring support.

**Why this priority**: Self-service troubleshooting reduces support burden and builds user confidence. It also documents known edge cases such as throttling and permission errors.

**Independent Test**: Can be tested independently by simulating common failure scenarios and confirming the troubleshooting page addresses each one with a clear cause and resolution.

**Acceptance Scenarios**:

1. **Given** a user cannot sign in or receives a permissions error, **When** they consult the troubleshooting page, **Then** they find guidance covering the most common causes: missing Microsoft 365 licence, not being a member of any group, or admin consent not yet granted.
2. **Given** a user sees "No groups found" in the app, **When** they consult the troubleshooting page, **Then** they find a clear explanation and the steps required to resolve it.
3. **Given** a user's CSV fails validation, **When** they consult the troubleshooting page, **Then** they find common validation failure causes and how to fix them.
4. **Given** a user has questions such as "will this create duplicates?" or "is my data stored?", **When** they visit the FAQ page, **Then** they find concise, honest answers.

---

### User Story 5 - User Understands Privacy and Security (Priority: P5)

A user or their IT administrator wants to understand what data the app reads, what is sent to external services, and what permissions are required, before approving use or granting consent.

**Why this priority**: Privacy and security transparency builds trust and enables IT governance decisions. It is especially important for organisations evaluating whether to permit use.

**Independent Test**: Can be tested independently by reading the privacy and security page and confirming an IT administrator can make an informed consent decision without reading source code.

**Acceptance Scenarios**:

1. **Given** an IT administrator reads the privacy and security page, **When** they look for data handling information, **Then** they find a clear statement of what data is read from and written to Microsoft Graph, confirmation that imported Planner/task data is not persisted as application data, and an explanation that limited operational logs or telemetry may exist for reliability and support.
2. **Given** a user is concerned about credentials, **When** they read the privacy and security page, **Then** they find a clear statement that no credentials or secrets are stored in the application or repository.
3. **Given** an administrator needs to understand what permissions to consent to, **When** they read the page, **Then** they find a plain-English summary of the delegated permissions used and why each is required.

---

### User Story 6 - Self-Hosted User Accesses Relevant Guidance (Priority: P6)

A technically capable user wants to run their own instance of the app rather than using the hosted version. They need to know where to find the relevant information without the self-hosted path dominating the primary user experience.

**Why this priority**: Self-hosted deployments are a valid secondary scenario, but the primary audience is users of the hosted app. Self-hosted guidance must be accessible without obscuring the main content.

**Independent Test**: Can be tested by confirming that a primary user journey (P1–P3) can be completed without encountering confusing self-hosted instructions, while a self-hosted user can still locate relevant guidance.

**Acceptance Scenarios**:

1. **Given** a user of the hosted app is reading the getting-started guide, **When** they follow the guide, **Then** self-hosted content is limited to brief signposting so it does not confuse the primary flow.
2. **Given** a self-hosted user wants to know how to configure and run the app, **When** they visit the docs site, **Then** they can locate a separate secondary page covering self-hosted deployment.

---

### Edge Cases

- What happens when a user visits a page that does not yet have full content (stub page)?
- How does the docs site render on a narrow mobile screen?
- What if a user's Microsoft 365 tenant uses custom domain names — does the getting-started guide still apply?
- How should the site handle users accessing it from a country where GitHub Pages may be restricted?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The documentation site MUST be publicly accessible at docs.importplanner.app without requiring authentication.
- **FR-002**: The site MUST include a landing page that clearly states the app's purpose, its intended audience, and provides navigation links to all main sections.
- **FR-003**: The site MUST include a getting-started guide covering: prerequisites (Microsoft 365 account, appropriate Planner access), how to reach the hosted app URL, and what to expect during sign-in.
- **FR-004**: The site MUST include a CSV format guide covering: required columns, optional columns, accepted values for each column, at least one minimal example, at least one full-featured example, and a list of common formatting mistakes.
- **FR-005**: The site MUST include an import workflow guide with step-by-step instructions matching the actual app screens, and plain-English explanations of the "created", "reused", and "skipped" task outcomes. Screenshots are out of scope for the initial release.
- **FR-006**: The site MUST include a troubleshooting page addressing: sign-in and permission failures, the "No groups found" scenario, CSV validation failures, duplicate task handling, and temporary API or throttling issues.
- **FR-007**: The site MUST include an FAQ page answering the most common user questions, including questions about duplicates, importing to existing plans, the "Goal" field, who can use the app, and data storage.
- **FR-008**: The site MUST include a privacy and security page covering: data read from and written to Microsoft Graph, confirmation that imported Planner/task data is not persisted as application data, an explanation that limited operational logs or telemetry may exist for reliability and support, a statement that no credentials are stored in the app or repository, and a plain-English summary of delegated permissions.
- **FR-009**: All content MUST be written in plain English, without internal implementation jargon, and suitable for non-technical end users as the primary audience.
- **FR-010**: The site MUST render correctly on both mobile and desktop screen sizes.
- **FR-011**: The documentation source files MUST live under the `/docs` folder in the repository.
- **FR-012**: The site MUST be published automatically whenever the documentation source is updated in the main branch.
- **FR-013**: The README MUST include a "User Documentation" section linking to the published docs site at docs.importplanner.app.
- **FR-014**: The site SHOULD include a separate secondary page covering self-hosted deployment, linked from the main docs and positioned so it does not interrupt the primary end-user journey.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time user with no prior knowledge of the app can locate, read the landing page, and complete the getting-started steps without consulting any other resource.
- **SC-002**: The documentation site is publicly reachable at docs.importplanner.app with no authentication required.
- **SC-003**: A user can produce a valid CSV file ready for import using only the CSV format guide, without trial and error.
- **SC-004**: The troubleshooting page addresses 100% of the known error scenarios documented in issue #11 (sign-in/permission issues, no groups found, validation failures, duplicate handling, throttling).
- **SC-005**: The site content renders without layout defects on a 375 px wide mobile viewport and on a 1280 px wide desktop viewport.
- **SC-006**: Each page loads and is readable within a standard web browser without any additional plugins or sign-in.
- **SC-007**: A change merged to the main branch triggers an automatic documentation deployment without requiring a manual publish step, and the published site eventually reflects the latest committed source.
- **SC-008**: An IT administrator can read the privacy and security page and make an informed consent decision without needing to inspect the source code.

## Assumptions

- The hosted app is published and accessible at a stable URL that can be referenced in the getting-started guide.
- The custom domain docs.importplanner.app will be configured to point to the GitHub Pages deployment.
- The primary audience is users of the hosted version of the app; self-hosted users are a secondary audience.
- Self-hosted guidance, if included in this feature, will live on a separate secondary page rather than inside the main getting-started flow.
- Developer setup and internal engineering notes remain in `docs-internal/` and are out of scope for this feature.
- The documentation will be authored in Markdown.
- Screenshots are out of scope for the initial release because suitable demo data is not yet available; they may be added in a later update.
- The existing `/docs/README.md` file may need to be reviewed and updated to serve as the new landing page or redirected appropriately.
- Consent and permission requirements are governed by the Microsoft 365 tenant administrator, not the app itself.
- The hosted service may retain limited operational logs or telemetry for reliability and support, but not imported Planner/task data as application data.
- GitHub Pages and DNS propagation timing may vary, so the feature requires automatic publication rather than a fixed post-commit timing SLA.
