# Tasks: End-User Documentation Site

**Input**: Design documents from `/specs/007-end-user-docs-site/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, `contracts/docs-site-contract.md`  
**Tests**: No automated test tasks are included because the feature specification does not request them; verification is handled through manual content, deployment, and responsive-review tasks in `specs/007-end-user-docs-site/quickstart.md`.  
**Agent delegation**: Coding, workflow, and repository-change tasks should follow the repository delegation rules in `AGENTS.md`; this feature is primarily documentation and GitHub workflow work.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[US1-US6]**: User story label from `spec.md`; setup and foundational tasks have no story label

---

## Phase 1: Setup

**Purpose**: Create the shared publication artefacts the public docs site needs before content work begins.

- [X] T001 [P] Create the GitHub Pages site configuration and navigation defaults in `docs/_config.yml`
- [X] T002 [P] Add the custom domain declaration for `docs.importplanner.app` in `docs/CNAME`
- [X] T003 [P] Create the GitHub Pages deployment workflow for docs changes in `.github/workflows/docs-pages.yml`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the public/private boundary guidance and shared page shells that every user story depends on.

**⚠️ CRITICAL**: Complete T004-T005 before any user story work begins.

- [X] T004 [P] Update the public-folder boundary note and landing-page handoff in `docs/README.md`
- [X] T005 Create the shared public page shells and navigation/front matter in `docs/index.md`, `docs/getting-started.md`, `docs/csv-format.md`, `docs/import-workflow.md`, `docs/troubleshooting.md`, `docs/faq.md`, `docs/privacy-and-security.md`, and `docs/self-hosted.md`

**Checkpoint**: GitHub Pages configuration exists, the custom domain is declared, and every public route has a repository-owned page shell ready for story-specific content.

---

## Phase 3: User Story 1 - New User Gets Started (Priority: P1) 🎯 MVP

**Goal**: Give first-time hosted users a clear landing page and getting-started guide so they can understand the app and reach the sign-in point confidently.

**Independent Test**: Open the landing page and getting-started guide only, then confirm a new user can explain what the app does, who it is for, what prerequisites they need, and what to expect when signing in.

### Implementation for User Story 1

- [X] T006 [P] [US1] Write the landing page overview, audience explanation, and guide links in `docs/index.md`
- [X] T007 [P] [US1] Write the hosted-user prerequisites, app access steps, and sign-in expectations in `docs/getting-started.md`
- [X] T008 [US1] Add the final hosted-user cross-links and onboarding flow between `docs/index.md` and `docs/getting-started.md`

**Checkpoint**: User Story 1 is complete when a hosted user can start from the docs home page and reach the app sign-in step without reading any other page.

---

## Phase 4: User Story 2 - User Prepares a CSV File (Priority: P2)

**Goal**: Explain the CSV schema well enough that users can prepare a valid import file without trial and error.

**Independent Test**: Read only the CSV format page and confirm a user can produce both a minimal valid CSV and a fuller example file using the documented headings and values.

### Implementation for User Story 2

- [X] T009 [US2] Write the required and accepted columns, safe CSV examples, and priority guidance in `docs/csv-format.md`
- [X] T010 [US2] Add common formatting mistakes, extra-column behaviour, and invalid-priority guidance in `docs/csv-format.md`

**Checkpoint**: User Story 2 is complete when the CSV page alone is enough for a user to prepare a valid file with safe example data.

---

## Phase 5: User Story 3 - User Completes an Import (Priority: P3)

**Goal**: Describe the import workflow clearly enough that users understand the five steps, preview behaviour, execution outcomes, and manual follow-up expectations.

**Independent Test**: Read only the import workflow page and confirm a user can describe the full process, including plan reuse and the meaning of created, reused, and skipped outcomes.

### Implementation for User Story 3

- [X] T011 [US3] Write the five-step import walkthrough aligned to the current UI in `docs/import-workflow.md`
- [X] T012 [US3] Add plan reuse, created/reused/skipped outcome explanations, and manual follow-up guidance in `docs/import-workflow.md`

**Checkpoint**: User Story 3 is complete when the workflow guide stands on its own as an accurate text-first walkthrough without screenshots.

---

## Phase 6: User Story 4 - User Resolves a Problem (Priority: P4)

**Goal**: Provide troubleshooting and FAQ content that helps users recover from common problems without needing support.

**Independent Test**: Read the troubleshooting and FAQ pages only, then confirm the documented guidance covers sign-in, permission, `No groups found`, validation, duplicate, throttling, and common-question scenarios.

### Implementation for User Story 4

- [X] T013 [P] [US4] Write troubleshooting guidance for sign-in, permissions, `No groups found`, validation, duplicate handling, and throttling in `docs/troubleshooting.md`
- [X] T014 [P] [US4] Write concise end-user FAQ answers in `docs/faq.md`
- [X] T015 [US4] Cross-link the support journeys between `docs/troubleshooting.md` and `docs/faq.md`

**Checkpoint**: User Story 4 is complete when the support pages together cover all known issue #11 failure and question scenarios in plain language.

---

## Phase 7: User Story 5 - User Understands Privacy and Security (Priority: P5)

**Goal**: Give end users and administrators a trustworthy summary of Graph data handling, limited telemetry, credentials, and delegated permissions.

**Independent Test**: Read only the privacy and security page and confirm an IT administrator can understand what the app reads and writes, what is not retained as application data, and what permissions are required.

### Implementation for User Story 5

- [X] T016 [US5] Write the privacy and security guidance for Graph data handling, limited telemetry, credentials, and retention boundaries in `docs/privacy-and-security.md`
- [X] T017 [US5] Add the delegated-permission and administrator-consent explanation in `docs/privacy-and-security.md`

**Checkpoint**: User Story 5 is complete when the privacy page can stand alone as an accurate approval-readiness summary for end users and tenant administrators.

---

## Phase 8: User Story 6 - Self-Hosted User Accesses Relevant Guidance (Priority: P6)

**Goal**: Make self-hosted guidance discoverable without interrupting the primary hosted-user journey.

**Independent Test**: Complete the hosted-user reading path from the landing page through getting started without being forced into self-hosted detail, then confirm a self-hosted user can still locate the separate guidance page.

### Implementation for User Story 6

- [X] T018 [US6] Write the clearly secondary self-hosted guidance page in `docs/self-hosted.md`
- [X] T019 [US6] Add self-hosted signposting from `docs/index.md` and `docs/getting-started.md`

**Checkpoint**: User Story 6 is complete when self-hosted information is available but clearly secondary to the hosted-user journey.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Finalise discoverability, consistency, and verification across the whole docs site.

- [X] T020 [P] Add the root `User Documentation` section linking to `docs.importplanner.app` in `README.md`
- [X] T021 [P] Review UK English, navigation consistency, and internal-link coverage across `docs/index.md`, `docs/getting-started.md`, `docs/csv-format.md`, `docs/import-workflow.md`, `docs/troubleshooting.md`, `docs/faq.md`, `docs/privacy-and-security.md`, and `docs/self-hosted.md`
- [X] T022 [P] Re-check the public/private boundary wording against the completed public pages in `docs/README.md`
- [X] T023 Run the manual publication and responsive verification checklist in `specs/007-end-user-docs-site/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies; can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 and delivers the MVP hosted-user entry journey
- **Phase 4 (US2)**: Depends on Phase 2; can start after the shared page shells exist
- **Phase 5 (US3)**: Depends on Phase 2; can start after the shared page shells exist
- **Phase 6 (US4)**: Depends on Phase 2; can start after the shared page shells exist
- **Phase 7 (US5)**: Depends on Phase 2; can start after the shared page shells exist
- **Phase 8 (US6)**: Depends on Phase 3 because self-hosted signposting should hang from the final hosted-user pages
- **Phase 9 (Polish)**: Depends on all implemented user stories

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Foundational and is the MVP
- **US2 (P2)**: Starts after Foundational with no dependency on other stories
- **US3 (P3)**: Starts after Foundational with no dependency on other stories
- **US4 (P4)**: Starts after Foundational with no dependency on other stories
- **US5 (P5)**: Starts after Foundational with no dependency on other stories
- **US6 (P6)**: Depends on US1 because the self-hosted page is linked from the hosted-user landing and getting-started pages

### Within Each User Story

- Create the relevant page shell before filling story-specific content
- Complete the main page content before adding cross-links or secondary signposting
- Finish each story's independent reading path before moving to the next priority when working sequentially

### Parallel Opportunities

- T001-T003 can run in parallel during Setup
- T004 can run in parallel with T005 during Foundational because they touch different files
- After Foundational, US1-US5 can proceed in parallel if team capacity allows
- Within US1, T006 and T007 can run in parallel before T008
- Within US4, T013 and T014 can run in parallel before T015
- During Polish, T020-T022 can run in parallel before T023

---

## Parallel Example: User Story 1

```text
T006 (landing page content in docs/index.md)
T007 (getting-started guide in docs/getting-started.md)
then:
T008 (hosted-user cross-links between docs/index.md and docs/getting-started.md)
```

---

## Parallel Example: User Story 4

```text
T013 (troubleshooting guidance in docs/troubleshooting.md)
T014 (FAQ guidance in docs/faq.md)
then:
T015 (support cross-links between docs/troubleshooting.md and docs/faq.md)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Review the landing page and getting-started guide independently before expanding scope

### Incremental Delivery

1. Finish Setup + Foundational to make the site publishable and route-ready
2. Deliver US1 as the hosted-user MVP
3. Add US2, US3, US4, and US5 as independent guide slices in priority order
4. Add US6 last so self-hosted guidance stays secondary
5. Finish with README discoverability, link review, and publication verification

### Parallel Team Strategy

1. One contributor handles Setup/Foundational publication scaffolding
2. After Foundational, separate contributors can draft US1-US5 pages in parallel
3. A final contributor can add self-hosted signposting and run the cross-site verification pass

---

## Summary

- **Total tasks**: 23
- **Setup + Foundational**: 5 (T001-T005)
- **US1**: 3 (T006-T008)
- **US2**: 2 (T009-T010)
- **US3**: 2 (T011-T012)
- **US4**: 3 (T013-T015)
- **US5**: 2 (T016-T017)
- **US6**: 2 (T018-T019)
- **Polish**: 4 (T020-T023)
- **Parallel [P] tasks**: 10 of 23

Independent test criteria by story:

- **US1**: A new hosted user can understand the app and reach sign-in using only the landing page and getting-started guide
- **US2**: A user can prepare a valid CSV using only the CSV format page
- **US3**: A user can explain the five-step import flow and outcome states using only the import workflow page
- **US4**: A user can recover from the documented common problems using only the troubleshooting and FAQ pages
- **US5**: An administrator can understand data handling and delegated permissions using only the privacy and security page
- **US6**: A self-hosted user can find the separate guidance without disrupting the primary hosted-user reading path

Suggested MVP scope: Complete Phase 1, Phase 2, and Phase 3 (US1) before adding the remaining guide pages and cross-cutting polish.

Format validation: Every task uses the required checklist form with checkbox, task ID, optional `[P]`, required story label in user-story phases, and an exact file path.
