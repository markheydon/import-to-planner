# Implementation Plan: Optional Assignees From CSV

**Branch**: `014-import-assignees` | **Date**: 2026-09-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/014-import-assignees/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Accept optional CSV heading **Assigned To**, split email/sign-in names in the existing Infrastructure parser, resolve them against destination members at preview (existing `GroupMember.Read.All`), assign on Graph create only, and emit `AssignPersonToTask` follow-up when someone cannot be assigned. Unknown people never fail the row. Lookup failure with addresses present blocks preview. Name-matched existing tasks stay skip-only. No new Graph scopes, people-picker, or heading aliases.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK from `global.json`: 10.0.300, `rollForward: latestFeature`)

**Primary Dependencies**: Existing `ICsvImportParser` / `CsvTaskRow`, `IPlannerGateway`, Microsoft Graph group members + `plannerTask.assignments` (Infrastructure only), MudBlazor preview grid, public docs under `docs/`

**Storage**: N/A — no new persistence

**Testing**: xUnit v3 in `ImportToPlanner.Tests` (`CsvImportParserTests`, `ImportPlanningUseCaseTests`, `ImportExecutionUseCaseTests`, `GraphPlannerGatewayTests`); architecture compliance unchanged; no AppHost tests; no Playwright for this increment

**Target Platform**: ASP.NET Core Blazor app (hosted and self-hosted); same parser, planning, and gateway mapping in both modes

**Project Type**: Layered Blazor web application

**Performance Goals**: One member-list fetch per preview when Assigned To values exist (paginated); create remains one POST with assignments; files already capped at 10 MB

**Constraints**: UK English messages; constitution dependency rule (Graph types stay out of Application/Domain); create-only writes; exact heading `Assigned To` (aliases #127); workbook import remains #55; no new delegated scopes

**Scale/Scope**: Parser + Application models + gateway members/create + preview column + manual-action presenter + docs; no new projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Dependency Direction**: Pass. CsvHelper and Graph members/assignments stay in `ImportToPlanner.Infrastructure.Graph`. Application owns `PlanMember` and address lists.
- **II. Core Policy Neutrality**: Pass. Match policy is mail/sign-in name on repository types, not Graph `User` or `plannerAssignments`.
- **III. Boundary Explicitness**: Pass. Parser maps CSV text → address list. Planning matches members. Execution passes ids into the gateway. Presenter owns UK follow-up copy.
- **IV. Replaceability**: Pass. A future Planner adapter can resolve members differently; Application does not depend on group APIs.
- **V. Traceability**: Pass. Behaviour maps to issue #132 and spec FR-001–FR-019.
- **VI. Testability**: Pass. Parser, planning doubles, execution doubles, and Graph request-adapter tests fail without deployment.
- **VII. Explicit errors**: Pass. Lookup failure is request-level structured validation; unknown people are follow-up, not swallowed.
- **VIII. Security**: Pass. Untrusted CSV; delegated session; no new secrets; no directory-wide user search; messages must not dump files or member lists.
- **IX. Quality evidence**: Pass. Quickstart lists parser, planning, execution, Graph, architecture, format, and docs checks.
- **X. Self-hosted viability**: Pass. Same scopes and gateway; no hosted-only directory.

## Project Structure

### Documentation (this feature)

```text
specs/014-import-assignees/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── import-assignees-contracts.md
└── tasks.md             # Phase 2 (/speckit-tasks) — not created by /speckit-plan
```

### Source Code (repository root)

```text
src/
├── ImportToPlanner.Application/
│   ├── Models/CsvTaskRow.cs, ImportTaskPlanItem.cs, ManualAction.cs
│   ├── Abstractions/IPlannerGateway.cs          ← GetPlanMembersAsync + create ids
│   └── Services/ImportExecutionUseCase.cs, ImportFingerprintBuilder.cs,
│       ImportPlanningUseCase.cs
├── ImportToPlanner.Infrastructure.Graph/
│   ├── Import/CsvImportParser.cs                ← heading, split list
│   └── Planner/GraphPlannerGateway.cs           ← members + assignments on POST
└── ImportToPlanner.Web/
    ├── Features/Import/Pages/Home/Home.razor    ← accepted fields + preview column
    └── Features/Import/Presenters/ImportPlanningPresenter.cs,
        ImportExecutionPresenter.cs

docs/
├── csv-format.md
└── troubleshooting.md

docs-internal/
├── entra-app-registration-setup.md
└── microsoft-graph-guidelines.md                ← note existing scope used for Assigned To

tests/
├── ImportToPlanner.Tests/
│   ├── CsvImportParserTests.cs
│   ├── Fixtures/with-extra-columns.csv
│   ├── ImportExecutionUseCaseTests.cs
│   ├── ImportPlanningUseCaseTests.cs
│   ├── GraphPlannerGatewayTests.cs
│   └── TestDoubles/PlannerGatewayStub.cs
└── ImportToPlanner.Web.Tests/
```

**Structure Decision**: Extend the existing CSV → plan → create pipeline. Do not add Domain Graph user entities. Matching stays in `ImportPlanningUseCase`. Preview formatting and follow-up prose stay in Web presenters.

## Complexity Tracking

No constitution violations. `CreateTaskAsync` gains a list parameter and a richer return (`CreatedPlannerTask`); a full create-request DTO remains deferred to keep this story reviewable.

---

## Phase 0: Research

Complete — see [research.md](research.md).

Resolved decisions:

1. Parser splits `Assigned To`; no row error for unknown text.
2. Match mail and sign-in name only; guests who are members assign.
3. `GetPlanMembersAsync` when addresses exist; existing Graph scopes only.
4. Lookup failure → `HasValidationErrors`; unknown people → follow-up.
5. Create POST assignments; retry create without assignments if Graph rejects them.
6. `AssignPersonToTask` + `PersonIdentifier` + reason codes; skip is skip-only.
7. Docs and Home copy; tests across parser, planning, execution, Graph.

No `NEEDS CLARIFICATION` items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md), [quickstart.md](quickstart.md), and [contracts/import-assignees-contracts.md](contracts/import-assignees-contracts.md).

Key design outcomes:

- `CsvTaskRow` gains `AssigneeAddresses`; plan items gain resolved/unresolved lists.
- `IPlannerGateway` gains `GetPlanMembersAsync`; `CreateTaskAsync` gains assignee ids and `CreatedPlannerTask`.
- Parser heading set includes `Assigned To`.
- Graph adapter: group/`me` members; assignments on create POST only.
- UI: Assigned to column; `AssignPersonToTask` presenter mapping; accepted-fields copy.

### Architecture impact statement

- **Dependency direction**: Unchanged. Infrastructure → Application models; Application does not reference Graph.
- **Boundary changes**: Gateway and `CsvTaskRow` are additive. `ManualAction` gains `PersonIdentifier`. No Domain Graph fields.
- **Adapter responsibilities**: Parser owns list splitting. Planning owns match and lookup gate. Graph gateway owns members and `assignments`. Web owns UK display strings.
- **Traceability**: #132 / FR-001–FR-019.
- **Testability**: Parser and stubbed members without live Planner; Graph tests use existing request adapter.
- **Error handling**: Request-level Assigned To lookup errors; per-person follow-up; coordinator already blocks on `HasValidationErrors`.
- **Security trust boundary**: Untrusted CSV; delegated session; no new scopes; no secret or member-list logging.
- **Self-hosted**: Same code path and consent set as hosted.

### Post-design Constitution Check

All gates remain Pass. Complexity Tracking notes gateway arity and create result wrapper only.

## Implementation notes (for `/speckit-tasks`)

- Reuse Due Date’s heading + extra-column fixture pattern; keep `Owner` as a true extra.
- XML-document new `CsvTaskRow`, `PlanMember`, `CreatedPlannerTask`, and gateway members per csharp-docs skill.
- Update every `CreateTaskAsync` implementer and call site (credits tests, web stubs) in one change.
- Assert Graph POST JSON contains assignment keys for user ids, omitted when empty.
- In-memory / stub gateways must seed at least one member (mail + UPN) and one guest member for tests.
- Docs: [end-user-docs](../../.github/skills/end-user-docs/SKILL.md) voice.
- After code changes, run `dotnet format ImportToPlanner.slnx --no-restore --verify-no-changes --verbosity minimal`.
