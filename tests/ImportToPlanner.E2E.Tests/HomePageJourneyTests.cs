using ImportToPlanner.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace ImportToPlanner.E2E.Tests;

public sealed class HomePageJourneyTests
{
    [Fact]
    public async Task CommercialMode_WhenAnonymous_ShowsSignInGate()
    {
        await using var factory = new ImportToPlannerWebApplicationFactory(commercialModeEnabled: true);
        var baseAddress = factory.ServerBaseAddress;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await PlaywrightTestPrerequisites.LaunchChromiumAsync(playwright);
        var page = await browser.NewPageAsync();

        var response = await page.GotoAsync(new Uri(baseAddress, "/").ToString());
        Assert.NotNull(response);
        Assert.True(response.Ok);

        var content = await page.ContentAsync();
        Assert.Contains("Sign in to Import To Planner", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelfHost_WhenSignedIn_ShowsFiveStepImportWorkflow()
    {
        await using var factory = new ImportToPlannerWebApplicationFactory(commercialModeEnabled: false);
        var baseAddress = factory.ServerBaseAddress;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await PlaywrightTestPrerequisites.LaunchChromiumAsync(playwright);
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(new Uri(baseAddress, "/e2e/sign-in").ToString());
        await page.WaitForURLAsync("**/", new PageWaitForURLOptions { Timeout = 15000 });

        var content = await page.ContentAsync();
        Assert.Contains("Select Planner location", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Upload CSV", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preview and confirm", content, StringComparison.OrdinalIgnoreCase);
    }
}
