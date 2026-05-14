# Import Use-Case Contracts

## Purpose

This feature introduces explicit application-boundary contracts for import planning and import
execution so that use cases emit structured outcomes while outer adapters own presentation and
provider translation.

## Contract Overview

### Import planning

- Input boundary: `IImportPlanningUseCase`
- Request model: `ImportPlanningRequest`
- Output boundary: `IImportPlanningOutputBoundary`
- Response model: `ImportPlanningResult`
- Presenter owner: Web layer

Responsibilities:
- Validate the planning request at use-case level.
- Query planner gateway abstractions for the current planner state.
- Decide plan, bucket, and task actions.
- Emit structured validation findings, warnings, and preview fingerprints.
- Do not emit user-facing prose.

### Import execution

- Input boundary: `IImportExecutionUseCase`
- Request model: `ImportExecutionRequest`
- Output boundary: `IImportExecutionOutputBoundary`
- Response model: `ImportExecutionResult`
- Presenter owner: Web layer

Responsibilities:
- Verify that execution is authorised by the approved preview/parity data.
- Execute approved plan, bucket, and task actions through the planner gateway abstraction.
- Emit structured created/reused/skipped/failure/manual-action data.
- Do not emit user-facing prose.

## Proposed Interface Shape

```csharp
public interface IImportPlanningUseCase
{
    Task HandleAsync(
        ImportPlanningRequest request,
        IImportPlanningOutputBoundary outputBoundary,
        CancellationToken cancellationToken);
}

public interface IImportPlanningOutputBoundary
{
    Task PresentAsync(ImportPlanningResult response, CancellationToken cancellationToken);
}

public interface IImportExecutionUseCase
{
    Task HandleAsync(
        ImportExecutionRequest request,
        IImportExecutionOutputBoundary outputBoundary,
        CancellationToken cancellationToken);
}

public interface IImportExecutionOutputBoundary
{
    Task PresentAsync(ImportExecutionResult response, CancellationToken cancellationToken);
}
```

Notes:
- Returning through an explicit output boundary keeps presenter ownership unambiguous.
- If repository conventions prefer returning response objects directly as well, the output boundary still remains the primary seam for presentation shaping.

## Request and Response Expectations

### ImportPlanningRequest

Required inputs:
- Selected container identifier and type.
- Target plan identifier when reusing a plan, otherwise a plan title.
- Parsed CSV task rows.

Response expectations:
- Planned entity actions for plan, buckets, and tasks.
- Validation findings and warnings in neutral categories.
- Preview freshness/parity data required for later execution.

### ImportExecutionRequest

Required inputs:
- Approved preview or equivalent parity-approved execution payload.
- Explicit confirmation state from the Web workflow.
- Task rows or action payload required to execute the approved plan.

Response expectations:
- Structured created items.
- Structured reused or skipped items.
- Structured failures with neutral category/target metadata.
- Manual follow-up actions and aggregate summary counters.

## Mapping Responsibilities

### Infrastructure

Owns:
- CSV parsing implementation.
- Graph and in-memory gateway translation.
- Provider-specific exception capture and mapping to neutral failure categories.

Must not own:
- Business decisions about what should be created or reused once neutral data is available.
- User-facing wording.

### Application

Owns:
- Planning and execution rules.
- Response structure.
- Neutral validation and failure categories.

Must not own:
- UI text.
- SDK, Graph, MudBlazor, or CSV-library specific semantics.

### Web

Owns:
- Presenter implementations for planning and execution.
- Workflow coordination state.
- UK-English wording and visual grouping.
- Authentication and interaction flow concerns tied to the page.

Must not own:
- Planner decision rules that belong inside use cases.

## Compatibility Notes

- Existing `IImportPlannerOrchestrator` callers will need to migrate to separate planning and execution collaborators.
- Existing tests that assert user-facing strings directly from Application must be rewritten to assert neutral response data in Application and wording in Web presenters.
- Runtime-mode parity remains required whenever planner-facing behaviour changes as part of the refactor.
