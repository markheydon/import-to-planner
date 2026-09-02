using Bunit;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCreditPreviewTests
{
    [Fact]
    public async Task HomePage_WhenWouldCreateExceedsRemaining_ShowsWarningAndDisablesConfirmImport()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 0;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("would create", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("0 credits remaining", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.True(confirmButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task HomePage_WhenWouldCreateWithinRemaining_DoesNotDisableConfirmForCreditReasons()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 25;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("would create", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.False(confirmButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task HomePage_WhenLiveRemainingIncreasesAfterPreview_EnablesConfirmImport()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 0;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.True(confirmButton.HasAttribute("disabled"));
        });

        ctx.CreditEnsureUseCase.RemainingCredits = 25;
        await coordinator.RefreshCreditBalanceSnapshotForConfirmAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.False(confirmButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task HomePage_WhenLedgerUnavailableOnPreview_FailsClosedWithoutTreatingRemainingAsZero()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.FailClosed = true;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Credit balance is temporarily unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0 credits remaining", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Dry-run preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(
                0,
                cut.FindAll("button").Count(button =>
                    button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase)));
        });
    }

    private static async Task PreparePreviewAsync(
        HomePageTestContext ctx,
        IRenderedComponent<Home> cut,
        ImportWorkflowCoordinator coordinator,
        WorkflowCoordinationState state)
    {
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
    }
}
