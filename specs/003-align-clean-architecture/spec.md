# Feature Specification: Clean Architecture Alignment

**Feature Branch**: `003-align-clean-architecture`  
**Created**: 2026-05-13  
**Status**: Draft  
**Input**: User description: "Review the clean architecture audit and specify the remaining codebase changes needed to bring the application into line with the updated constitution and the report recommendations."

## Clarifications

### Session 2026-05-13

- Q: Which use-case boundary shape should this feature require? → A: Split into separate planning and execution use cases with their own contracts.
- Q: What presenter boundary should this feature require? → A: Require explicit output-boundary or presenter interfaces for planning and execution use cases.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Restore Inner-Layer Purity (Priority: P1)

As a maintainer, I want business policy code to use only repository-owned concepts so that the core of the system is no longer coupled to file-format, provider, or presentation concerns.

**Why this priority**: This is the highest-value architectural correction because it addresses the audit's most serious boundary violations in the core policy layers.

**Independent Test**: Can be fully tested by reviewing the affected core-policy slices and running focused automated checks that confirm the refactored import parsing, error handling, and domain models no longer depend on outer-layer concerns while preserving current workflow behaviour.

**Acceptance Scenarios**:

1. **Given** a refactored core-policy slice, **When** maintainers inspect its dependencies and public models, **Then** it contains only business-relevant concepts and no provider-specific, file-format-specific, or UI-facing wording.
2. **Given** an import workflow that previously depended on outer-layer details, **When** the refactored flow is executed, **Then** the same business decisions are produced without leaking those details into Domain or Application.

---

### User Story 2 - Make Use-Case Boundaries Explicit (Priority: P2)

As a maintainer, I want import planning and import execution to be expressed as separate use cases with explicit request, response, and output-boundary contracts so that each behaviour is testable without depending on UI wording or adapter-specific shapes.

**Why this priority**: Clear input, output, and presenter boundaries are necessary to enforce the constitution's requirement for explicit seams and to keep future changes from reintroducing leakage.

**Independent Test**: Can be fully tested by invoking the refactored use cases through their contracts and verifying that presenter implementations can transform the structured outcomes into UI-facing wording without changing use-case code.

**Acceptance Scenarios**:

1. **Given** an import planning request, **When** the planning use case completes, **Then** it delivers structured outcome data through an explicit output boundary that a presenter can render.
2. **Given** an import execution failure, **When** the execution use case reports the outcome, **Then** the failure is represented through an explicit output boundary as neutral business-relevant data rather than transport-specific or user-facing prose.

---

### User Story 3 - Reduce UI Workflow Coupling (Priority: P3)

As a maintainer, I want the main import page to focus on UI composition and interaction binding so that workflow coordination can evolve without turning the page into the place where business and adapter rules accumulate.

**Why this priority**: The current page-level coordination is an outer-layer problem rather than a direct dependency-rule break, but leaving it unchanged would make the other boundary improvements harder to preserve.

**Independent Test**: Can be fully tested by exercising the stepped import workflow and verifying that validation, preview, confirmation, and execution behaviour still work while the page delegates coordination responsibilities to dedicated collaborators.

**Acceptance Scenarios**:

1. **Given** a user moves through the stepped import workflow, **When** they validate, preview, confirm, and execute an import, **Then** each step behaves consistently with the current product workflow.
2. **Given** the page component after refactoring, **When** maintainers review its responsibilities, **Then** workflow coordination logic is delegated outside the page and the page remains responsible only for UI composition and interaction handling.

### Edge Cases

- How does the system behave when external container metadata contains values that are not part of the business model but are still needed at the adapter boundary?
- How does the system preserve actionable failure reporting when provider-specific exceptions are translated into neutral outcome data?
- What happens when the refactored workflow is exercised under each supported planner gateway runtime mode?
- What happens when a user revisits earlier workflow steps after preview data or execution outcomes have already been generated?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST keep file-import parsing implementation outside the core policy layers while preserving a core-facing parsing contract for use cases.
- **FR-002**: The system MUST replace provider-branded and transport-branded error concepts in core policy layers with neutral business-relevant error categories and outcome data.
- **FR-003**: The system MUST remove external API residue from domain entities so that core models contain only business-relevant planner, container, bucket, and task concepts.
- **FR-004**: The system MUST expose import planning and import execution as separate use cases with explicit request, response, and output-boundary contracts at the application boundary.
- **FR-005**: The system MUST move user-facing message composition outside use-case implementations so that presenters or UI adapters own wording.
- **FR-005a**: The system MUST provide explicit presenter or output-boundary interfaces for the planning and execution use cases so that response shaping ownership is unambiguous.
- **FR-006**: The system MUST reduce the main import page's responsibilities to UI composition, state binding, and delegation to dedicated workflow collaborators.
- **FR-007**: The system MUST preserve existing validation, preview, confirmation, and execution workflow semantics during the refactor.
- **FR-008**: The system MUST provide regression coverage for every affected behaviour slice at the smallest practical automated-test level before relying on broader workflow checks.
- **FR-009**: The system MUST verify behaviour parity for affected planner scenarios across both supported runtime modes whenever planner behaviour is changed by the refactor.
- **FR-010**: The system MUST produce architecture compliance evidence for dependency direction, forbidden references in inner layers, and leakage of presentation or adapter concerns across boundaries.

### Quality and Non-Functional Requirements *(mandatory)*

- **NFR-001 Dependency Direction**: Solution MUST preserve allowed dependency direction (Web/Infrastructure -> Application -> Domain).
- **NFR-002 Inner-Layer Purity**: Domain/Application behaviour MUST remain technology-neutral and MUST NOT leak framework, transport, or presentation concerns.
- **NFR-003 Boundary Contracts**: Use-case input, output, and presenter-boundary contract changes MUST be explicit, including where adapter mapping and presentation mapping occur.
- **NFR-004 Framework Replaceability**: Features MUST treat framework/library choices as replaceable adapter concerns, not architectural invariants.
- **NFR-005 Compliance Evidence**: Feature delivery MUST define measurable architecture evidence (dependency checks, forbidden-reference checks, and boundary leakage checks).
- **NFR-006 Policy Alignment**: Feature MUST identify applicable non-constitutional repository policies from `docs-internal/engineering-policies.md` and `AGENTS.md`.
- **NFR-007 Workflow Consistency**: User-facing workflow safeguards, including dry-run style validation and explicit confirmation before execution, MUST remain intact.
- **NFR-008 Documentation Language**: Contributor-facing and user-facing wording introduced by this feature MUST remain in UK English.

### Key Entities *(include if feature involves data)*

- **Import Planning Request**: The information required by the planning use case to analyse source tasks, target planner context, and preview decisions before execution.
- **Import Planning Result**: Structured outcome data returned by the planning use case describing validation findings, task mappings, warnings, and previewable actions without user-facing prose.
- **Import Execution Request**: The confirmed instruction set required by the execution use case to execute an approved import against the selected planner context.
- **Import Execution Result**: Structured outcome data returned by the execution use case describing completed work, skipped work, warnings, and failures in a presenter-neutral form.
- **Workflow Coordination State**: The minimum state required to move between validation, preview, confirmation, and execution without embedding business decisions in the page itself.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of code changed by this feature in core policy layers is free from provider-specific, file-format-specific, framework-specific, and user-facing presentation references during architecture compliance review.
- **SC-002**: 100% of affected import planning and import execution use cases expose explicit request, response, and output-boundary contracts, and neither use case directly emits user-facing prose.
- **SC-003**: The primary import workflow can still be completed end-to-end without adding new mandatory user steps or removing existing safeguards.
- **SC-004**: All automated tests covering affected behaviour pass, and each refactored behaviour slice gains at least one focused regression check.
- **SC-005**: Affected planner behaviours are verified in both supported runtime modes with equivalent user-visible outcomes for the same approved import scenario.
- **SC-006**: Architecture review can trace each moved responsibility to a single boundary owner without unresolved ambiguity about whether the responsibility belongs in Domain, Application, Web, or Infrastructure.
- **SC-007**: No new architecture boundary violations are introduced while resolving the audit findings covered by this feature.

## Assumptions

- This feature is limited to resolving the open codebase refactor recommendations in the audit and does not add new end-user import capabilities.
- Existing planner import workflow semantics, safeguards, and supported scenarios remain the behavioural baseline unless a deviation is explicitly documented.
- User-facing wording may be revised for clarity when it moves to presenters or UI adapters, provided the meaning and safeguards remain equivalent and wording stays in UK English.
- The repository's existing automated test strategy and architecture evidence checks will be extended rather than replaced.
- The current orchestrator responsibilities will be decomposed into separate planning and execution use cases within this feature rather than retained as a single application boundary.
- Explicit presenter or output-boundary interfaces are part of this feature's scope and are not deferred to a later cleanup.
- Multi-tenant support, new external providers, and broad product redesign are out of scope for this feature.
