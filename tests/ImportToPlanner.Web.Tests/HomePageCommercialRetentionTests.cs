using Bunit;
using ImportToPlanner.Web.Tests.TestInfrastructure;
using MudBlazor;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageCommercialRetentionTests
{
    [Fact]
    public async Task HomePage_WhenAccountDeleted_ShowsRetentionGuidanceAndRestoreAction()
    {
        var accountStore = new CommercialAccountStoreStub();
        await accountStore.CreateAsync(CreateDeletedAccountWithinRetention(), CancellationToken.None);

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
            Assert.Empty(cut.FindComponents<MudStepper>());
            Assert.DoesNotContain("Your import", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenRestoreClicked_ReactivateDeletedAccount()
    {
        var accountStore = new CommercialAccountStoreStub();
        await accountStore.CreateAsync(CreateDeletedAccountWithinRetention(), CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore);

        var cut = ctx.Render<Home>();
        cut.WaitForAssertion(() => Assert.Contains("Restore account", cut.Markup, StringComparison.OrdinalIgnoreCase));

        var restoreAccountButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Restore account", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => restoreAccountButton.Click());

        cut.WaitForAssertion(() =>
        {
            var restoredAccount = Assert.Single(ctx.CommercialAccountStore.Accounts);
            Assert.Equal(CommercialAccountStatus.Active, restoredAccount.Status);
        });
    }

    private static CommercialAccount CreateDeletedAccountWithinRetention()
    {
        var referenceUtc = DateTimeOffset.UtcNow;
        var deletedUtc = referenceUtc.AddDays(-14);

        return new CommercialAccount(
            "tenant-001",
            "user-001",
            deletedUtc.AddMonths(-2),
            CommercialAccountStatus.Deleted,
            DeletedUtc: deletedUtc,
            RetentionExpiresUtc: referenceUtc.AddMonths(6),
            RestoredUtc: null,
            LastSignInOutcomeUtc: deletedUtc);
    }
}
