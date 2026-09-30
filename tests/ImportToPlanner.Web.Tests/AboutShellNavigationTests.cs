using Bunit;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using ImportToPlanner.Web.Components.Layout;
using ImportToPlanner.Web.Features.About.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImportToPlanner.Web.Tests;

public sealed class AboutShellNavigationTests
{
    [Fact]
    public async Task HomePage_WhenSignedIn_IncludesHelpMenuWithAboutEntry()
    {
        await using var ctx = new HomePageTestContext(isAuthenticated: true);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindComponents<ImportWorkflowHelpMenu>());
        });
    }

    [Fact]
    public async Task MainLayout_FooterIncludesAboutLink()
    {
        await using var ctx = new HomePageTestContext(isAuthenticated: false);
        RegisterMainLayoutDependencies(ctx.Services);

        var cut = ctx.Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Body content"))));

        cut.WaitForAssertion(() =>
        {
            var aboutLinks = cut.FindAll("a")
                .Where(anchor => string.Equals(anchor.GetAttribute("href"), "/about", StringComparison.Ordinal))
                .ToList();
            Assert.Single(aboutLinks);
            Assert.Contains("About", aboutLinks[0].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HelpMenu_WhenUnsigned_AboutSelectionRoutesThroughSignIn()
    {
        await using var ctx = new HomePageTestContext(isAuthenticated: false);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var cut = ctx.Render<ImportWorkflowHelpMenu>();

        await InvokeAboutClickedAsync(cut);

        Assert.Contains("MicrosoftIdentity/Account/SignIn", navigationManager.Uri, StringComparison.Ordinal);
        Assert.Contains("redirectUri=%2Fabout", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HelpMenu_WhenSignedIn_AboutSelectionNavigatesToAboutRoute()
    {
        await using var ctx = new HomePageTestContext(isAuthenticated: true);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var cut = ctx.Render<ImportWorkflowHelpMenu>();

        await InvokeAboutClickedAsync(cut);

        Assert.EndsWith("/about", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AboutPage_WhenReleaseQueryFails_ShowsErrorMessage()
    {
        await using var ctx = new HomePageTestContext(isAuthenticated: true);
        ctx.Services.RemoveAll<IReleaseInformationQuery>();
        ctx.Services.AddScoped<IReleaseInformationQuery>(_ => new ThrowingReleaseInformationQueryStub());

        var cut = ctx.Render<About>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                "Release information is temporarily unavailable. Try again later.",
                cut.Markup,
                StringComparison.Ordinal);
        });
    }

    private static void RegisterMainLayoutDependencies(IServiceCollection services)
    {
        services.AddScoped<IDemoModeSession>(_ => new DemoModeSession());
        services.AddScoped<IDemoModeAuthorisationService>(_ =>
        {
            var authorisation = Substitute.For<IDemoModeAuthorisationService>();
            authorisation.CanShowDemoControls().Returns(false);
            authorisation.CanActivateDemo().Returns(false);
            return authorisation;
        });
    }

    private static async Task InvokeAboutClickedAsync(IRenderedComponent<ImportWorkflowHelpMenu> cut)
    {
        var method = typeof(ImportWorkflowHelpMenu).GetMethod(
            "OnAboutClicked",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = (Task?)method.Invoke(cut.Instance, null);
        Assert.NotNull(task);
        await cut.InvokeAsync(async () => await task.ConfigureAwait(false));
    }
}
