using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCommercialOffCreditTests
{
    [Fact]
    public async Task HomePage_WhenCommercialOff_ShowsNoCreditCopyAndAllowsConfirm()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false);
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
            Assert.DoesNotContain("credits remaining", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Credits used", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.False(confirmButton.HasAttribute("disabled"));
            Assert.Null(state.CreditBalanceSnapshot);
        });
    }

    [Fact]
    public async Task HomePage_WhenCommercialOff_CompletesExecuteWithoutCreditFiguresOrEnsureCalls()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false);
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();

        state.SelectedContainer = ctx.Gateway.Containers[0];
        state.SelectedPlan = ctx.Gateway.Plans[0];
        state.CsvContent = "Task Name\nTask A";
        state.ActiveTenantContext = new TenantContext(
            "tenant-001",
            "tenant-key",
            "user-001",
            SupportedAccountType.WorkOrSchool,
            "Contoso");

        await coordinator.BuildPreviewAsync(state, CancellationToken.None);
        await coordinator.ExecuteAsync(state, CancellationToken.None);

        Assert.NotNull(state.ExecutionReport);
        Assert.Null(state.ExecutionReport.CreditsUsed);
        Assert.Null(state.ExecutionReport.RemainingCredits);
        Assert.Equal(0, ctx.CreditEnsureUseCase.EnsureCallCount);

        var cut = ctx.Render<HomeExecutionReport>(
            parameters => parameters.Add(component => component.ExecutionResult, state.ExecutionReport));

        Assert.DoesNotContain("Credits used", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Remaining credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
