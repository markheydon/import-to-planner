using Bunit;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;
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
        Assert.Equal(5, cut.FindAll(".step-card").Count);
        Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Select plan", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Upload CSV", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preview import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirm and import", cut.Markup, StringComparison.OrdinalIgnoreCase);
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

        // Act
        var cut = ctx.Render<Home>();

        // Assert
        Assert.Contains("Preview import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirm and import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Validate and preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Confirm and execute", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
}
