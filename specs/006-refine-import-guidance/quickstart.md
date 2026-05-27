# Quickstart: Refine Import Guidance Verification

## Purpose

Use this guide after implementation to verify that the import page now gives
clearer workflow guidance, clearer step progression cues, compact CSV schema
expectation-setting, and the corrected light-theme border token without changing
workflow behaviour.

## Prerequisites

- .NET 10 SDK installed.
- Repository dependencies restored.
- Existing developer configuration for the Web test projects available.

## Verification Checkpoints

| Checkpoint | Command or action | Evidence to capture |
| --- | --- | --- |
| Focused home page smoke coverage | `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --filter FullyQualifiedName~HomePageSmokeTests` | Updated five-step structure, new action labels, and refreshed top-of-page guidance render as expected |
| Focused workflow behaviour coverage | `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --filter FullyQualifiedName~HomePageWorkflowTests` | Existing plan/preview/execution semantics still hold while step presentation cues update |
| Architecture guardrail coverage | `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj --filter FullyQualifiedName~ArchitectureComplianceTests` | Home page guidance logic still avoids brittle status-message string scanning |
| Full targeted test baseline | `dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj` and `dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj` | No regression in nearby web or architecture slices |
| Manual desktop review | Run the app and open the home page in a desktop browser | Current/completed/upcoming states are visually distinct; headings and buttons use the updated wording |
| Manual mobile-width review | Narrow the browser or use device emulation | Card stack remains legible and state cues still read clearly |
| Theme token review | Inspect outlined surfaces in light mode | Borders and dividers use the updated neutral tone and match the intended Fluent-like look |

## Recommended Verification Order

### 1. Run focused automated tests first

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --filter FullyQualifiedName~HomePageSmokeTests
```

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj --filter FullyQualifiedName~HomePageWorkflowTests
```

```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj --filter FullyQualifiedName~ArchitectureComplianceTests
```

Expected evidence:
- The home page still renders five workflow cards.
- Card headings no longer depend on `Step X ·` text in the title itself.
- Updated button labels and top-of-page CSV guidance appear in rendered markup.
- Existing workflow state transitions still unlock later steps correctly.

### 2. Run the broader nearby regression baseline

```bash
dotnet test tests/ImportToPlanner.Web.Tests/ImportToPlanner.Web.Tests.csproj
```

```bash
dotnet test tests/ImportToPlanner.Tests/ImportToPlanner.Tests.csproj
```

Expected evidence:
- No regressions in nearby presenter, startup, or workflow tests.
- Architecture guardrails still pass after the copy and markup refactor.

### 3. Perform a manual wording and layout review

Suggested checks:

1. Open the home page in light mode and confirm the introduction now mentions:
   - the required field `Task Name`
   - the accepted fields `Task Name`, `Description`, `Priority`, `Bucket`, and `Goal`
   - a concise manual follow-up note using goals as the example
2. Confirm the five visible step titles read:
   - `Select Planner location`
   - `Select plan`
   - `Upload CSV`
   - `Preview import`
   - `Confirm and import`
3. Confirm the primary actions use sentence case and plain language.
4. Confirm the current step is visually emphasised, completed steps show a
   completion cue, and upcoming steps are subdued.

### 4. Perform a responsive and theme check

1. Reduce the viewport to a mobile width.
2. Confirm the stacked cards still communicate sequence and state clearly.
3. Confirm outlined surfaces use the updated light-theme border colour.
4. Confirm no extra navigation bar or stepper has been introduced.

## Review Checklist

- The page still presents a five-step workflow in the same functional order.
- Step headings are action-oriented and remove redundant `Step X` text from the
  heading itself.
- The introduction gives compact CSV guidance without becoming long-form
  documentation.
- Manual follow-up wording names one concrete example only.
- Preview and execution actions remain semantically unchanged.
- Light-theme borders and dividers use the corrected neutral token.
- Desktop and mobile layouts remain readable.

## Verification Evidence

- Automated verification: pending implementation.
- Manual desktop and mobile verification: pending implementation.
