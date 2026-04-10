# Import To Planner

Single-use Blazor utility to import tasks from CSV into Microsoft Planner with explicit validation, dry-run preview, confirmation, and execution reporting.

## Current Implementation Status

This repository now includes:

- Clean architecture solution structure:
  - `src/ImportToPlanner.Web`
  - `src/ImportToPlanner.Application`
  - `src/ImportToPlanner.Domain`
  - `src/ImportToPlanner.Infrastructure.Graph`
  - `tests/ImportToPlanner.Tests`
- CSV parser and validation pipeline for columns:
  - `Task Name` (required)
  - `Description`
  - `Priority`
  - `Bucket`
  - `Goal`
- Priority validation/mapping:
  - Accepts numeric `0-10`
  - Accepts `Urgent`, `Important`, `Medium`, `Low`
- Dry-run planning with idempotency rules:
  - Plan create/reuse
  - Bucket create/reuse
  - Goal label create/reuse
  - Task create/skip
  - Duplicate task names in CSV are skipped after the first occurrence
- Confirmation + execution reporting UI
- Unit tests for parser and orchestrator behavior

## Important Note

The infrastructure currently uses an in-memory `IPlannerGateway` implementation to enable end-to-end UI and orchestration development.

The next implementation step is replacing this with real Microsoft Graph delegated calls.

## Microsoft Graph Constraints Accounted For

- Planner plans are container-scoped (group-backed plan container expected).
- User context is delegated.
- Tasks are idempotent by app logic (title-based check in selected plan).
- Goal is mapped to Planner category/label behavior in the app model.

## Run Locally

```bash
dotnet restore
dotnet build ImportToPlanner.slnx
dotnet test ImportToPlanner.slnx
dotnet run --project src/ImportToPlanner.Web/ImportToPlanner.Web.csproj
```

## CSV Example

```csv
Task Name,Description,Priority,Bucket,Goal
Create user stories,Draft sprint stories,Important,Backlog,Delivery
Review architecture,Validate boundaries,5,Architecture,Quality
Prepare release notes,,Low,,Communication
```

## Next Steps

1. Implement real Graph gateway in `src/ImportToPlanner.Infrastructure.Graph`.
2. Add Entra ID delegated auth in the web app and token acquisition.
3. Replace in-memory groups with real user-accessible group lookup.
4. Add integration tests for Graph error handling and permission failures.
