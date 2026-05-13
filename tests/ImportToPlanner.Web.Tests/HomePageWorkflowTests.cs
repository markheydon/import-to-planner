using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageWorkflowTests
{
    [Fact]
    public async Task HomeExecutionReport_WithCreatedAndManualActions_RendersTabbedExecutionReport()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        var result = new ImportExecutionResult
        {
            PlanId = "plan-1",
            Created = ["Task: Alpha Task"],
            ReusedOrSkipped = [],
            Errors = [],
            ManualActions =
            [
                new ManualAction("EnsureGoalExists", "Sprint 1", null, "Verify this goal exists in Planner."),
                new ManualAction("LinkTaskToGoal", "Sprint 1", "Alpha Task", "Link this task to the goal manually."),
            ],
            OutcomeSummary = new ImportExecutionOutcomeSummary(1, 0, 0, 2, false, false),
        };

        // Act
        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, result));

        // Assert
        Assert.Contains("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summary", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manual Actions", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Errors", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Alpha Task", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mud-badge", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InitialRender_LeavesContainerAndPlanSelectorsUnselected()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert
        Assert.Equal(4, cut.FindAll(".step-card--locked").Count);
        Assert.Single(cut.FindAll(".step-card--active"));
        Assert.Contains("Step 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_ExplicitContainerSelection_UnlocksPlanStep()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0];
        var firstContainer = ctx.Gateway.Containers[0];

        // Act
        await cut.InvokeAsync(() => containerAutocomplete.Instance.ValueChanged.InvokeAsync(firstContainer));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindAll(".step-card--locked").Count);
            Assert.Contains($"Container: {firstContainer.DisplayName}", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("✓", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HomePage_CompletingStepOneAndTwo_ShowsExpectedVisualStates()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0];
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0];

        // Act
        await cut.InvokeAsync(() => containerAutocomplete.Instance.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));
        await cut.InvokeAsync(() => planAutocomplete.Instance.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll(".step-card--complete").Count);
            Assert.Single(cut.FindAll(".step-card--active"));
            Assert.Equal(2, cut.FindAll(".step-card--locked").Count);
            Assert.Contains("mud-elevation-6", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("mud-elevation-2", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("mud-elevation-0", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenAllStepsAreComplete_DoesNotRenderAnActiveStep()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();

        var request = new ImportRequest(
            ctx.Gateway.Containers[0].Id,
            ctx.Gateway.Containers[0].Type,
            ctx.Gateway.Plans[0].Id,
            ctx.Gateway.Plans[0].Title,
            [new CsvTaskRow(2, "Task A", null, null, null, null)]);

        var preview = new ImportPlanPreview
        {
            ContainerId = request.ContainerId,
            PlanId = request.PlanId,
            PlanName = request.PlanName,
            PlanAction = PlannedEntityAction.Reuse,
            HasValidationErrors = false,
            RequestFingerprint = "test-request",
            PlannerStateFingerprint = "test-state",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            BucketActions = new Dictionary<string, PlannedEntityAction>(StringComparer.OrdinalIgnoreCase),
            TaskActions = [],
        };

        var executionResult = new ImportExecutionResult
        {
            PlanId = request.PlanId,
            Created = ["Task: Task A"],
            ReusedOrSkipped = [],
            Errors = [],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(1, 0, 0, 0, false, false),
        };

        // Act
        SetPrivateField(cut.Instance, "selectedContainer", ctx.Gateway.Containers[0]);
        SetPrivateField(cut.Instance, "selectedPlan", ctx.Gateway.Plans[0]);
        SetPrivateField(cut.Instance, "csvContent", "Bucket,Task\nEngineering,Task A");
        SetPrivateField(cut.Instance, "selectedFileName", "tasks.csv");
        SetPrivateField(cut.Instance, "currentRequest", request);
        SetPrivateField(cut.Instance, "preview", preview);
        SetPrivateField(cut.Instance, "executionResult", executionResult);
        await cut.InvokeAsync(() => cut.Render());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".step-card--active"));
            Assert.Equal(5, cut.FindAll(".step-card--complete").Count);
        });
    }

    [Fact]
    public async Task HomePage_SearchContainers_FiltersCaseInsensitiveLargeList()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.Containers = Enumerable.Range(1, 50)
            .Select(index => new PlannerContainer($"container-{index}", $"Alpha Team {index}", ContainerType.Group))
            .ToList();

        var cut = ctx.Render<Home>();
        var autocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        if (autocomplete.SearchFunc is not { } searchFunc)
        {
            throw new InvalidOperationException("Container search function was not assigned.");
        }

        // Act
        var searchTask = searchFunc("team 19", CancellationToken.None);
        if (searchTask is null)
        {
            throw new InvalidOperationException("Container search returned no task.");
        }

        var results = await searchTask ?? [];

        // Assert
        var filtered = results.ToList();
        Assert.Single(filtered);
        Assert.NotNull(filtered[0]);
        Assert.Equal("container-19", filtered[0]!.Id);
    }

    [Fact]
    public async Task HomePage_PlanSelectionViaValueChanged_UnlocksStepThree()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;

        // Act
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Step 3 · Upload CSV and Options", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, cut.FindAll(".step-card--locked").Count);
        });
    }

    [Fact]
    public async Task HomePage_ChangingPlanAfterPreview_ShowsStaleWarningAndDisablesExecution()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.Plans =
        [
            new PlannerPlan("plan-1", "Plan One", "container-1", ContainerType.Group),
            new PlannerPlan("plan-2", "Plan Two", "container-1", ContainerType.Group),
        ];

        var cut = ctx.Render<Home>();
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        SetPrivateField(cut.Instance, "csvContent", "Bucket,Task\nEngineering,Task A");
        await InvokePrivateMethodAsync(cut, "BuildPreviewAsync");

        // Act
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[1]));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Preview is stale because your selected plan changed", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var executeButton = cut.FindAll("button")
                .First(button => button.TextContent.Contains("Confirm and execute", StringComparison.OrdinalIgnoreCase));
            Assert.True(executeButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task HomePage_RevalidatingAfterStalePreview_ClearsWarningAndEnablesExecution()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.Plans =
        [
            new PlannerPlan("plan-1", "Plan One", "container-1", ContainerType.Group),
            new PlannerPlan("plan-2", "Plan Two", "container-1", ContainerType.Group),
        ];

        var cut = ctx.Render<Home>();
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;
        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        SetPrivateField(cut.Instance, "csvContent", "Bucket,Task\nEngineering,Task A");
        await InvokePrivateMethodAsync(cut, "BuildPreviewAsync");
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[1]));

        // Act
        await InvokePrivateMethodAsync(cut, "BuildPreviewAsync");

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.True(GetPrivateBooleanProperty(cut.Instance, "canExecute"));
        });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static async Task InvokePrivateMethodAsync(IRenderedComponent<Home> cut, string methodName)
    {
        var target = cut.Instance;
        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        await cut.InvokeAsync(async () =>
        {
            var task = method!.Invoke(target, null) as Task;
            Assert.NotNull(task);
            await task!;
        });
    }

    private static bool GetPrivateBooleanProperty(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(property);
        var value = property!.GetValue(target);
        Assert.IsType<bool>(value);
        return (bool)value;
    }
}
