<!--
Sync Impact Report
- Version change: 1.3.0 -> 2.0.0
- Modified principles:
	- I. Clean Architecture Code Quality -> I. Dependency Rule Is Absolute
	- II. Mandatory Testing Standards -> II. Core Policy Must Be Technology-Neutral
	- III. User Experience Consistency -> III. Boundaries Must Be Explicit and Enforced
	- IV. Performance and Responsiveness Budgets -> IV. Frameworks and Delivery Mechanisms Are Replaceable
	- V. Observability and Safe Operations -> V. Architectural Compliance Must Be Measurable
	- VI. Agent Delegation Discipline and Process Continuity -> Removed (moved to AGENTS.md)
- Added sections:
	- Architectural Guardrails
- Removed sections:
	- Engineering Guardrails
- Templates requiring updates:
	- ✅ updated: .specify/templates/plan-template.md
	- ✅ updated: .specify/templates/spec-template.md
	- ✅ updated: .specify/templates/tasks-template.md
	- ⚠ pending: .specify/templates/commands/*.md (directory not present in repository)
- Follow-up TODOs:
	- None
-->

# Import To Planner Constitution

## Core Principles

### I. Dependency Rule Is Absolute
Source dependencies MUST point inwards only: Web and Infrastructure MAY depend on
Application and Domain; Application MAY depend on Domain; Domain MUST NOT depend on any
outer layer. Cross-layer references that violate this direction are prohibited.

Rationale: Enforcing one-way dependency flow preserves replaceability and prevents policy
logic from drifting into frameworks.

### II. Core Policy Must Be Technology-Neutral
Domain and Application code MUST express business policy using repository-owned types and
language. Framework, transport, UI, and vendor-specific concepts (including SDK exception
taxonomies, API payload residue, and delivery-specific wording) MUST remain in outer
adapter layers.

Rationale: Technology-neutral policy keeps use cases stable when infrastructure choices
change.

### III. Boundaries Must Be Explicit and Enforced
Use-case interactions MUST be modelled with explicit request/response contracts and
boundary interfaces at Application seams. Adapters MUST map external inputs and outputs
to these contracts, and presentation text assembly MUST be performed outside interactors.

Rationale: Explicit boundaries make policy behaviour testable and prevent presentation or
transport concerns from leaking into core use cases.

### IV. Frameworks and Delivery Mechanisms Are Replaceable
Frameworks and libraries are implementation choices in outer layers, not constitutional
invariants. The constitution MUST NOT mandate specific UI libraries, SDKs, or transport
providers as irreplaceable architecture components.

Rationale: Treating frameworks as replaceable tools maintains clean architecture intent
and reduces long-term lock-in.

### V. Architectural Compliance Must Be Measurable
Every change that modifies or adds code MUST include objective architecture evidence:
dependency direction validation, forbidden-reference validation for inner layers, and
boundary leakage checks for use-case outputs and domain models.

Rationale: Testable architecture gates prevent drift and make compliance review consistent
across contributors.

## Architectural Guardrails

- Domain and Application projects MUST NOT take direct package or framework dependencies
  that are unrelated to business policy execution.
- Domain entities MUST NOT contain transport-specific or provider-specific fields whose
  sole purpose is external API shape preservation.
- Use-case implementations MUST return structured response data, not user-facing prose.
  UI-specific wording MUST be produced by presenter or UI adapter layers.
- External provider details (for example Microsoft Graph API shapes, Kiota models, and UI
  component behaviours) MUST be translated at adapter boundaries before reaching
  Application or Domain.
- Architecture-impacting exceptions MUST be documented in a pull request with explicit
  rationale, alternatives considered, and a retirement plan if temporary.

## Delivery Workflow and Quality Gates

- Planning artefacts MUST include an architecture impact statement covering dependency
  direction, boundary changes, and adapter responsibilities.
- Pull requests MUST provide evidence for all affected constitutional architecture gates.
- Reviewers MUST block merges when architectural compliance evidence is absent or when
  policy leakage is identified in Domain/Application layers.
- Repository process and operational policies that are not architecture-constitutional are
  defined in AGENTS.md and docs-internal/engineering-policies.md and MUST still be
  followed.

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

**Version**: 2.0.0 | **Ratified**: 2026-05-09 | **Last Amended**: 2026-05-13
