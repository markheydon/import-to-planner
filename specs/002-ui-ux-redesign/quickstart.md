# Quickstart: Verifying the Redesigned UI

**Feature**: 002-ui-ux-redesign  
**Date**: 2026-05-11

## Prerequisites

- .NET SDK installed (see `global.json` for required version)
- Repository checked out on branch `002-ui-ux-redesign`
- Either: the in-memory gateway mode (no Azure credentials required), or a valid `appsettings.Development.json` / user secrets for Graph mode

## Running the App (In-Memory Gateway)

The in-memory gateway requires no authentication and is the fastest way to verify the UI locally.

```bash
# From the repository root
dotnet run --project src/ImportToPlanner.Web
```

Then open `https://localhost:PORT` in your browser (port shown in terminal output).

> With `PlannerGateway:UseGraph` set to `false` (the default for development), no sign-in is required. The in-memory gateway serves pre-seeded container and plan data.

## Running via Aspire AppHost

```bash
# From the repository root
aspire start --isolated
```

The Aspire dashboard URL will be printed to the terminal. Click through to the Web frontend resource.

## Verifying the Stepped Layout

Walk through the five-step flow to confirm the redesign:

### Step 1 — Select Container
1. The page loads showing Step 1 in an **active** (elevated card) state.
2. Steps 2–5 are visibly **locked** (muted styling, disabled controls).
3. Select a container from the dropdown. The step transitions to a **complete** state showing a checkmark and compact summary (e.g. "My Group (Group)").

### Step 2 — Select Plan
1. Step 2 is now **active**.
2. Select a plan. The step transitions to **complete** with a compact summary.

### Step 3 — Upload CSV & Options
1. Step 3 is now **active**.
2. Click "Browse CSV file" and select a `.csv` file from `tests/ImportToPlanner.Tests/TestData/` (e.g. `valid-tasks.csv`).
3. The file name appears and the step transitions to **complete** summary.

### Step 4 — Validate & Preview
1. Step 4 is now **active**.
2. Click "Validate And Preview".
3. On success: the dry-run preview (bucket actions + task actions) appears within Step 4.
4. On validation errors: the error grid appears within Step 4, step remains active.
5. When Step 4 has a valid preview, Step 5 unlocks.

### Step 5 — Confirm & Import
1. Step 5 is now **active**.
2. Click "Confirm And Execute".
3. The execution report appears within Step 5 using **three tabs**:
   - **Summary** — created and reused/skipped items
   - **Manual Actions** — items requiring follow-up (with a count badge when non-zero)
   - **Errors** — any import errors (with a count badge when non-zero)

## Verifying Stale Preview Warning

1. Complete Step 4 (generate a valid preview).
2. Go back to Step 2 and change the selected plan.
3. Step 5 should become **locked** again and a warning should appear within Step 4 indicating the preview is stale.
4. Re-validate to clear the warning and re-unlock Step 5.

## Running Tests

```bash
# All tests
dotnet test

# Web UI tests only
dotnet test tests/ImportToPlanner.Web.Tests/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

See `tests/README.md` for coverage reporting guidance.

## Test Data

Sample CSV files for testing are in:

```
tests/ImportToPlanner.Tests/TestData/
```

Use these to exercise the full validation → preview → execute flow without needing live Planner data.
