using Bunit;
using ImportToPlanner.Domain;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageSmokeTests
{
    [Fact]
    public async Task HomePage_WithoutThemeCascade_DisablesThemeMenu()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert
        var themeMenu = cut.FindComponent<MudMenu>();
        Assert.True(themeMenu.Instance.Disabled);
    }

    [Fact]
    public async Task HomePage_InSupportedGraphPath_RendersFiveStepWorkflow()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert — verify key structural elements are present
        Assert.Single(cut.FindComponents<MudStepper>());
        Assert.Equal(5, cut.FindComponents<MudStep>().Count);
        Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Select plan", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Upload CSV", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preview and confirm", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Report", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Confirm and import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Step 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Step 5", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InSupportedGraphPath_RendersIntroHeaderWithThemeAndAuthControls()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert
        Assert.Contains("CSV to Planner Import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Theme mode", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Required field", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Task Name", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Description", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Priority", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bucket", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Goal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sign out", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InSupportedGraphPath_UsesUpdatedActionLabels()
    {
        // Arrange
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

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Preview import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Confirm import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
        Assert.DoesNotContain("Validate and preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Confirm and execute", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Confirm and import", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InSupportedGraphPath_ShowsConciseManualFollowUpGuidance()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert
        Assert.Contains("manual follow-up", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("confirming goals", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InSupportedGraphPath_RendersWithoutThrowing()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act — verifies OnInitializedAsync completes without exception
        var exception = Record.Exception(() => ctx.Render<Home>());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task HomePage_WhenCommercialModeDisabled_DoesNotShowProfileLink()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false, isAuthenticated: true);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=\"/profile\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenCommercialModeDisabled_DoesNotShowCommercialLoginGate()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false, isAuthenticated: true);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Sign in with Microsoft 365 to continue", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }
}
