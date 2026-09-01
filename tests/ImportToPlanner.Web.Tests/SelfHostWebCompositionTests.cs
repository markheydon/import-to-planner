using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class SelfHostWebCompositionTests
{
    [Fact]
    public async Task SelfHostComposition_DoesNotRegisterCommercialUseCases()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false, isAuthenticated: true);

        Assert.Null(ctx.Services.GetService<ICommercialAccessUseCase>());
        Assert.Null(ctx.Services.GetService<ICommercialProfileUseCase>());
    }

    [Fact]
    public async Task SelfHostComposition_RendersHomeWithoutCommercialServices()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false, isAuthenticated: true);

        var exception = Record.Exception(() => ctx.Render<Home>());

        Assert.Null(exception);
    }
}
