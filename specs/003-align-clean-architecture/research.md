# Research: Clean Architecture Alignment

## Decision 1: Keep CSV parsing as an Application boundary, but move its implementation to Infrastructure

- Decision: Retain a core-facing parser contract in Application and move the CsvHelper-based parser implementation out of `ImportToPlanner.Application` into an Infrastructure adapter.
- Rationale: CSV parsing is an input-adapter concern. Keeping only the contract in Application preserves use-case independence while removing the current framework/data-format leakage identified by the audit.
- Alternatives considered: Leave the parser in Application with stricter wrapping. Rejected because the implementation would still pull CsvHelper and file-format concerns into an inner layer.

## Decision 2: Replace the current orchestrator with separate planning and execution use cases

- Decision: Split `IImportPlannerOrchestrator` and `ImportPlannerOrchestrator` into distinct planning and execution use-case contracts and implementations.
- Rationale: The feature spec requires separate use-case boundaries, and the audit shows the current orchestrator mixes preview and execution responsibilities as well as message shaping.
- Alternatives considered: Keep one orchestrator and add helper methods. Rejected because it would preserve an ambiguous boundary and make presenter ownership harder to enforce.

## Decision 3: Use explicit output-boundary interfaces with Web-owned presenters

- Decision: Define explicit output-boundary interfaces for planning and execution in Application, with presenter implementations in Web that convert neutral results into UI-facing text and view state.
- Rationale: This satisfies the constitution’s explicit-boundary requirement and removes message composition from use cases.
- Alternatives considered: Return only response DTOs and let the page shape them directly. Rejected because it would leave presentation ownership diffused and keep `Home.razor` as the de facto presenter.

## Decision 4: Replace Graph-branded inner-layer exception concepts with neutral failure categories

- Decision: Remove Graph-branded exception naming and user-message mapping from Application, replacing them with neutral business-relevant failure categories and structured outcome data.
- Rationale: The constitution requires technology-neutral core policy. A neutral taxonomy lets Graph and in-memory runtime modes share the same use-case contracts.
- Alternatives considered: Keep current exception types and only rename displayed strings. Rejected because the provider coupling would remain embedded in Application semantics.

## Decision 5: Remove provider metadata residue from Domain entities

- Decision: Remove `ContainerUrl` and `RawContainerType` from `PlannerPlan` and keep equivalent provider metadata, where still needed, inside adapter-specific models or mapping code.
- Rationale: Those fields preserve external API shape rather than business policy concepts, which violates the domain-purity requirement.
- Alternatives considered: Mark the fields as adapter-only and keep them on the domain entity. Rejected because that still leaks provider concerns into the core model.

## Decision 6: Move workflow coordination out of `Home.razor` into dedicated Web collaborators

- Decision: Keep `Home.razor` focused on composition, data binding, and event delegation, while moving stepped workflow coordination and stale-preview handling into dedicated Web-layer collaborator classes.
- Rationale: The audit identifies the page as a large coordinator. Keeping that coordination in the Web layer maintains the dependency rule while making the UI easier to reason about and test.
- Alternatives considered: Move workflow coordination into Application. Rejected because UI progression, step locking, and authentication-adjacent flow are adapter concerns, not use-case policy.

## Decision 7: Preserve existing workflow semantics and confirmation safeguards unchanged

- Decision: Treat current validation, preview, confirmation, and execution semantics as the behavioural baseline and refactor around them rather than redesigning the workflow.
- Rationale: The feature is architectural alignment work, not a product-capability change. The engineering policies also require dry-run safety and explicit confirmation to remain first-class safeguards.
- Alternatives considered: Simplify the workflow while refactoring. Rejected because that would expand scope and weaken parity claims.

## Decision 8: Use focused architecture evidence plus runtime-mode verification

- Decision: Implementation evidence will combine focused use-case and workflow regression tests with explicit checks for forbidden inner-layer references and runtime-mode verification when planner behaviour changes.
- Rationale: The constitution requires measurable architecture compliance, and repository policy requires smallest-practical tests first plus `PlannerGateway:UseGraph=true/false` verification when planner-facing behaviour is affected.
- Alternatives considered: Rely on broad end-to-end tests and manual review. Rejected because that would not make architecture compliance measurable or localise regressions effectively.

## Decision 9: Keep the existing four-project solution structure

- Decision: Deliver the refactor inside the existing `Domain`, `Application`, `Infrastructure.Graph`, and `Web` projects.
- Rationale: The current issue is incorrect ownership, not missing deployment units. New projects would add packaging complexity without improving boundary clarity for this feature.
- Alternatives considered: Add a dedicated adapters project for CSV parsing or presenters. Rejected for now because the existing Infrastructure and Web projects already provide the correct outer-layer homes.

## Decision 10: Rework tests around planning/execution seams rather than the retiring orchestrator

- Decision: Replace current orchestrator-focused tests with planning-use-case and execution-use-case tests, then update bUnit tests to target the new Web coordinator/presenter wiring.
- Rationale: Tests should align with the new explicit seams, not preserve the old service shape. This also supports smaller-scope regression checks before broader workflow tests.
- Alternatives considered: Keep the existing orchestrator tests and adapt them minimally. Rejected because those tests would encode a boundary the feature is explicitly removing.

## Repository Policy Checkpoints

- Delegation checkpoint: coding, architecture, and test implementation were handled by the C# Expert agent path in line with `AGENTS.md`.
- Runtime-mode checkpoint: planner-facing behaviour changes include parity-focused test coverage and configurable test context support for both `PlannerGateway:UseGraph` modes.
- Language checkpoint: UI and documentation wording remains UK English.
- Boundary checkpoint: Domain/Application keep technology-neutral contracts; provider and presentation translation remains at Infrastructure/Web boundaries.
