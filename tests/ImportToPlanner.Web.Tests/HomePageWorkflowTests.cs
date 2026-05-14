using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Presenters;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageWorkflowTests
{
    [Fact]
    public async Task HomePage_WhenContainerLoadFailsWithAuthenticationFailure_ShowsUserSafeMessage()
    {
        await using var ctx = new HomePageTestContext();
        ctx.Gateway.GetAvailableContainersException = StubPlannerGateway.AuthenticationFailure();

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
    }

    [Fact]
    public async Task HomePage_PlanSelection_UnlocksUploadStep()
    {
        await using var ctx = new HomePageTestContext();
        var cut = ctx.Render<Home>();
        var containerAutocomplete = cut.FindComponents<MudAutocomplete<PlannerContainer>>()[0].Instance;
        var planAutocomplete = cut.FindComponents<MudAutocomplete<PlannerPlan>>()[0].Instance;

        await cut.InvokeAsync(() => containerAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Containers[0]));
        await cut.InvokeAsync(() => planAutocomplete.ValueChanged.InvokeAsync(ctx.Gateway.Plans[0]));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Step 3", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, cut.FindAll(".step-card--locked").Count);
        });
    }
}
