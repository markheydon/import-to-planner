# Quickstart: Verifying the Redesigned UI (MudBlazor)

**Feature**: 002-ui-ux-redesign  
**Date**: 2026-05-12

## Prerequisites

- .NET 10 SDK installed (see `global.json`)
- Repository checked out on branch `002-ui-ux-redesign`
- Either: in-memory gateway mode (no Azure credentials required), or a valid
  `appsettings.Development.json` / user secrets for Graph mode

## Running the App (In-Memory Gateway)

The in-memory gateway requires no authentication and is the fastest way to verify the UI.

```bash
# From the repository root
PlannerGateway__UseGraph=false dotnet run --project src/ImportToPlanner.Web
```

Then open `http://localhost:PORT` in your browser (port shown in terminal output).

> With `PlannerGateway:UseGraph` set to `false`, no sign-in is required. The in-memory
> gateway serves pre-seeded container and plan data.

## Running via Aspire AppHost

```bash
# From the repository root
aspire start --isolated
```

The Aspire dashboard URL will be printed to the terminal. Navigate to the Web frontend
resource to open the app.

## Verifying the Stepped Layout

Walk through the five-step flow to confirm the redesign:

### Step 1 — Select Container
1. The page loads showing a `MudProgressLinear` bar briefly while containers load.
2. Step 1 displays an active `MudPaper` card (elevated, accent left-border) with a
   `MudAvatar Color="Color.Primary"` showing "1".
3. Steps 2–5 are visibly locked (low-elevation `MudPaper`, muted text, `Color.Default`
   avatar, disabled controls).
4. Type into the `MudAutocomplete` to filter containers, or click to open the dropdown.
5. Select a container. Step 1 transitions to complete: avatar shows a success checkmark,
   compact summary ("Container: My Group (Group)") appears below.

### Step 2 — Select Plan
1. Step 2 is now active.
2. Select a plan from the `MudAutocomplete`. Step 2 transitions to complete with summary.
3. If no plans exist for the container, a `MudAlert Severity="Warning"` appears within
   Step 2 prompting the user to create a plan.

### Step 3 — Upload CSV & Options
1. Step 3 is now active.
2. Click the "Browse CSV file" `MudButton` (inside `MudFileUpload`) and select a `.csv`
   file from `tests/ImportToPlanner.Tests/TestData/` (e.g., `valid-tasks.csv`).
3. The selected file name appears below the button. Step 3 transitions to complete.
4. Toggle the "Ignore extra columns" `MudSwitch` if needed.

### Step 4 — Validate & Preview
1. Step 4 is now active.
2. Click "Validate And Preview" (`MudButton Variant.Filled Color.Primary`).
3. On success: bucket actions appear in a `MudSimpleTable` and task actions in a
   `MudDataGrid<ImportTaskPlanItem>` within Step 4.
4. On validation errors: a `MudDataGrid<ImportValidationError>` appears within Step 4;
   step remains active (error state).
5. When Step 4 has a valid preview, Step 5 unlocks.

### Step 5 — Confirm & Import
1. Step 5 is now active.
2. Click "Confirm And Execute" (`MudButton Variant.Filled Color.Primary`).
3. The execution report appears within Step 5 using three `MudTabPanel` tabs:
   - **Summary** — `MudSimpleTable` rows for created and reused/skipped items.
   - **Manual Actions** — `MudDataGrid<ManualAction>` with a `MudBadge` count when > 0.
   - **Errors** — `MudDataGrid<string>` with a `MudBadge` count when > 0.
4. A `MudAlert Severity="Success"` with a link to the plan in Planner appears at the
   top of the results.

## Verifying Stale Preview Warning

1. Complete Step 4 (generate a valid preview).
2. Go back to Step 2 and change the selected plan using the `MudAutocomplete`.
3. Step 5 becomes locked again and a `MudAlert Severity="Warning"` appears within
   Step 4 indicating the preview is stale.
4. Re-validate in Step 4 to clear the warning and re-unlock Step 5.

## Verifying First-Option Selection (FR-014)

1. Load the app with the in-memory gateway.
2. In Step 1, click the `MudAutocomplete` without typing anything, then explicitly click
   the first option in the dropdown list.
3. Step 2 must unlock — confirming that explicit selection of the first option is treated
   as a valid progression event (not silently preselected on render).

## Running Tests

```bash
# All tests
dotnet test

# Web UI tests only
dotnet test tests/ImportToPlanner.Web.Tests/

# Unit tests only
dotnet test tests/ImportToPlanner.Tests/
```

## Key MudBlazor Provider Checklist

If any of the following UI behaviours are silently broken, check `MainLayout.razor` for
the relevant provider:

| Broken behaviour                             | Required provider      |
|----------------------------------------------|------------------------|
| `MudAutocomplete` / `MudSelect` dropdown empty or missing | `MudPopoverProvider`  |
| Dialogs do nothing when opened              | `MudDialogProvider`   |
| Snackbar notifications never appear        | `MudSnackbarProvider` |
| Theme colours not applying                 | `MudThemeProvider`    |
