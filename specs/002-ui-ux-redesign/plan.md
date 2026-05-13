# Implementation Plan: UI/UX Redesign — Stepped Import Workflow (MudBlazor)

**Branch**: `002-ui-ux-redesign` | **Date**: 2026-05-12 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/002-ui-ux-redesign/spec.md`

## Summary

The import page and shared layout are redesigned using MudBlazor as the sole UI component
library, replacing the existing Fluent UI Blazor implementation. The workflow is presented
as five vertically-stacked `MudPaper` step cards that progressively unlock as the user
completes each step: Select Container → Select Plan → Upload CSV & Options →
Validate & Preview → Confirm & Import. Each step has three visual states (locked /
active / complete) driven entirely by existing `Home.razor` state fields. The execution
report is refactored into a tabbed `MudTabs` layout within Step 5. No back-end, domain,
or application-layer changes are required.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (SDK 10.0.100 per `global.json`)  
**Primary Dependencies**: MudBlazor (new), Blazor Interactive Server (existing),
Microsoft Graph / Microsoft Identity Web (existing, unchanged)  
**Storage**: N/A (stateless UI feature; no persistence changes)  
**Testing**: xUnit (existing unit tests), bUnit via `ImportToPlanner.Web.Tests`
(existing Blazor component tests)  
**Target Platform**: Blazor Server web application with desktop-first presentation and
mobile/touch usability preserved for the primary workflow  
**Performance Goals**: UI-only responsiveness checks for feature-controlled interactions
(step unlock, preview-state rendering) with no perceptible additional render delay vs
current flat layout; third-party component internals and back-end/API latency are out of
scope for this UI redesign  
**Constraints**: Single import page + shared layout only; no new pages, routes, or
back-end changes; desktop-first; no multi-tenant scope  
**Scale/Scope**: Two Razor component files (`Home.razor`, `HomeExecutionReport.razor`),
one layout file (`MainLayout.razor`), two support files (`App.razor`, `_Imports.razor`),
two project config files, two CSS files

## Constitution Check

*GATE: Pre-phase assessment — all gates pass.*

- **Code Quality Gate**: All changes are confined to the `ImportToPlanner.Web`
  presentation project. No new cross-layer coupling is introduced. Domain, Application,
  and Infrastructure layers are untouched. Component logic remains lightweight in Razor;
  complex state derivation stays in code-behind expressions.

- **Testing Gate**: Existing `HomePageSmokeTests` and `HomePageWorkflowTests` must
  continue to pass. Fluent UI component references in those tests will need to be updated
  to reference MudBlazor component markup. The step progression logic (FR-013, FR-014)
  requires regression coverage: the C# Expert agent must add or update tests verifying
  that `MudAutocomplete` selectors remain null on load and that explicit first-option
  selection unlocks the next step.

- **UX Consistency Gate**: All five workflow steps, stale-preview warning, and execution
  report are preserved. UK English copy is maintained. Accessibility: MudBlazor
  components include ARIA attributes by default; `MudAutocomplete`, `MudFileUpload`, and
  `MudButton` all expose accessible labels. Responsive: the `MudMainContent` +
  `MudStack` layout remains usable across desktop and mobile/touch viewports.

- **Performance Gate**: MudBlazor renders server-side; no additional client-side bundle
  weight beyond the MudBlazor CSS (replacing Fluent CSS). Step card visibility is
  controlled by Blazor rendering via conditional Razor expressions — no JavaScript
  animation overhead. Performance expectation: identical to or better than the current
  layout.

- **Operations Gate**: No changes to logging, telemetry, or error handling. The
  `blazor-error-ui` overlay is retained. Dry-run safety is unchanged (execution is still
  gated behind a valid preview).

- **Runtime Mode Compatibility Gate**: Both `PlannerGateway:UseGraph = true` and
  `= false` modes are UI-only changes. Both modes are covered by the existing
  test suite. The authentication redirect path in `Home.razor` is preserved unchanged.

- **Graph Contract Volatility Gate**: No Graph API changes. Not applicable.

- **Scope Boundary Gate**: Single-tenant scope is preserved. No multi-tenant behaviour
  is introduced.

- **CI and AppHost Gate**: Only `ImportToPlanner.Web` project files change. Solution and
  AppHost build viability are preserved; no project references or service registrations
  are affected beyond the package swap.

- **Agent Delegation Gate**: All Blazor component implementation, refactoring, and test
  work is delegated to the C# Expert agent per `AGENTS.md`. The `mudblazor` skill is
  the primary reference for UI implementation decisions. This plan is the specification
  input; the agent handles all code-level execution.

## Project Structure

### Documentation (this feature)

```text
specs/002-ui-ux-redesign/
├── plan.md           ← This file
├── research.md       ← MudBlazor component decisions
├── data-model.md     ← UI state model and file change map
├── quickstart.md     ← Manual verification guide
└── tasks.md          ← Generated by /speckit.tasks
```

### Source Code (repository root)

```text
src/ImportToPlanner.Web/
├── Components/
│   ├── App.razor                           ← Add MudBlazor CSS link
│   ├── _Imports.razor                      ← Replace Fluent UI @using with MudBlazor
│   ├── Layout/
│   │   ├── MainLayout.razor               ← Full rewrite: MudLayout shell + providers
│   │   └── MainLayout.razor.css           ← Remove (shell CSS replaced by MudBlazor)
│   └── Pages/
│       ├── Home.razor                     ← Full rewrite: five MudPaper step cards
│       └── HomeExecutionReport.razor      ← Refactor: MudTabs + badged headers
├── wwwroot/
│   └── app.css                            ← Remove import-grid/shell CSS; keep error overlay
├── Program.cs                             ← AddMudServices(); remove AddFluentUIComponents()
└── ImportToPlanner.Web.csproj             ← Remove Fluent refs; add MudBlazor ref

Directory.Packages.props                   ← Remove Fluent versions; add MudBlazor version

tests/ImportToPlanner.Web.Tests/
├── HomePageSmokeTests.cs                  ← Update Fluent-specific markup assertions to MudBlazor
└── HomePageWorkflowTests.cs              ← Update + add step-progression regression tests
```

## Complexity Tracking

No constitution gate violations. No complexity overrides required.

---

## Phase 0: Research

Complete — see [research.md](research.md).

All 12 decisions are resolved. No NEEDS CLARIFICATION items remain.

---

## Phase 1: Design

Complete — see [data-model.md](data-model.md) and [quickstart.md](quickstart.md).

Key design outputs:

### Package changes

| Action  | Package                                           | Location                         |
|---------|---------------------------------------------------|----------------------------------|
| Remove  | `Microsoft.FluentUI.AspNetCore.Components` 4.14.1 | `Directory.Packages.props`, `.csproj` |
| Remove  | `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.1 | `Directory.Packages.props`, `.csproj` |
| Add     | `MudBlazor` (latest stable / .NET 10 compatible)  | `Directory.Packages.props`, `.csproj` |

### MainLayout.razor structure

```razor
@inherits LayoutComponentBase
@inject NavigationManager NavigationManager

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudText Typo="Typo.h6" Class="ml-3">CSV to Planner Import</MudText>
        <MudSpacer />
        <AuthorizeView>
            <Authorized Context="authState">
                <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                    <MudText Typo="Typo.body2">@authState.User.Identity?.Name</MudText>
                    <MudButton Variant="Variant.Text" Color="Color.Inherit"
                               OnClick="OnSignOutClicked">Sign out</MudButton>
                </MudStack>
            </Authorized>
            <NotAuthorized>
                <MudButton Variant="Variant.Filled" Color="Color.Primary"
                           OnClick="OnSignInClicked">Sign in</MudButton>
            </NotAuthorized>
        </AuthorizeView>
    </MudAppBar>
    <MudMainContent Class="pa-4">
        @Body
    </MudMainContent>
</MudLayout>
```

### Home.razor step structure (per step card pattern)

Each of the five steps follows this pattern, parameterised by step state:

```razor
<MudStack Spacing="4">
    @* Repeat for each step *@
    <MudPaper Elevation="@StepElevation(stepIndex)" Class="@StepClass(stepIndex) pa-4">
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2" Class="mb-3">
            <MudAvatar Color="@StepAvatarColour(stepIndex)" Size="Size.Medium">
                @if (IsComplete(stepIndex))
                {
                    <MudIcon Icon="@Icons.Material.Filled.CheckCircle" />
                }
                else
                {
                    @(stepIndex + 1)
                }
            </MudAvatar>
            <MudText Typo="Typo.h6" Class="@(IsLocked(stepIndex) ? "mud-text-disabled" : "")">
                @StepTitle(stepIndex)
            </MudText>
        </MudStack>

        @if (IsComplete(stepIndex))
        {
            <MudText Typo="Typo.body2" Class="mud-text-secondary">@StepSummary(stepIndex)</MudText>
        }
        else if (IsActive(stepIndex))
        {
            @* Step content (controls, alerts, grids) *@
        }
    </MudPaper>
</MudStack>
```

State helper methods (`StepElevation`, `StepAvatarColour`, `IsLocked`, `IsActive`,
`IsComplete`, `StepSummary`) are private methods in the `@code` block, derived from
the existing state fields. No new fields are introduced.

### MudAutocomplete<T> selector pattern (Steps 1 and 2)

```razor
<MudAutocomplete T="PlannerContainer"
                 @bind-Value="selectedContainer"
                 SearchFunc="SearchContainers"
                 ToStringFunc="@(c => c is null ? string.Empty : $"{c.DisplayName} ({c.Type})")"
                 Label="Container"
                 Placeholder="Search or select a container…"
                 Variant="Variant.Outlined"
                 Disabled="@isBusy" />
```

```csharp
private Task<IEnumerable<PlannerContainer>> SearchContainers(
    string value, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(value))
        return Task.FromResult<IEnumerable<PlannerContainer>>(containers);
    return Task.FromResult(containers.Where(c =>
        c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase)));
}
```

`selectedContainer` is bound via `@bind-Value` which internally uses `ValueChanged`.
It is explicitly set to `null` on container list refresh and triggers `StateHasChanged`
to re-evaluate derived step states.

### MudFileUpload pattern (Step 3)

```razor
<MudFileUpload T="IBrowserFile" Accept=".csv,text/csv" MaximumFileCount="1"
               OnFilesChanged="OnFileChangedAsync">
    <ActivatorContent>
        <MudButton Variant="Variant.Outlined" StartIcon="@Icons.Material.Filled.AttachFile">
            Browse CSV file
        </MudButton>
    </ActivatorContent>
</MudFileUpload>
@if (!string.IsNullOrWhiteSpace(selectedFileName) && selectedFileName != "No file selected.")
{
    <MudText Typo="Typo.body2" Class="mt-1">@selectedFileName</MudText>
}
```

### HomeExecutionReport.razor tab structure

```razor
@if (ExecutionResult is not null)
{
    <MudTabs Elevation="0" Rounded="true" ApplyEffectsToContainer="true">
        <MudTabPanel Text="Summary">
            @* MudSimpleTable for Created + ReusedOrSkipped *@
        </MudTabPanel>
        <MudTabPanel>
            <TabContent>
                <MudBadge Content="@ExecutionResult.ManualActions.Count"
                          Visible="@(ExecutionResult.ManualActions.Count > 0)"
                          Color="Color.Warning" Overlap="true">
                    <MudText>Manual Actions</MudText>
                </MudBadge>
            </TabContent>
            <ChildContent>
                @* MudDataGrid<ManualAction> *@
            </ChildContent>
        </MudTabPanel>
        <MudTabPanel>
            <TabContent>
                <MudBadge Content="@ExecutionResult.Errors.Count"
                          Visible="@(ExecutionResult.Errors.Count > 0)"
                          Color="Color.Error" Overlap="true">
                    <MudText>Errors</MudText>
                </MudBadge>
            </TabContent>
            <ChildContent>
                @* MudDataGrid<string> *@
            </ChildContent>
        </MudTabPanel>
    </MudTabs>
}
```

---

## Implementation Notes for C# Expert Agent

1. **MudBlazor skill is the authoritative reference** for all component and pattern
   decisions. Read `.github/skills/mudblazor/SKILL.md` and the relevant reference files
   before implementing each component. Follow the decision order: component parameters
   first, utility classes second, scoped CSS last resort.

2. **Providers must all be present** in `MainLayout.razor` or popup/dialog/snackbar
   features will silently fail. See [KNOWN-PITFALLS.md](.github/skills/mudblazor/references/KNOWN-PITFALLS.md).

3. **MudAutocomplete SearchFunc signature** must be
   `Func<string, CancellationToken, Task<IEnumerable<T>>>`. Return
   `Task.FromResult(...)` for synchronous in-memory filtering. Do not mark the method
   `async` without an `await`.

4. **File upload** uses `OnFilesChanged` with `InputFileChangeEventArgs` (same as the
   existing Blazor `InputFile` pattern internally). Read the file content with
   `await e.File.OpenReadStream().ReadToEndAsync(...)` or via `StreamReader`.

5. **`@bind-Value` on MudAutocomplete**: clearing `selectedContainer` to `null`
   programmatically does not fire `ValueChanged`. After clearing, call `StateHasChanged()`
   to trigger re-render. Confirm this behaviour during implementation.

6. **MudTabs TabContent fragment**: Use the `<TabContent>` child content fragment for
   custom tab headers with badges; use `<ChildContent>` for tab body content. This is
   the standard MudBlazor pattern for badged tabs.

7. **Test updates**: `HomePageSmokeTests` and `HomePageWorkflowTests` use bUnit. All
   Fluent UI component CSS selectors must be replaced with MudBlazor equivalents. Add
   tests for FR-013 (no preselection on load) and FR-014 (first-option explicit selection
   unlocks next step).

8. **NFR-009 is now MudBlazor-first**: The equivalent of the old Fluent-first NFR
   applies to MudBlazor. Selector and upload behaviour must use MudBlazor component
   capabilities; custom HTML/CSS workarounds are a last resort only when MudBlazor has
   no suitable option.


