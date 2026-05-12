<!--
Sync Impact Report
- Version change: 1.2.0 -> 1.3.0
- Modified principles:
	- None
- Added sections:
	- None
- Removed sections:
	- None
- Engineering Guardrail updated:
	- UI library changed from Fluent UI Blazor to MudBlazor
- Templates requiring updates:
	- None (MudBlazor is library-agnostic from a skill perspective)
- Follow-up TODOs:
	- None
-->

# Import To Planner Constitution

## Core Principles

### I. Clean Architecture Code Quality
All production code MUST preserve clear architectural boundaries across Domain,
Application, Infrastructure, and Web layers. New changes MUST keep business rules in
Domain/Application, avoid leaking framework-specific types into core logic, and keep
methods and components focused on one responsibility. Public code paths MUST be readable,
reviewable, and documented when intent is non-obvious.

Rationale: High quality architecture and readability reduce defects, make onboarding
faster, and keep long-term maintenance costs predictable.

### II. Mandatory Testing Standards
Every behaviour change MUST be verified by automated tests at the smallest practical
level (unit first, then integration where boundaries are crossed). Bug fixes MUST include
a regression test that fails before the fix and passes after. Changes that affect planner
gateway orchestration or Graph integration MUST include integration-style verification
using approved test doubles and repository testing patterns in the repository test suite.
Changes affecting planner gateway behaviour MUST verify compatibility for both configured
runtime modes (`PlannerGateway:UseGraph` true and false), unless the change is explicitly
scoped to one mode and documented as such.

Rationale: Reliable test evidence prevents regressions and allows safe iteration on a
Graph-integrated workflow.

### III. User Experience Consistency
User-facing flows MUST stay consistent across validation, preview, confirmation, and
execution reporting. UI wording, error states, and action semantics MUST match existing
application patterns and use clear UK English. Accessibility and responsive behaviour MUST
be preserved for primary workflows on desktop and mobile layouts.

Rationale: Consistent UX reduces user error during imports and improves trust in a tool
that performs potentially large task operations.

### IV. Performance and Responsiveness Budgets
Import planning and validation operations MUST remain responsive for expected CSV sizes.
New work MUST define measurable performance expectations and MUST NOT introduce avoidable
O(n^2) or repeated remote-call patterns in hot paths. Any change that may affect latency,
memory, or throughput MUST include lightweight measurement evidence or justified limits.
When behaviour differs between in-memory and Graph-backed execution, performance impact
MUST be assessed for the affected mode and any known trade-offs documented.

Rationale: The import workflow must remain predictable and fast enough for practical,
interactive use.

### V. Observability and Safe Operations
Operationally significant steps MUST emit actionable diagnostics without exposing secrets
or tenant-sensitive values. Errors from external dependencies MUST be translated into
clear, user-safe outcomes and preserve enough context for debugging. Dry-run safety MUST
be maintained as a first-class behaviour for import execution.
Because this repository currently depends on Microsoft Graph beta endpoints for supported
planner scenarios, changes to Graph contracts MUST include a compatibility note describing
expected impact and mitigation when beta API behaviour changes.

Rationale: Strong observability and safe failure modes are required when integrating with
external Graph services and user data.

### VI. Agent Delegation Discipline and Process Continuity
Where repository-registered custom agents exist for a task category, implementation
workflow commands MUST use those agents for applicable work. For this repository,
coding, architecture, and test implementation tasks MUST be delegated to the C# Expert
agent unless a task falls outside its scope or a maintainer-approved exception is
documented in the related plan or pull request. Agents MUST remain aware of and strictly
follow the skill mapping, process expectations, and operational constraints defined in
`AGENTS.md` throughout all work phases—not just at initial delegation. This includes
mandatory build/rebuild operations, tool selection, and testing discipline. If AGENTS.md
is not referenced mid-session, agents MUST re-read it to restore contextual compliance
before continuing work or handing off to the next phase.

Rationale: Explicit delegation and continuous process adherence keeps implementation
behaviour consistent with repository skills, reduces drift between governance intent and
execution practice, and ensures users receive fully prepared code without manual intervention.

## Engineering Guardrails

- Language and framework decisions MUST remain aligned to the current .NET and Blazor
	stack unless an approved architectural decision records a change.
- Microsoft Graph contracts MUST remain the domain/application-facing contract; Kiota
	implementation details MUST stay internal to infrastructure.
- Both runtime modes (Graph and in-memory gateway) MUST remain operable unless a
	governance-approved scope decision explicitly changes that requirement.
- The repository scope is currently single-tenant; multi-tenant behaviour MUST NOT be
	introduced without an explicit constitution amendment or approved scope decision.
- End-user and contributor documentation, including comments intended for users, MUST use
	UK English.
- User interface implementation MUST use MudBlazor components and patterns as the standard
	library. Custom CSS or hand-authored HTML overrides MUST be treated as a last resort and
	only used where MudBlazor capabilities are insufficient or where a known issue is
	documented.
- Implementation choices for MudBlazor component behaviour MUST follow the component
	library's documented patterns and best practices. Any deviation MUST be documented with
	rationale.
- Security-sensitive values (credentials, certificate material, tenant identifiers)
	MUST NOT be committed and MUST be handled through approved configuration paths.
- Agent process requirements defined in `AGENTS.md` (skill usage, build/rebuild operations,
	testing discipline, delegation scope) MUST be treated as mandatory governance and adhered to
	continuously throughout all work phases. Agents MUST re-read `AGENTS.md` if context is lost
	during iteration to restore compliance before handing off work.

## Delivery Workflow and Quality Gates

- Every change proposal MUST map to explicit requirements and measurable outcomes.
- Pull requests MUST include evidence for affected constitutional gates: code quality,
	testing, UX consistency, and performance expectations.
- Changes that affect hosting/orchestration MUST preserve AppHost build viability and CI
	parity for both solution-level and AppHost validation paths.
- Implementation planning and execution MUST include an agent-delegation check against
	`AGENTS.md`, and any exception MUST be documented with rationale.
- Pull requests with custom HTML/CSS workarounds in Fluent UI workflows MUST include
	evidence that component-native and MCP-guided options were evaluated first.
- Reviewers MUST block merges when constitutional gates are unmet or unsupported by
	evidence.
- Large or risky changes SHOULD be delivered incrementally with verifiable checkpoints.

## Governance

This constitution defines the Spec Kit governance baseline for this repository.
Where guidance conflicts, repository precedence defined in `.github/copilot-instructions.md`
and `AGENTS.md` governs conflict resolution.

Amendment process:
1. Propose the amendment with a clear rationale and impacted principles/sections.
2. Update related templates and workflow guidance in the same change where feasible.
3. Obtain maintainer approval before merge.

Versioning policy:
- MAJOR: Backward-incompatible governance changes or principle removals/redefinitions.
- MINOR: New principle/section or materially expanded governance requirements.
- PATCH: Clarifications, wording improvements, and non-semantic refinements.

Compliance review expectations:
- Constitution compliance MUST be checked in planning and pull request review.
- Non-compliance MUST be tracked as explicit follow-up work or resolved before release.

**Version**: 1.3.0 | **Ratified**: 2026-05-09 | **Last Amended**: 2026-05-12
