using Bunit;
using ImportToPlanner.CommercialService.Models;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCommercialRetentionTests
{
    [Fact]
    public async Task HomePage_WhenAccountDeleted_ShowsRetentionGuidanceAndRestoreAction()
    {
        var accountStore = new CommercialAccountStoreStub();
        var deletedUtc = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                DeletedUtc: deletedUtc,
                RetentionExpiresUtc: deletedUtc.AddMonths(6),
                RestoredUtc: null,
                LastSignInOutcomeUtc: deletedUtc),
            CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("retention period", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Restore account", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Select Planner location", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenRestoreClicked_ReactivateDeletedAccount()
    {
        var accountStore = new CommercialAccountStoreStub();
        var deletedUtc = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                DeletedUtc: deletedUtc,
                RetentionExpiresUtc: deletedUtc.AddMonths(6),
                RestoredUtc: null,
                LastSignInOutcomeUtc: deletedUtc),
            CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore);

        var cut = ctx.Render<Home>();
        cut.WaitForAssertion(() => Assert.Contains("Restore account", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Restore account", StringComparison.OrdinalIgnoreCase))
            .Click();

        var restoredAccount = Assert.Single(ctx.CommercialAccountStore.Accounts);
        Assert.Equal(CommercialAccountStatus.Active, restoredAccount.Status);
    }
}
