using System.Reflection;
using Bunit;
using ImportToPlanner.Application.Models;
using Microsoft.AspNetCore.Components;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageWorkflowTests
{
    private static readonly FieldInfo ExecutionResultField =
        typeof(Home).GetField("executionResult", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly System.Reflection.MethodInfo StateHasChangedMethod =
        typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static async Task InjectExecutionResultAsync(IRenderedComponent<Home> cut, ImportExecutionResult result)
    {
        await cut.InvokeAsync(() =>
        {
            ExecutionResultField.SetValue(cut.Instance, result);
            StateHasChangedMethod.Invoke(cut.Instance, null);
        });
    }

    [Fact]
    public async Task HomePage_WithCreatedAndManualActions_RendersExecutionReportSections()
    {
        // Arrange
        using var ctx = new HomePageTestContext();
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

        var cut = ctx.Render<Home>();

        // Act — inject execution result directly to test report rendering without a real file upload
        await InjectExecutionResultAsync(cut, result);

        // Assert — execution report sections are present
        Assert.Contains("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Alpha Task", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EnsureGoalExists", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LinkTaskToGoal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sprint 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_WithPartialFailure_RendersErrorsSection()
    {
        // Arrange
        using var ctx = new HomePageTestContext();
        var result = new ImportExecutionResult
        {
            PlanId = "plan-1",
            Created = ["Task: Task 1"],
            ReusedOrSkipped = [],
            Errors = ["Task 'Task 2' failed: operation failed."],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(1, 0, 1, 0, true, false),
        };

        var cut = ctx.Render<Home>();

        // Act
        await InjectExecutionResultAsync(cut, result);

        // Assert — errors section renders with the failure message
        Assert.Contains("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Task 2", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_WithReusedOrSkippedItems_RendersReusedOrSkippedSection()
    {
        // Arrange
        using var ctx = new HomePageTestContext();
        var result = new ImportExecutionResult
        {
            PlanId = "plan-1",
            Created = [],
            ReusedOrSkipped = ["Task: Existing Task (already exists)"],
            Errors = [],
            ManualActions = [],
            OutcomeSummary = new ImportExecutionOutcomeSummary(0, 1, 0, 0, false, false),
        };

        var cut = ctx.Render<Home>();

        // Act
        await InjectExecutionResultAsync(cut, result);

        // Assert
        Assert.Contains("Execution Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Existing Task", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reused Or Skipped", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}

