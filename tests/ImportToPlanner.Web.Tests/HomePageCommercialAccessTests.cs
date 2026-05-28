using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCommercialAccessTests
{
    [Fact]
    public async Task HomePage_WhenCommercialModeEnabledAndUserIsSignedOut_ShowsCommercialLoginGate()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true, isAuthenticated: false);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sign in to Import To Planner", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("signing in creates your account", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("minimum details required", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Sign in", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CSV to Planner Import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenCommercialModeEnabledAndUserSignsInForFirstTime_ShowsFirstSignInExplanationAndCreatesAccount()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true, isAuthenticated: true);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Your account has been created", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        var account = Assert.Single(ctx.CommercialAccountStore.Accounts);
        Assert.Equal("tenant-001", account.TenantId);
        Assert.Equal("user-001", account.UserId);
    }

    [Fact]
    public async Task HomePage_WhenCommercialModeEnabledAndUserIsReturning_DoesNotShowFirstSignInExplanation()
    {
        var commercialAccountStore = new CommercialAccountStoreStub();
        await commercialAccountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: null),
            CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: commercialAccountStore);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Your account has been created", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        Assert.Single(ctx.CommercialAccountStore.Accounts);
    }
}
