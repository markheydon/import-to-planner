# Research: CSV To Planner Import Workflow

## Decision 1: Platform and UI stack
- Decision: Implement the feature in .NET 10 LTS Blazor web app using Fluent UI components.
- Rationale: The application is structured around .NET 10, Blazor pages, and Fluent UI controls; this stack ensures consistency and delivery speed.
- Alternatives considered:
  - Introduce a separate CLI tool: rejected because current user workflow is browser-based with explicit confirmation/reporting.
  - Build a separate frontend stack: rejected due to unnecessary complexity and architecture drift.

## Decision 2: Runtime mode compatibility
- Decision: Ensure behavioural parity across in-memory and Graph gateway modes for validation, preview, and execution outcomes.
- Rationale: Both modes must be supported—in-memory for local validation and development, Graph for live tenant operations.
- Alternatives considered:
  - Graph-only behaviour: rejected because it breaks local development/test workflows.
  - In-memory-only behaviour: rejected because production utility requires Graph execution.

## Decision 3: Duplicate detection and idempotency
- Decision: Use task name only for match detection; skip matched tasks and report `already exists`.
- Rationale: Clarified requirement and predictable operator mental model for single-use imports.
- Alternatives considered:
  - Composite matching (name + bucket/description): rejected because requirement explicitly selected name-only.
  - Always create new tasks: rejected due to duplicate risk.

## Decision 4: Row-failure execution strategy
- Decision: Continue remaining rows when one row fails; produce partial success/failure report.
- Rationale: Maximises useful completion while preserving transparent per-row outcomes.
- Alternatives considered:
  - Fail-fast: rejected because one bad row blocks unrelated valid work.
  - All-or-nothing rollback: rejected because external task creation rollback guarantees are not straightforward and increase complexity.

## Decision 5: Transient Graph error policy
- Decision: Retry once per failed row for transient Graph errors, then mark row failed.
- Rationale: Provides basic resilience while controlling latency and avoiding indefinite retries.
- Alternatives considered:
  - No retry: rejected as too brittle for transient network/throttling conditions.
  - Multi-retry exponential backoff: rejected for this kickoff scope to keep behaviour simple and bounded.

## Decision 6: Stale preview handling
- Decision: Block execution if planner state changes after preview; require a fresh preview before execution.
- Rationale: Maintains explicit user confirmation semantics and prevents drift between reviewed plan and executed actions.
- Alternatives considered:
  - Execute stale preview anyway: rejected due to mismatch risk.
  - Auto-refresh without confirmation: rejected because it weakens explicit approval guarantees.

## Decision 7: Contract surface for planning
- Decision: Treat the user workflow and application abstractions (`ICsvImportParser`, `IImportPlannerOrchestrator`, `IPlannerGateway`) as the feature contract surface.
- Rationale: The project is an internal web utility without public HTTP API contracts; behaviour is defined through UI flow and application interfaces.
- Alternatives considered:
  - Define REST API contract: rejected because no public API surface is required for this feature.
