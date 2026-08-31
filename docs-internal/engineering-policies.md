# Engineering Policies (Non-Constitutional)

This document preserves repository policies that govern delivery quality, operations,
and workflow but are intentionally outside the architecture constitution.

Stack-named rules (projects, packages, SDKs, UI libraries, and concrete
architecture tests) belong here, in `docs-internal/microsoft-graph-guidelines.md`,
or in decision logs. The constitution states the yes/no architecture rules; this
file records how those rules are applied in this repository.

## Repository Layer Map

Allowed dependency direction for this solution:

- Outer adapters MAY depend on Application and Domain:
  `ImportToPlanner.Web`, `ImportToPlanner.Infrastructure.Graph`, and
  `ImportToPlanner.Commercial`.
- Application MAY depend on Domain.
- Domain MUST NOT depend on any outer project.

Web owns UI, authentication composition, and host configuration.
`Infrastructure.Graph` owns CSV parsing and Microsoft Graph planner adapters.
`Commercial` owns hosted commercial-account persistence and related adapters.
Application and Domain own policy using repository-owned types only.

## Architecture Evidence Gates

Pull requests that add or change code MUST include evidence for:

- Dependency direction (Web / Infrastructure.Graph / Commercial → Application → Domain).
- Forbidden-reference validation for Domain and Application (no provider, UI,
  or delivery packages in inner layers). The automated check lives in
  `tests/ImportToPlanner.Tests/ArchitectureComplianceTests.cs`.
- Boundary leakage checks: use-case outputs and domain models MUST NOT carry
  provider payload residue, SDK exception taxonomies, UI component types, or
  delivery-specific wording.

## Testing and Runtime Behaviour

- Every behaviour change MUST be verified by automated tests at the smallest practical
  level first (unit, then integration where boundaries are crossed).
- Bug fixes MUST include a regression test that fails before the fix and passes after.
- Changes affecting planner gateway behaviour MUST verify the single supported Graph
  runtime path and any authority-specific auth guard behaviour impacted by the change.
- Graph-facing behaviour changes SHOULD include integration-style verification using the
  established repository test patterns and approved test doubles.

## User Experience and Accessibility

- User-facing workflows MUST keep consistent semantics across validation, preview,
  confirmation, and execution reporting.
- End-user wording and contributor-facing documentation intended for users MUST use UK
  English.
- Public-facing and UI-facing failures MUST be shown as graceful, human-friendly,
  actionable messages and MUST NOT expose raw exception details to end users.
- Accessibility and responsive behaviour MUST be preserved for primary workflows across
  desktop and mobile layouts.

## Performance and Operational Safety

- Import planning and validation paths MUST define measurable performance expectations
  when behaviour or algorithmic complexity changes.
- Changes MUST avoid avoidable repeated remote calls and avoidable superlinear hot-path
  behaviour unless an exception is documented.
- Operationally significant workflow steps MUST emit actionable diagnostics without
  exposing secrets or tenant-sensitive values.
- Dry-run safety and explicit confirmation behaviour MUST remain first-class safeguards
  for import execution flows.

## External Integration and Scope Constraints

- External-provider implementation details MUST stay in adapter/infrastructure
  layers. That includes Microsoft Graph API shapes, Kiota models, UI component
  behaviours, SDK exception taxonomies, and API payload residue. Map those at
  the adapter boundary before they reach Application or Domain.
- Because supported Planner scenarios currently rely on Microsoft Graph beta endpoints,
  Graph contract changes MUST include compatibility notes and mitigation guidance.
- The repository supports two explicit authority modes only: self-hosted single-tenant and
  approved hosted shared multi-tenant. Changes to tenant behaviour MUST preserve the
  self-hosted single-tenant baseline and stay aligned with the approved hosted contracts and
  runbooks.
- Commercial or hosted-service capabilities MUST be designed so self-hosters retain a
  supported path without depending on SaaS-specific login, billing, subscription state, or
  hosted control-plane availability.
- When commercial mode is disabled, startup and runtime behaviour MUST avoid requiring
  commercial account persistence infrastructure.
- Commercial account lifecycle changes MUST preserve delete, retention, and restore semantics:
  immediate block after delete, retention-window restore, and purge only after expiry.
- Feature proposals and pull requests that add hosted-only behaviour MUST explicitly state
  the self-hosted impact, including how self-hosted operators bypass or replace any hosted
  service workflow that would otherwise be irrelevant or obstructive.
- Security-sensitive values (credentials, certificate material, tenant identifiers)
  MUST NOT be committed and MUST use approved configuration paths.

## Delivery Evidence

- Pull requests MUST include quality evidence for testing, UX impact, operational safety,
  and performance impact when the change affects those areas.
- Pull requests that change architecture-relevant code MUST also include the
  architecture evidence gates listed above.
- Large or risky changes SHOULD be delivered incrementally with verifiable checkpoints.
