# Engineering Policies (Non-Constitutional)

This document preserves repository policies that govern delivery quality, operations,
and workflow but are intentionally outside the architecture constitution.

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

- External-provider implementation details (including Graph and Kiota specifics) MUST stay
  in adapter/infrastructure layers.
- Because supported Planner scenarios currently rely on Microsoft Graph beta endpoints,
  Graph contract changes MUST include compatibility notes and mitigation guidance.
- The repository supports two explicit authority modes only: self-hosted single-tenant and
  approved hosted shared multi-tenant. Changes to tenant behaviour MUST preserve the
  self-hosted single-tenant baseline and stay aligned with the approved hosted contracts and
  runbooks.
- Security-sensitive values (credentials, certificate material, tenant identifiers)
  MUST NOT be committed and MUST use approved configuration paths.

## Delivery Evidence

- Pull requests MUST include quality evidence for testing, UX impact, operational safety,
  and performance impact when the change affects those areas.
- Large or risky changes SHOULD be delivered incrementally with verifiable checkpoints.
