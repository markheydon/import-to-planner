using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using MudBlazor;
using MudBlazor.Extensions;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageWorkflowTests
{
    [Fact]
    public async Task HomePage_WhenContainerLoadFailsWithAuthenticationFailure_ShowsUserSafeMessage()
    {
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.GetAvailableContainersException = PlannerGatewayStub.AuthenticationFailure();

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Authentication expired.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomeExecutionReport_WithPresenterViewModel_RendersTabbedExecutionReport()
    {
        await using var ctx = new HomePageTestContext();
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            ["Task: Alpha Task"],
            [],
            [new ManualActionViewModel("Ensure Goal Exists", "Sprint 1", null, "Verify this goal exists in Planner.")],
            [],
            new ImportExecutionOutcomeSummary(1, 0, 0, 1, false, false));

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manual Actions", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Alpha Task", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Created: 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manual: 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomeExecutionReport_WithNoCreatedItems_OmitsEmptyCreatedSection()
    {
        await using var ctx = new HomePageTestContext();
        var report = new ImportExecutionReportViewModel(
            "plan-1",
            [],
            ["Task: Review architecture", "Task: Prepare release notes"],
            [],
            [],
            new ImportExecutionOutcomeSummary(0, 2, 0, 0, false, false));

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, report));

        Assert.Contains("Created: 0", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reused or skipped", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Review architecture", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.FindComponents<MudDataGrid<string>>());
    }

    [Fact]
    public async Task HomePage_PlanSelection_UnlocksUploadStep()
    {
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Upload CSV", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var steps = cut.FindComponents<MudStep>();
            Assert.Equal(2, steps.Count(step => step.Instance.GetState(static x => x.Completed)));
            Assert.Equal(2, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex));
            Assert.Single(steps, step => !step.Instance.GetState(static x => x.Completed) && !step.Instance.GetState(static x => x.Disabled));
        });
    }

    [Fact]
    public async Task HomePage_WhenContainerIsCleared_AfterCsvSelection_AllowsExplicitCsvClearAndResetsSelectionText()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";
        state.ParseErrors.Add(new ImportValidationError(3, "Task Name", "Sample validation issue."));

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        await coordinator.ExecuteAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-step")));
        cut.FindAll(".mud-step")[0].Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var clearedContainerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => clearedContainerAutocomplete.ValueChanged.InvokeAsync(null));

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(state.CsvContent);
            Assert.Equal("import.csv", state.SelectedFileName);
        });

        await cut.InvokeAsync(() => clearedContainerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var restoredPlanAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => restoredPlanAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 3));
        cut.FindAll(".mud-step")[2].Click();

        cut.WaitForAssertion(() =>
        {
            var clearButtons = cut.FindAll("button").Where(button => button.TextContent.Contains("Clear CSV", StringComparison.OrdinalIgnoreCase)).ToArray();
            Assert.Single(clearButtons);
            Assert.False(clearButtons[0].HasAttribute("disabled"));
        });

        var csvClearButton = cut.FindAll("button").Single(button => button.TextContent.Contains("Clear CSV", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => csvClearButton.Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, state.CsvContent);
            Assert.Equal("No file selected", state.SelectedFileName);
            Assert.Empty(state.ParseErrors);
            Assert.Null(state.PlanningViewModel);
            Assert.Null(state.CurrentPlanningRequest);
            Assert.Null(state.ExecutionReport);
            Assert.Contains("Selected file: No file selected", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Content explorer _ Microsoft Purview.csv", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Coordinator_BuildPreviewWithoutSelectedLocation_UsesLocationValidationMessage()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = new WorkflowCoordinationState
        {
            SelectedPlan = ctx.Gateway.Plans[0],
            CsvContent = "Task Name\nTask A",
        };

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);

        var validationError = Assert.Single(state.ParseErrors);
        Assert.Equal("Location", validationError.Field);
        Assert.Equal("A location must be selected.", validationError.Message);
    }

    [Fact]
    public async Task Coordinator_WhenSelectedPlanDisappearsOnRefresh_InvalidatesPreviewAndExecutionState()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = new WorkflowCoordinationState
        {
            SelectedContainer = ctx.Gateway.Containers[0],
            SelectedPlan = ctx.Gateway.Plans[0],
        };

        state.CurrentPlanningRequest = new ImportPlanningRequest(
            state.SelectedContainer.Id,
            state.SelectedContainer.Type,
            state.SelectedPlan.Id,
            state.SelectedPlan.Title,
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);
        state.PlanningViewModel = new ImportPlanningViewModel(
            new ImportPlanPreview
            {
                ContainerId = state.CurrentPlanningRequest.ContainerId,
                PlanId = state.CurrentPlanningRequest.PlanId,
                PlanName = state.CurrentPlanningRequest.PlanName,
                PlanAction = PlannedEntityAction.Reuse,
                HasValidationErrors = false,
                ValidationFindings = [],
                RequestFingerprint = "request-fingerprint",
                PlannerStateFingerprint = "state-fingerprint",
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                BucketActions = new Dictionary<string, PlannedEntityAction>(StringComparer.OrdinalIgnoreCase),
                TaskActions = [],
            },
            [],
            []);
        state.ExecutionReport = new ImportExecutionReportViewModel(
            state.SelectedPlan.Id,
            ["Task: Task A"],
            [],
            [],
            [],
            new ImportExecutionOutcomeSummary(1, 0, 0, 0, false, false));

        ctx.Gateway.Plans = [];

        await coordinator.LoadPlansAsync(state, CancellationToken.None);

        Assert.Null(state.SelectedPlan);
        Assert.Null(state.CurrentPlanningRequest);
        Assert.Null(state.PlanningViewModel);
        Assert.Null(state.ExecutionReport);
        Assert.True(state.IsPreviewStale);
    }

    [Fact]
    public async Task HomePage_InHostedMode_WithUnsupportedAccount_ShowsHostedAccountGuidance()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations");
        ctx.TenantAccessor.GetRequiredContextException =
            new InvalidOperationException("Unsupported account type. Sign in with a supported work or school account.");

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            const string unsupportedAccountGuidance = "Unsupported account type. Sign in with a supported work or school account.";
            var occurrenceCount = cut.Markup.Split(unsupportedAccountGuidance, StringSplitOptions.None).Length - 1;
            Assert.Equal(1, occurrenceCount);
        });
    }

    [Fact]
    public async Task HomePage_InHostedMode_WhenAuthErrorQueryExists_DoesNotReTriggerSignInChallenge()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations", isAuthenticated: false);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/?authError=Unsupported%20account%20type.%20Sign%20in%20with%20a%20supported%20work%20or%20school%20account.", forceLoad: false);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unsupported account type", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MicrosoftIdentity/Account/SignIn", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_InHostedMode_WhenAuthErrorQueryIncludesReference_ShowsReferenceId()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations", isAuthenticated: false);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/?authError=Unsupported%20account%20type.%20Sign%20in%20with%20a%20supported%20work%20or%20school%20account.&authRef=trace-123", forceLoad: false);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unsupported account type", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Reference ID: trace-123", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_InHostedMode_WhenTokenAcquisitionRequiresInteraction_TriggersOneTimeReauthentication()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations");
        ctx.Gateway.GetAvailableContainersException = CreateChallengeException();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        _ = ctx.Render<Home>();

        Assert.Contains("MicrosoftIdentity/Account/Challenge", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tokenReauth%3D1", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InHostedMode_WhenReauthenticationAlreadyAttempted_ShowsInteractionGuidanceWithoutLoop()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations");
        ctx.Gateway.GetAvailableContainersException = CreateChallengeException();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/?tokenReauth=1", forceLoad: false);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Microsoft Graph access still needs confirmation", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MicrosoftIdentity/Account/Challenge", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_InHostedMode_WhenTokenReauthenticationQueryIsPresentAndLoadSucceeds_ClearsQueryWithoutWarning()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations");
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/?tokenReauth=1", forceLoad: false);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("tokenReauth=1", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Microsoft Graph access still needs confirmation", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Coordinator_WhenTenantChanges_MarksTenantContextMismatch()
    {
        await using var ctx = new HomePageTestContext(tenantId: "organizations");
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = new WorkflowCoordinationState
        {
            SelectedContainer = ctx.Gateway.Containers[0],
        };

        ctx.TenantAccessor.Context = ctx.TenantAccessor.Context with { TenantId = "tenant-a", TenantKey = "tenant-key-a" };
        await coordinator.LoadContainersAsync(state, CancellationToken.None);

        ctx.TenantAccessor.Context = ctx.TenantAccessor.Context with { TenantId = "tenant-b", TenantKey = "tenant-key-b" };
        await coordinator.LoadContainersAsync(state, CancellationToken.None);

        Assert.True(state.IsTenantContextMismatch);
    }

    [Fact]
    public async Task HomePage_SharedAndSpecificAuthorities_PreserveStepWorkflowSemantics()
    {
        await using var specificAuthority = new HomePageTestContext(tenantId: "tenant-specific");
        await using var sharedAuthority = new HomePageTestContext(tenantId: "organizations");

        var specificAuthorityCut = specificAuthority.Render<Home>();
        var sharedAuthorityCut = sharedAuthority.Render<Home>();

        specificAuthorityCut.WaitForAssertion(() => Assert.Contains("Select Planner location", specificAuthorityCut.Markup, StringComparison.OrdinalIgnoreCase));
        sharedAuthorityCut.WaitForAssertion(() => Assert.Contains("Select Planner location", sharedAuthorityCut.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Preview and confirm", specificAuthorityCut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Report", specificAuthorityCut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Report", sharedAuthorityCut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InitialRender_ShowsCurrentCompletedAndUpcomingStates()
    {
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            var stepper = cut.FindComponent<MudStepper>();
            Assert.Equal(0, stepper.Instance.GetState(static x => x.ActiveIndex));
            var steps = cut.FindComponents<MudStep>();
            Assert.DoesNotContain(steps, step => step.Instance.GetState(static x => x.Completed));
            Assert.Equal(4, steps.Count(step => step.Instance.GetState(static x => x.Disabled)));
        });
    }

    [Fact]
    public async Task HomePage_WhenOnPreviewStep_ShowsSummaryRailAndCollapsesSetupPanels()
    {
        await using var ctx = new HomePageTestContext();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Your import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Not generated", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Preview and confirm", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Your import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("import.csv", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenPreviewReadyOnStep4_ShowsConfirmImportAndAdvancesToReport()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.False(confirmButton.HasAttribute("disabled"));
        });

        var confirmImportButton = cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => confirmImportButton.Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex));
            Assert.Contains("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(state.ExecutionReport);
        });
    }

    [Fact]
    public async Task HomePage_WhenSetupChangesAfterPreview_ClearsPreviewAndRequiresRegeneration()
    {
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.Plans =
        [
            new PlannerPlan("plan-1", "Test Plan", "container-1", ContainerType.Group),
            new PlannerPlan("plan-2", "Other Plan", "container-1", ContainerType.Group),
        ];
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.NotNull(state.PlanningViewModel));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Confirm import", cut.Markup, StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[1]));

        cut.WaitForAssertion(() =>
        {
            Assert.Null(state.PlanningViewModel);
            Assert.Null(state.CurrentPlanningRequest);
            Assert.False(state.IsPreviewStale);
            Assert.DoesNotContain("Confirm import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Preview cleared because your selected plan changed", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenSetupChangesOnPreviewStep_ResetsViewToAffectedSetupStep()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(3, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex)));

        cut.FindAll(".mud-step")[0].Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var clearedContainerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => clearedContainerAutocomplete.ValueChanged.InvokeAsync(null));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(0, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex));
            Assert.DoesNotContain("Confirm import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_RendersManualFollowUpGuidanceWithGoalsExample()
    {
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("manual follow-up", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("confirming goals", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenPreviewReady_Step5RemainsLockedUntilImportRuns()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            var steps = cut.FindComponents<MudStep>();
            Assert.True(steps[4].Instance.GetState(static x => x.Disabled));
            Assert.Contains("Preview ready — confirm to import.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenPreviewIsStale_ShowsWarningAndBlocksConfirmImport()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        state.IsPreviewStale = true;
        cut.Render();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                "Preview is stale because Planner state changed. Generate a fresh preview before import.",
                cut.Markup,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Confirm import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenPlanSwappedOnPreviewStep_FocusesPlanSelectionStep()
    {
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.Plans =
        [
            new PlannerPlan("plan-1", "Test Plan", "container-1", ContainerType.Group),
            new PlannerPlan("plan-2", "Other Plan", "container-1", ContainerType.Group),
        ];
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(3, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex)));

        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[1]));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex));
            Assert.Contains("Select plan", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Confirm import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenRefreshRemovesSelectedPlan_MovesViewToReachableStep()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        await coordinator.ExecuteAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 5));
        cut.FindAll(".mud-step")[4].Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(4, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex)));

        ctx.Gateway.Plans = [];
        var refreshPlansButton = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Refresh plans", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => refreshPlansButton.Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, cut.FindComponent<MudStepper>().Instance.GetState(static x => x.ActiveIndex));
            Assert.True(cut.FindComponents<MudStep>()[4].Instance.GetState(static x => x.Disabled));
            Assert.DoesNotContain("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenCoordinatorMarksPreviewStaleWithoutPreview_ShowsStaleStatusInSummaryRail()
    {
        await using var ctx = new HomePageTestContext();
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerContainer>>()));
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<MudAutocomplete<PlannerPlan>>()));
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));
        state.CsvContent = "Task Name\nTask A";
        state.SelectedFileName = "import.csv";

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        ctx.Gateway.Plans = [];
        await coordinator.LoadPlansAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            Assert.True(state.IsPreviewStale);
            Assert.Null(state.PlanningViewModel);
            Assert.Contains("Stale — regenerate preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_SummaryRail_IsHiddenBelowMediumBreakpoint()
    {
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            var summaryColumn = cut.FindAll(".summary-rail-column");
            Assert.Single(summaryColumn);
            var classAttribute = summaryColumn[0].GetAttribute("class") ?? string.Empty;
            Assert.Contains("d-none", classAttribute, StringComparison.Ordinal);
            Assert.Contains("d-md-block", classAttribute, StringComparison.Ordinal);
        });
    }

    private static MicrosoftIdentityWebChallengeUserException CreateChallengeException()
        => new(
            new MsalUiRequiredException("invalid_grant", "Interactive sign-in is required to acquire the downstream Graph token."),
            ["Tasks.ReadWrite"],
            userflow: string.Empty);
}
