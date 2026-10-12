using Bunit;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class ComplimentaryCreditCommercialOffTests
{
    [Fact]
    public async Task HomePage_WhenCommercialOff_IgnoresComplimentaryAllowlistConfiguration()
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
            Assert.DoesNotContain("unlimited complimentary credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("credits remaining", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var confirmButton = cut.FindAll("button").Single(button =>
                button.TextContent.Contains("Confirm import", StringComparison.OrdinalIgnoreCase));
            Assert.False(confirmButton.HasAttribute("disabled"));
        });
    }
}
