using Bunit;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageSmokeTests
{
    [Fact]
    public async Task HomePage_InInMemoryMode_RendersFiveStepWorkflow()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert — verify key structural elements are present
        Assert.Equal(5, cut.FindAll(".step-card").Count);
        Assert.Contains("Step 1", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Step 5", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Validate and preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirm and execute", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomePage_InInMemoryMode_RendersWithoutThrowing()
    {
        // Arrange
        await using var ctx = new HomePageTestContext();

        // Act — verifies OnInitializedAsync completes without exception
        var exception = Record.Exception(() => ctx.Render<Home>());

        // Assert
        Assert.Null(exception);
    }
}
