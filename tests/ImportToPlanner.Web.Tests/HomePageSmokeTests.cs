using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageSmokeTests
{
    [Fact]
    public void HomePage_InInMemoryMode_RendersImportInputSection()
    {
        // Arrange
        using var ctx = new HomePageTestContext();

        // Act
        var cut = ctx.Render<Home>();

        // Assert — verify key structural elements are present
        Assert.Contains("Import Input", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Validate And Preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirm And Execute", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HomePage_InInMemoryMode_RendersWithoutThrowing()
    {
        // Arrange
        using var ctx = new HomePageTestContext();

        // Act — verifies OnInitializedAsync completes without exception
        var exception = Record.Exception(() => ctx.Render<Home>());

        // Assert
        Assert.Null(exception);
    }
}
