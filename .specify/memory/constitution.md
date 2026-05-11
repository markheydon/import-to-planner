<!--
Sync Impact Report
- Version change: 1.0.0 -> 1.0.1
- Modified principles:
	- None
- Added sections:
	- None
- Removed sections:
	- None
- Templates requiring updates:
	- ✅ none required (no constitutional gate changes)
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
- Security-sensitive values (credentials, certificate material, tenant identifiers)
	MUST NOT be committed and MUST be handled through approved configuration paths.

## Delivery Workflow and Quality Gates

- Every change proposal MUST map to explicit requirements and measurable outcomes.
- Pull requests MUST include evidence for affected constitutional gates: code quality,
	testing, UX consistency, and performance expectations.
- Changes that affect hosting/orchestration MUST preserve AppHost build viability and CI
	parity for both solution-level and AppHost validation paths.
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

**Version**: 1.0.1 | **Ratified**: 2026-05-09 | **Last Amended**: 2026-05-11
