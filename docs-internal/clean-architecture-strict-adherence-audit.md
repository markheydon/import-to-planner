# Clean Architecture Strict Adherence Audit (Uncle Bob Baseline)

## Executive Verdict (Hard Truth)

This codebase is **not in strict adherence** with the original Clean Architecture model from Uncle Bob’s article.  
It is layered and partially disciplined, but key boundaries are still porous, and some inner-layer code is coupled to framework concerns and presentation concerns.

If strict adherence is the target, the current state is best described as: **“Clean-ish layering, not Clean Architecture purity.”**

Update (2026-05-13): Constitution and governance-document recommendations in section B
have now been actioned. Codebase refactor recommendations in section A have now also
been implemented for feature `003-align-clean-architecture`.

## Remediation Evidence (2026-05-13)

- CSV parser implementation moved to Infrastructure: `src/ImportToPlanner.Infrastructure.Graph/CsvImportParser.cs`; Application parser implementation removed.
- `CsvHelper` package ownership moved to Infrastructure project usage and removed from Application project usage.
- Combined orchestrator retired and replaced with explicit planning/execution boundaries:
   - `IImportPlanningUseCase` and `IImportExecutionUseCase`
   - `IImportPlanningOutputBoundary` and `IImportExecutionOutputBoundary`
   - `ImportPlanningUseCase` and `ImportExecutionUseCase`
- Domain provider residue removed from `PlannerPlan` by dropping provider-shape metadata properties.
- Graph/provider exceptions are translated into neutral failure taxonomy (`PlannerOperationFailure`) at adapter boundaries.
- Presenter ownership introduced in Web:
   - `ImportPlanningPresenter`
   - `ImportExecutionPresenter`
- Workflow coordination moved into dedicated Web collaborators:
   - `WorkflowCoordinationState`
   - `ImportWorkflowCoordinator`
- Architecture regression checks added:
   - `ArchitectureComplianceTests`
   - `PlannerPlanTests`
- Runtime-mode parity hook added for Web tests by parameterising `HomePageTestContext(bool useGraphGateway = false)`.

### Remaining Follow-up

- Continue expanding planner runtime-mode parity assertions as additional planner behaviours change.

## Scope and Baseline

- Reviewed repository structure and key source files in `Domain`, `Application`, `Infrastructure`, and `Web`.
- Reviewed constitutional governance in `.specify/memory/constitution.md`.
- Assessed against the original principles:
  - Dependency rule (source dependencies point inwards only)
  - Framework independence
  - UI as a delivery mechanism, not business policy host
  - Clear separation between use cases, interface adapters, and frameworks

## Codebase Non-Conformance Findings

### 1) Application layer contains framework/data-format adapter code (High)

`Application` directly references and uses `CsvHelper`:

- `src/ImportToPlanner.Application/ImportToPlanner.Application.csproj:8`
- `src/ImportToPlanner.Application/Services/CsvImportParser.cs:2-3, 40-50`

Under strict Clean Architecture, CSV parsing is an outer-circle concern (interface adapter / gateway implementation detail), not an inner use-case concern.

### 2) Application layer leaks Graph-specific semantics (High)

The Application layer uses Graph-branded exception naming and mapping:

- `src/ImportToPlanner.Application/Exceptions/PlannerGraphExceptions.cs:3-142`

Even if the types are custom, naming and messages are Graph-specific (`PlannerGraphErrorMapper`, Graph permissions text), which ties inner policy language to an external mechanism.

### 3) Domain model carries external API residue (High)

`PlannerPlan` includes fields that are clearly Graph-shape metadata:

- `src/ImportToPlanner.Domain/PlannerPlan.cs:10-18`

`ContainerUrl` and `RawContainerType` are not core business concepts; they are transport/API artefacts. That is leakage from interface adapters into entities.

### 4) Use case returns presentation strings and UI-facing narrative text (High)

`ImportPlannerOrchestrator` builds user-facing text in use-case code:

- `src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs:162, 185, 193, 225`
- `src/ImportToPlanner.Application/Services/ImportPlannerOrchestrator.cs:213-217, 233-237, 269-273`

These are presenter/view concerns. Strict Clean Architecture expects use cases to emit response models; formatting and wording belong to presenters in outer layers.

### 5) Web component is a large workflow coordinator with mixed responsibilities (Medium-High)

`Home.razor` combines:
- UI rendering
- state machine/step gating
- authentication redirection
- file loading/parsing orchestration
- preview/execution flow control

Evidence:
- file size and mixed logic: `src/ImportToPlanner.Web/Components/Pages/Home.razor:1-1010`
- step-lock and completion policy logic: `src/ImportToPlanner.Web/Components/Pages/Home.razor:926-1009`
- orchestration flow: `src/ImportToPlanner.Web/Components/Pages/Home.razor:644-779`

This is still in an outer layer, so it is not a direct dependency-rule break, but it is far from strict interactor/controller-presenter separation.

### 6) No explicit input/output boundary interfaces for use cases (Medium)

The use case is exposed as a service interface:
- `src/ImportToPlanner.Application/Abstractions/IImportPlannerOrchestrator.cs:8-29`

But there is no explicit boundary pair (`InputBoundary`/`OutputBoundary`) and presenter implementation to decouple use-case response shaping from UI concerns. For strict interpretation, this is a structural gap.

## Constitution Non-Conformance Findings

### 1) Constitution is not centred on Clean Architecture; it is a mixed governance bundle (High)

~~The constitution includes testing, UX, performance, observability, and agent-process governance as co-equal core principles:~~

- ~~`.specify/memory/constitution.md:32-133`~~

~~These may be valid engineering rules, but they are not Clean Architecture principles. If this document is meant to enforce strict Uncle Bob adherence, it is diluted.~~

Summary of action taken (2026-05-13):
- Constitution rewritten to an architecture-only baseline in `.specify/memory/constitution.md`.
- Core principles now focus on dependency direction, technology-neutral policy,
  explicit boundaries, framework replaceability, and measurable architecture compliance.
- Version bumped from `1.3.0` to `2.0.0` to reflect substantive principle redefinition.

### 2) Constitution hard-codes framework/library choices, reducing framework independence (High)

~~- Microsoft Graph as mandatory contract: `.specify/memory/constitution.md:97-99`~~
~~- MudBlazor as mandatory UI library: `.specify/memory/constitution.md:105-111`~~

~~Strict Clean Architecture treats frameworks as replaceable tools in the outer circle, not constitutional anchors.~~

Summary of action taken (2026-05-13):
- Constitution now explicitly states frameworks and libraries are replaceable outer-layer
   implementation choices.
- Graph and MudBlazor mandates were removed from constitutional principles and guardrails.
- Framework-specific and operational policy was moved to repository governance docs
   outside the constitution.

### 3) Constitution has process/agent rules unrelated to architectural dependency discipline (Medium)

~~- Agent delegation as a core principle: `.specify/memory/constitution.md:77-91`~~
~~- Repeated AGENTS.md process mandates in guardrails: `.specify/memory/constitution.md:114-117`~~

~~This is governance/process policy, not architecture policy. It weakens architectural clarity.~~

Summary of action taken (2026-05-13):
- Agent delegation/process rules removed from constitutional principles.
- Mandatory process continuity retained in `AGENTS.md` under a new
   "Non-Constitutional Repository Policies" section.
- Non-architectural mandatory policies were centralised in
   `docs-internal/engineering-policies.md`.

### 4) Internal inconsistency indicates drift (Medium)

~~Delivery workflow still references “Fluent UI workflows”:~~
~~- `.specify/memory/constitution.md:128-129`~~

~~Repository has already standardised on MudBlazor. This mismatch weakens the constitution’s authority and precision.~~

Summary of action taken (2026-05-13):
- Removed outdated Fluent UI wording as part of constitution rewrite.
- Delivery workflow section now references architecture evidence gates and points to
   `AGENTS.md` plus `docs-internal/engineering-policies.md` for non-constitutional policy.

## Recommendations to Reach Strict Adherence

### A) Codebase Refactor Priorities

1. **Move CSV parsing implementation out of Application**
   - Keep `ICsvImportParser` boundary in Application.
   - Move `CsvImportParser` implementation and `CsvHelper` dependency to Infrastructure (or dedicated Adapters project).

2. **Remove Graph language from Application and Domain**
   - Rename `PlannerGraphExceptions.cs` to transport-agnostic terminology.
   - Replace Graph-specific message content in inner layers with neutral domain/use-case errors.
   - Remove `ContainerUrl` and `RawContainerType` from Domain entities; keep those in adapter DTOs if needed.

3. **Introduce explicit use-case boundaries**
   - Split orchestration into focused use cases (e.g., BuildPreview, ExecuteImport).
   - Add request/response models and output boundary interfaces.
   - Move final message composition to presenter layer in Web.

4. **Decompose `Home.razor`**
   - Keep component as UI composition only.
   - Move flow coordination into application-facing controller/presenter classes in the outer adapter layer.
   - Keep only interaction binding in Razor.

5. **Remove user-facing string assembly from interactors**
   - Interactors should emit structured outcome codes/data.
   - Presenter maps that to UK-English UI text.

### B) Constitution Changes for Strict Uncle Bob Alignment

1. **Narrow Principle I into enforceable architectural rules**
   - ~~Explicitly codify dependency rule.~~
   - ~~Explicitly ban framework/package dependencies in Domain and Use Cases.~~
   - ~~Require adapter-only ownership of transport, UI, and persistence details.~~

   Completed (2026-05-13): Implemented in the rewritten constitution principles and
   Architectural Guardrails section.

2. **Separate non-architecture governance from architecture constitution**
   - ~~Move agent workflow, UX, and operational process rules to separate governance docs.~~
   - ~~Keep constitution focused on architecture boundaries and dependency direction.~~

   Completed (2026-05-13): Constitution narrowed to architecture governance; moved
   non-architectural rules into `AGENTS.md` and `docs-internal/engineering-policies.md`.

3. **Remove framework lock-in language**
   - ~~Reframe Graph and MudBlazor as current implementation choices, not constitutional invariants.~~
   - ~~State replaceability expectation for frameworks and UI libraries.~~

   Completed (2026-05-13): Constitution now declares framework replaceability and no
   longer encodes Graph/MudBlazor as constitutional invariants.

4. **Add measurable conformance gates**
   - ~~Require architecture tests for project-reference direction.~~
   - ~~Require checks that Domain/Application have no forbidden package references.~~
   - ~~Require review checklist items for boundary leakage (UI strings in use cases, API artefacts in entities).~~

   Completed (2026-05-13): Added measurable architecture compliance expectations to the
   constitution and propagated these gates into `.specify/templates/plan-template.md`,
   `.specify/templates/spec-template.md`, and `.specify/templates/tasks-template.md`.

## Suggested Severity Summary

- **Critical to strictness:** framework leakage into Application, API leakage into Domain, presentation leakage into use-case layer.
- **Serious governance gap:** constitution not architecture-focused and partially framework-bound.
- **Secondary but important:** oversized UI coordinator and missing explicit presenter boundaries.

---

If strict Uncle Bob adherence is genuinely required, this is a **meaningful refactor plus constitution rewrite**, not a cosmetic tidy-up.
