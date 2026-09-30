---
description: "Task list for in-app About page and release version alignment"
---

# Tasks: In-App Release Version and About Surface

**Input**: Design documents from `/specs/018-in-app-about-version/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included per FR-010 (release label formatter unit tests and About bUnit tests).

**Organization**: Tasks grouped by user story for independent implementation and verification.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label ([US1]–[US4])
- Include exact file paths in task descriptions

## Path Conventions

- Application: `src/ImportToPlanner.Application/`
- Web UI: `src/ImportToPlanner.Web/`
- Hugo site: `website/`
- Tests: `tests/ImportToPlanner.Tests/`, `tests/ImportToPlanner.Web.Tests/`
- Docs: `docs/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Wire MinVer (or equivalent) at repo root so assembly versions align with `vX.Y.Z` tags (FR-001).

- [ ] T001 Add MinVer package reference and `MinVerTagPrefix` (`v`) in `Directory.Build.props` per `research.md` Decision 1
- [ ] T002 [P] Document optional MSBuild properties `SourceRevisionId`, `ContinuousIntegrationBuild`, and UTC build timestamp injection in `docs/developer-quickstart.md` (build metadata section for FR-004)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Application release models, policy, formatter, and Web adapter — required before About UI and public-site work.

**⚠️ CRITICAL**: No user story implementation should merge until this phase is complete.

- [ ] T003 [P] Add `DeploymentReleaseLabel`, `BuildMetadata`, and `ReleaseInformation` types in `src/ImportToPlanner.Application/Models/` per `data-model.md` field and validation rules (official tag `DisplayValue` matches tag; non-tag `DisplayValue` MUST include pre-release identifiers; `AllowUnqualifiedShippingLabel` default `false`)
- [ ] T004 [P] Add `ReleaseLabelPolicy` options type (default `AllowUnqualifiedShippingLabel: false`) and configuration section binding in `src/ImportToPlanner.Application/` (new options class + `appsettings` schema comment in `src/ImportToPlanner.Web/appsettings.json`)
- [ ] T005 Add `IReleaseInformationQuery` in `src/ImportToPlanner.Application/Abstractions/IReleaseInformationQuery.cs` returning `ReleaseInformation` per `contracts/release-version-contract.md`
- [ ] T006 Implement `ReleaseLabelFormatter` (or equivalent normaliser) in `src/ImportToPlanner.Application/Services/ReleaseLabelFormatter.cs` enforcing FR-005: tagged builds match tag; non-tag builds full SemVer with pre-release identifiers; no unqualified shipping label unless `ReleaseLabelPolicy.AllowUnqualifiedShippingLabel` is true
- [ ] T007 [P] Add unit tests in `tests/ImportToPlanner.Tests/ReleaseLabelFormatterTests.cs` for official tag input, pre-release/non-tag input, missing-tag/shallow behaviour, and policy flag edge cases per FR-010 and `contracts/release-version-contract.md`
- [ ] T008 Implement `ReleaseInformationQuery` adapter in `src/ImportToPlanner.Web/Infrastructure/ReleaseInformationQuery.cs` reading `AssemblyInformationalVersion` / custom assembly metadata and optional build properties; map through `ReleaseLabelFormatter`
- [ ] T009 Register `IReleaseInformationQuery`, `ReleaseLabelFormatter`, and `ReleaseLabelPolicy` options in `src/ImportToPlanner.Application/DependencyInjection.cs` and Web host composition (`src/ImportToPlanner.Web/ImportToPlannerWebHost.cs` or existing DI extension)

**Checkpoint**: `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj --filter "FullyQualifiedName~Release"` passes; query resolves labels on local build.

---

## Phase 3: User Story 1 — Signed-In User Checks Their Running Release (Priority: P1) 🎯 MVP

**Goal**: Dedicated `/about` route with product name and release label; reachable from footer and Help (FR-002, FR-003).

**Independent Test**: Sign in, open About from footer or Help, read release label on a known deployment (quickstart.md §3–§4).

### Implementation for User Story 1

- [ ] T010 [US1] Create `src/ImportToPlanner.Web/Features/About/Pages/About.razor` at route `/about` with `h1` product title, release label row, and sign-in prompt pattern aligned with `Profile.razor` / `Profile.razor.cs` (unsigned users MUST NOT see release content; no modal dialog)
- [ ] T011 [US1] Add `About.razor.cs` injecting `IReleaseInformationQuery`, `IOptions<DocsExternalLinksOptions>`, and auth services; assemble UK English copy in the presenter/code-behind per `contracts/about-page-ui-contract.md`
- [ ] T012 [P] [US1] Add internal About `MudLink` with `Href="/about"` in `src/ImportToPlanner.Web/Components/Layout/MainLayout.razor` footer row alongside Documentation, Terms, Privacy, and Support
- [ ] T013 [US1] Add Help `MudMenu` (or equivalent) on `src/ImportToPlanner.Web/Features/Import/Pages/Home/Home.razor` listing Documentation (external), Support (external), and About (`/about`); unsigned About selection routes through sign-in per `contracts/app-external-links-amendment.md`
- [ ] T014 [P] [US1] Add bUnit tests in `tests/ImportToPlanner.Web.Tests/AboutPageTests.cs` for unsigned sign-in prompt, signed-in release label visibility, and external link hrefs matching `DocsExternalLinksOptions` paths (`/`, `/terms`, `/privacy-and-security`, `/support`)

**Checkpoint**: Signed-in user reaches About from footer and Help within three actions from `/` (SC-001); non-tag local build shows pre-release SemVer, not bare `v1.0.0` (SC-003).

---

## Phase 4: User Story 2 — User Confirms Documentation Matches Their Product Release (Priority: P2)

**Goal**: Above-fold public guide applicability using the same `site.Params.releaseVersion` as the footer (FR-007, SC-005).

**Independent Test**: Build Hugo with `HUGO_PARAMS_RELEASEVERSION=v9.9.9-test`; footer and hero/import-workflow callouts show the same tag (quickstart.md §5).

### Implementation for User Story 2

- [ ] T015 [P] [US2] Create `website/layouts/_partials/custom/guide-release-applicability.html` with UK English applicability copy for tagged (`ReleaseVersionLabel`) and `unreleased` builds per `data-model.md` `PublicGuideReleaseContext`
- [ ] T016 [P] [US2] Include the partial above the fold in `website/content/_index.md` (landing hero region)
- [ ] T017 [US2] Include the same partial above the fold in `website/content/import-workflow.md` intro section

**Checkpoint**: `./scripts/invoke-hugo-site.sh build` output shows consistent version on footer partial and both guide entry surfaces.

---

## Phase 5: User Story 3 — Maintainer Cuts a Coherent Public Release (Priority: P3)

**Goal**: Extended release runbook with app deploy order and three-way verification checklist (FR-009, SC-002).

**Independent Test**: Maintainer can follow runbook steps and complete Pass/Fail checklist for a test tag (quickstart.md §6).

### Implementation for User Story 3

- [ ] T018 [US3] Extend `docs/release-runbook.md` with ordered steps: changelog → annotated `vX.Y.Z` tag → Hugo deploy verification → **application** deploy from the same tag (hosted ACA + self-hosted image/tag notes) per `research.md` Decision 7
- [ ] T019 [US3] Add `ReleaseVerificationChecklist` template table in `docs/release-runbook.md` with fields `TagName`, `PublicSiteLabel`, `InAppAboutLabel`, `VerifiedAtUtc`, `Verifier`, `Result` (Pass | Fail); document normalisation of `v` prefix for comparisons per `data-model.md`
- [ ] T020 [P] [US3] Add contributor subsection in `docs/release-runbook.md` or `docs/developer-quickstart.md` explaining how local, CI, staging, and tagged production builds derive version strings (MinVer pre-release vs official tag)

**Checkpoint**: New contributor can read runbook and explain staging pre-release labels vs tagged production.

---

## Phase 6: User Story 4 — Support Gathers Diagnostic Context (Priority: P4)

**Goal**: Optional UTC build time and source revision on About when build supplies them; omit rows when unknown (FR-004).

**Independent Test**: Build with MSBuild metadata properties set; About shows rows; local build without properties omits rows without placeholders (quickstart.md §2).

### Implementation for User Story 4

- [ ] T021 [US4] Extend `ReleaseInformationQuery` in `src/ImportToPlanner.Web/Infrastructure/ReleaseInformationQuery.cs` to populate `BuildMetadata` (`BuiltAtUtc`, `SourceRevisionId`, optional `SourceRevisionUrl`) only when build/CI provides values; MUST NOT include tenant ids or secrets
- [ ] T022 [US4] Render optional build metadata rows in `src/ImportToPlanner.Web/Features/About/Pages/About.razor` (UTC labelled, short SHA; optional commit link when URL template configured); omit entire rows when fields are null per `contracts/about-page-ui-contract.md`
- [ ] T023 [P] [US4] Pass `SourceRevisionId` and CI build timestamp MSBuild properties in `.github/workflows/ci.yml` (and staging deploy workflow if applicable) per `research.md` Decision 5
- [ ] T024 [P] [US4] Extend `tests/ImportToPlanner.Web.Tests/AboutPageTests.cs` to assert metadata rows appear only when query returns populated `BuildMetadata`

**Checkpoint**: About always shows release label; metadata is additive and honest.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: FR-008 link parity, accessibility, format gate, quickstart validation.

- [ ] T025 [P] Verify About external links reuse the same URL composition as `MainLayout.razor.cs` / `DocsExternalLinksOptions` in `src/ImportToPlanner.Web/Features/About/Pages/About.razor.cs` (no `{DocsBaseUrl}/about`)
- [ ] T026 [P] Keyboard/heading accessibility pass on About page (`h1` title, MudBlazor patterns) per `contracts/about-page-ui-contract.md`
- [ ] T027 Run `specs/018-in-app-about-version/quickstart.md` manual checklist and record any gaps in PR notes
- [ ] T028 Run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal` per `AGENTS.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **blocks all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 — MVP
- **US2 (Phase 4)**: Depends on Phase 1 only for Hugo params (can parallelise with US1 after Phase 2 if staffed separately; no Blazor dependency)
- **US3 (Phase 5)**: Can start after plan/contracts; best completed after US1/US2 behaviour is known
- **US4 (Phase 6)**: Depends on Phase 2 and US1 About shell (T010–T011)
- **Polish (Phase 7)**: After desired user stories complete

### User Story Dependencies

- **US1 (P1)**: Requires Foundational — no dependency on US2–US4
- **US2 (P2)**: Independent of Blazor About; aligns at release cut via shared tag discipline
- **US3 (P3)**: Documents US1/US2 verification; no code dependency
- **US4 (P4)**: Extends US1 About UI and Foundational query

### Parallel Opportunities

- T001 and T002 (Setup)
- T003, T004, T007 after T005–T006 sequencing (T007 parallel once T006 exists)
- T012 and T014 parallel after T011
- T015, T016 parallel; T017 after partial exists
- T020 parallel with T018–T019
- T023 and T024 parallel after T021–T022
- US2 Hugo work can proceed in parallel with US1 once Phase 2 is done

---

## Parallel Example: User Story 1

```bash
# After T011 completes:
# Developer A: T012 MainLayout footer link
# Developer B: T014 AboutPageTests.cs (with test doubles for query)
# Developer C: T013 Help menu on Home.razor
```

---

## Parallel Example: User Story 2

```bash
# After T015 partial exists:
# Developer A: T016 _index.md inclusion
# Developer B: T017 import-workflow.md inclusion
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE** using quickstart.md §2–§4
5. Demo release label path before Hugo/runbook polish

### Incremental Delivery

1. Setup + Foundational → honest in-app SemVer available
2. US1 → About + navigation (MVP)
3. US2 → public guide applicability above the fold
4. US3 → maintainer runbook and checklist
5. US4 → CI metadata and About diagnostics
6. Polish → format gate and quickstart sign-off

### Suggested PR Slicing

- PR A: Phases 1–2 + US1 (core product value)
- PR B: US2 (Hugo partials)
- PR C: US3 + US4 + Polish (operations and diagnostics)

---

## Notes

- Domain layer remains unchanged (constitution Dependency Rule)
- Do not duplicate version strings in source; rely on MinVer + Hugo `HUGO_PARAMS_RELEASEVERSION`
- Staging app on `main` keeps pre-release About labels while public site shows last tag until next release cut
- `[P]` tasks touch different files — coordinate before merging conflicting layout changes
