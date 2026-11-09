using Bunit;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ImportToPlanner.Web.Tests.Features.Import;

public sealed class HomeCreditPurchaseTests
{
    [Fact]
    public async Task HomePage_WhenShortfall_ShowsPackCatalogueRecommendedPackAndDisabledConfirm()
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
            Assert.Contains("50 credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("100 credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Recommended for this import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.True(confirmButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task HomePage_WhenComplimentary_ShowsNoBuyCreditsControls()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.ComplimentaryUnlimitedAccess = true;
        ctx.CreditEnsureUseCase.RemainingCredits = 0;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy additional credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenStartingCheckout_DoesNotChangeDisplayedRemainingCredits()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 0;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        var ensureCallsAfterPreview = ctx.CreditEnsureUseCase.EnsureCallCount;

        cut.WaitForAssertion(() =>
            Assert.Contains(
                cut.FindAll("button"),
                button => button.TextContent.Contains("Buy credits", StringComparison.OrdinalIgnoreCase)));

        var buyButton = cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Buy credits", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => buyButton.Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("0 credits remaining", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(ensureCallsAfterPreview, ctx.CreditEnsureUseCase.EnsureCallCount);
            Assert.Equal(1, ctx.CreditCheckoutPaymentPort!.CreateCheckoutSessionCallCount);
        });
    }

    [Fact]
    public async Task HomePage_WhenCommercialOff_ShowsNoBuyCreditsControls()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false);
        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy additional credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenLedgerUnavailableOnPreview_FailsClosedWithoutBuyCreditsSubstitute()
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
            Assert.DoesNotContain("would create", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenCheckoutReturnRefreshFails_FailsClosedWithoutInsufficientCreditsSubstitute()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true);
        ctx.CreditEnsureUseCase.RemainingCredits = 0;
        var coordinator = ctx.Services.GetRequiredService<ImportWorkflowCoordinator>();
        var state = ctx.Services.GetRequiredService<WorkflowCoordinationState>();
        var cut = ctx.Render<Home>();

        await PreparePreviewAsync(ctx, cut, coordinator, state);

        ctx.CreditEnsureUseCase.FailClosed = true;
        await coordinator.RefreshCreditBalanceSnapshotAfterCheckoutReturnAsync(state, CancellationToken.None);
        cut.Render();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".mud-step").Count >= 4));
        cut.FindAll(".mud-step")[3].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Credit balance is temporarily unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("would create", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0 credits remaining", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
