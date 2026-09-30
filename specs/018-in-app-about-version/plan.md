# Implementation Plan: In-App Release Version and About Surface

**Branch**: `018-in-app-about-version` | **Date**: 2026-09-30 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/018-in-app-about-version/spec.md`

## Summary

Add build-time SemVer labelling aligned with `vX.Y.Z` GitHub Release tags, a signed-in **About**
page at a dedicated route, footer and **Help** entry points, optional build metadata for support,
automated tests for version formatting policy, public-site above-fold “guide applies to release”
copy where spec 017 footer alone is insufficient, and an extended `docs/release-runbook.md`
three-way verification checklist pairing app deploys with Hugo tag deploys.

## Technical Context

**Language/Version**: .NET 10 / C# 14 (existing solution); Hugo extended for FR-007 gap-fill  
**Primary Dependencies**: MinVer (or equivalent MSBuild git versioning) at repo root; existing
MudBlazor shell; `DocsExternalLinksOptions`; Hugo `params.releaseVersion` / CI env
`HUGO_PARAMS_RELEASEVERSION` from spec 017  
**Storage**: N/A — release data from assembly attributes and build-time env; no new database  
**Testing**: xUnit v3 + NSubstitute in `ImportToPlanner.Tests`; bUnit in `ImportToPlanner.Web.Tests`
per `docs/engineering-policies.md`  
**Target Platform**: Blazor web host (hosted ACA staging/production + self-hosted); GitHub Pages
public docs  
**Project Type**: Bounded feature in existing Clean Architecture Blazor app + small Hugo partial/content
updates  
**Performance Goals**: About page load adds negligible overhead (read cached release info once per
request/session)  
**Constraints**: UK English; sign-in required for About; no modal About; honest pre-release labels
on non-tag builds; extend—not replace—017 external links contract  
**Scale/Scope**: One new route, shell link/menu updates, MSBuild versioning wiring, runbook/docs
updates, 2–3 Hugo layout touches, unit/component tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Gate | Assessment |
| --- | --- |
| I. Dependency Rule | Pass. Release query port in Application; assembly/git reads in Web/Infrastructure adapter; Domain unchanged. |
| II. Technology-Neutral Core | Pass. No SemVer or Mud types in Domain; release policy expressed with repository-owned types. |
| III. Explicit Boundaries | Pass. Interactor/query returns `ReleaseInformation` structures; Razor/presenter assembles UK English labels and link text. |
| IV. Replaceability | Pass. MinVer/Hugo params are delivery choices; About remains a standard routable page. |
| V. Traceability | Pass. Work maps to FR-001–FR-010 and user stories 1–4. |
| VI. Testable Behaviour | Pass. Unit tests for formatter; bUnit for About auth/links; quickstart scenarios. |
| VII. Explicit Errors | Pass. Sign-in gate fails closed; missing build metadata omits UI rather than fake values. |
| VIII. Security by Design | Pass. About requires auth; no secrets in labels; external links unchanged https policy. |
| IX. Quality Evidence | Pass when PR includes tests + `dotnet format` verification. |
| X. Self-Hosted Viability | Pass. Same MinVer/build args for self-hosted images; `DocsBaseUrl` remains configurable. |

**Post-design re-check**: No unjustified gate failures. Help menu addition is presentation chrome
only.

## Project Structure

### Documentation (this feature)

```text
specs/018-in-app-about-version/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── about-page-ui-contract.md
│   ├── release-version-contract.md
│   └── app-external-links-amendment.md
└── tasks.md                    # Phase 2 — /speckit-tasks (not created here)
```

### Source Code (repository root)

```text
Directory.Build.props             # ADD MinVer / versioning properties
src/ImportToPlanner.Application/  # ReleaseInformation types + query port + formatter
src/ImportToPlanner.Web/
  Features/About/Pages/           # About.razor (+ code-behind/presenter)
  Components/Layout/              # Footer About link, Help menu
  Infrastructure/                 # ReleaseInformation adapter (assembly attributes)
website/
  layouts/_partials/custom/       # Hero/guide applicability partial (FR-007)
  content/_index.md               # Include partial or shortcode
  content/import-workflow.md      # Include partial above fold
docs/release-runbook.md           # EXTEND — app deploy order + 3-way checklist
docs/                             # Optional: build versioning contributor note
tests/ImportToPlanner.Tests/      # Release label formatter tests
tests/ImportToPlanner.Web.Tests/  # About page bUnit tests
.github/workflows/                # OPTIONAL: pass SourceRevisionId/build timestamp on app CI
```

**Structure Decision**: Implement inside the existing solution layers; no new projects unless
formatter size warrants a tiny shared helper already covered by Application tests.

## Complexity Tracking

No constitution violations requiring justification table entries.

---

## Phase 0: Outline & Research

Complete — see [research.md](research.md).

Resolved: MinVer build-time versioning; Application query + formatter; `/about` auth pattern;
footer + new Help menu; CI build metadata; Hugo hero applicability partial; extended runbook;
test strategy.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design & Contracts

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and
[contracts/](contracts/).

### Design highlights

**Release versioning**

- Root MSBuild MinVer (or equivalent) aligned with `v*` tags used by Hugo deploy.
- Application-owned normalisation enforcing FR-005; Web adapter reads assembly/custom attributes.
- CI documents optional `/p:SourceRevisionId` and build timestamp properties.

**About page**

- Route `/about`, sign-in gate like Profile pattern.
- Present product name, release label, optional metadata, external links per
  `about-page-ui-contract.md`.

**Shell navigation**

- Footer About link in `MainLayout.razor`.
- New Help menu (MudMenu) meeting `app-external-links-amendment.md`.

**Public site (FR-007 gap-fill)**

- Reuse `site.Params.releaseVersion`; add above-fold partial on landing + import workflow.
- Do not change spec 017 demo/terms/Hugo scope beyond this presentation.

**Runbook (FR-009)**

- Extend `docs/release-runbook.md` with app deploy from tag and three-way checklist template.

### Architecture impact statement

- **Dependency direction**: Application adds release query + formatter; Web adds About feature and
  adapter registration; Domain untouched.
- **Boundary changes**: New read-only query (`ReleaseInformation`) with no Graph or commercial
  coupling.
- **Adapter responsibilities**: Web maps query results to MudBlazor About UI; Hugo partial maps
  `releaseVersion` param only.
- **Traceability**: Tasks should cite FR ids and contract sections.
- **Testability**: Formatter unit tests; About bUnit auth/links; quickstart manual checklist.
- **Error handling**: Unsigned About → sign-in prompt; missing metadata → omitted rows.
- **Security trust boundaries**: About authenticated; build metadata excludes tenant secrets.
- **Self-hosted**: Document image build args mirroring CI; same About behaviour.

---

## Phase 2: Implementation Tasks

Not created by `/speckit-plan`. Run `/speckit-tasks` to generate `tasks.md`.
