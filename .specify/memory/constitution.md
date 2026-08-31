<!--
Sync Impact Report
- Version change: 2.1.0 -> 2.2.0
- Modified principles:
	- III. Boundaries Must Be Explicit and Enforced
	  -> III. Boundaries and Separation of Concerns Are Explicit
- Added sections:
	- Core Principles: V. Changes Must Be Traceable
	- Core Principles: VI. Behaviour Must Be Testable
	- Core Principles: VII. Errors Must Be Handled Explicitly
	- Core Principles: VIII. Security by Design Is Mandatory
	- Core Principles: IX. Quality Evidence Is Mandatory
	  (absorbs former V. Architectural Compliance Must Be Measurable)
- Removed sections:
	- Core Principles: V. Architectural Compliance Must Be Measurable
	  (merged into IX)
- Renumbered:
	- VI. Self-Hosted Viability Is Non-Negotiable
	  -> X. Self-Hosted Viability Is Non-Negotiable
- Follow-up TODOs:
	- None in this file. Agent-policy wording outside this constitution
	  (for example AGENTS.md still describing Copilot as a loader) is
	  out of constitution scope.
-->

# Import To Planner Constitution

## Core Principles

### I. Dependency Rule Is Absolute
Source dependencies MUST point inwards only: outer delivery and infrastructure
layers MAY depend on Application and Domain; Application MAY depend on Domain;
Domain MUST NOT depend on any outer layer. A change that introduces a
cross-layer reference violating this direction MUST NOT be merged.

Rationale: One-way dependency flow keeps policy replaceable and stops
frameworks from owning business rules.

### II. Core Policy Must Be Technology-Neutral
Domain and Application code MUST express business policy using
repository-owned types and language. Framework, transport, UI, vendor SDK,
and delivery-specific types or wording MUST remain in outer adapter layers.
This constitution MUST NOT treat any library, framework, or toolchain as an
architectural invariant.

Rationale: Technology-neutral policy stays stable when infrastructure or
delivery choices change. Stack decisions belong in tech-stack notes,
guidelines, or decision logs.

### III. Boundaries and Separation of Concerns Are Explicit
Use-case interactions MUST be modelled with explicit request and response
contracts and boundary interfaces at Application seams. Adapters MUST map
external inputs and outputs to those contracts. Presentation text assembly,
transport mapping, and persistence mapping MUST be performed outside
interactors. A specification or implementation that mixes these concerns in
an inner layer fails this principle.

Rationale: Explicit seams make policy testable and prevent presentation or
I/O from leaking into core use cases.

### IV. Frameworks and Delivery Mechanisms Are Replaceable
Frameworks, libraries, and delivery hosts are implementation choices in
outer layers, not constitutional invariants. Specifications, plans, tasks,
and pull requests MUST remain valid if the delivery stack is replaced.
A rule that would need rewriting solely because the tech stack changed
MUST NOT live in this constitution.

Rationale: Treating delivery mechanisms as replaceable tools preserves
clean architecture intent and reduces lock-in.

### V. Changes Must Be Traceable
Every specified behaviour, planned work item, task, and implementation
change MUST map to an identified requirement, user story, or defect. A
pull request MUST state which requirement or task it satisfies. Behaviour
that cannot be traced to an agreed artefact MUST NOT be introduced.

Rationale: Traceability makes review binary: either the change fulfils a
stated need, or it is scope drift.

### VI. Behaviour Must Be Testable
Every new or changed behaviour MUST be verifiable by an automated check
that can fail without requiring a full production deployment. Inner-layer
policy MUST be testable without outer adapters. A specification that
cannot be answered pass/fail, or a pull request that adds behaviour with
no failing-path test for that behaviour, fails this principle.

Rationale: Testability is an architecture property. Tool and framework
choices for running tests are not constitutional.

### VII. Errors Must Be Handled Explicitly
Operations that can fail MUST declare failure outcomes in specifications
and return structured results or typed failures in implementations.
Failures MUST NOT be swallowed, converted into success, or exposed to
end users as raw exception or diagnostic dumps. Public-facing adapters
MUST present failures in a human-friendly, actionable form; raw detail
MUST stay in diagnostics channels.

Rationale: Explicit failure contracts keep behaviour reviewable and stop
silent data loss or accidental information disclosure.

### VIII. Security by Design Is Mandatory
Trust boundaries MUST be identified for any operation that reads or
writes data, authenticates, authorises, or calls an external system.
Secrets, credentials, and tokens MUST NOT appear in source control,
logs, diagnostics, or user-facing output. Privileged operations MUST
specify who may perform them and how unauthenticated or unauthorised
requests fail. A change that cannot answer these checks MUST NOT be
merged.

Rationale: Security constraints are architectural. They MUST be present
in the design, not added after a working path exists.

### IX. Quality Evidence Is Mandatory
Every change that modifies or adds code MUST include objective evidence
that this constitution still holds: dependency direction, forbidden
inner-layer references, boundary leakage, testability of changed
behaviour, explicit failure handling, and security checks above.
Reviewers MUST treat missing evidence as non-compliance.

Rationale: Declarative rules only constrain the product when compliance
is measurable on every spec and pull request.

### X. Self-Hosted Viability Is Non-Negotiable
Self-hosting is a permanent supported delivery mode of this repository.
New capabilities MUST preserve a supported self-hosted path.
Hosted-only or commercial capabilities MUST be additive and MUST NOT
make self-hosted use depend on SaaS-specific login, billing, tenancy, or
service availability unless an equivalent self-host-compatible path
exists.

Rationale: Commercial evolution is allowed, but it MUST not turn
repository users into hosted-service dependants when the codebase is
intended to remain self-hostable.

## Architectural Guardrails

- Inner layers (Domain and Application) MUST NOT take package or
  framework dependencies that are unrelated to executing business
  policy.
- Domain entities MUST NOT contain transport-specific or
  provider-specific fields whose sole purpose is external API shape
  preservation.
- Use-case implementations MUST return structured response data, not
  user-facing prose. UI-specific wording MUST be produced by presenter
  or UI adapter layers.
- Public-facing delivery adapters MUST present failures in a
  human-friendly, actionable form. Raw exception detail MUST stay in
  diagnostics channels and MUST NOT be exposed directly to end users.
- External provider shapes and vendor models MUST be translated at
  adapter boundaries before reaching Application or Domain.
- Commercial or hosted-service flows (for example subscription gating,
  hosted account login, billing capture, or tenant administration) MUST
  be isolated so they do not block or degrade legitimate self-hosted
  operation.
- Secrets and security-sensitive values MUST NOT be committed or
  emitted to users.
- Architecture-impacting exceptions MUST be documented in a pull
  request with explicit rationale, alternatives considered, and a
  retirement plan if temporary.

## Delivery Workflow and Quality Gates

- Planning artefacts MUST include an architecture impact statement
  covering dependency direction, boundary changes, adapter
  responsibilities, traceability, testability, error handling, and
  security trust boundaries.
- Planning and review for product or workflow changes MUST state how
  self-hosted users continue to complete the supported import journey
  when hosted-only features are added.
- Pull requests MUST provide evidence for all affected constitutional
  gates. Absence of evidence is a fail.
- Reviewers MUST block merges when constitutional evidence is absent
  or when policy leakage is identified in Domain or Application
  layers.
- Repository process, agent policy, and operational policies that are
  not architecture-constitutional are defined in AGENTS.md and
  docs-internal/engineering-policies.md and MUST still be followed.
  Stack, library, and convention choices MUST live in those documents,
  tech-stack notes, guidelines, or decision logs — not in this
  constitution.

## Governance

This constitution defines the Spec Kit architecture governance baseline
for this repository. It applies to all specifications, plans, tasks,
and implementations, regardless of which agent or human author produced
them.

Where guidance conflicts, repository precedence in AGENTS.md governs
conflict resolution. This constitution remains the architecture
authority; AGENTS.md remains the agent-policy authority.

Amendment process:
1. Propose the amendment with a clear rationale and impacted
   principles or sections.
2. Keep this document free of stack, library, and convention rules.
3. Update related workflow guidance in the same change where feasible.
4. Obtain maintainer approval before merge.

Versioning policy:
- MAJOR: Backward-incompatible governance changes or principle
  removals or redefinitions.
- MINOR: New principle or section, or materially expanded governance
  requirements.
- PATCH: Clarifications, wording improvements, and non-semantic
  refinements.

Compliance review expectations:
- Constitution compliance MUST be checked in planning and pull request
  review as a set of yes/no questions derived from the principles
  above.
- Non-compliance MUST be tracked as explicit follow-up work or
  resolved before release.

**Version**: 2.2.0 | **Ratified**: 2026-05-09 | **Last Amended**: 2026-08-31
