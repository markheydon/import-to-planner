using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Components.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class ProfilePageTests
{
    [Fact]
    public async Task ProfilePage_WhenAccountExists_ShowsStoredAccountDetails()
    {
        var accountStore = new CommercialAccountStoreStub();
        var createdUtc = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                createdUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: createdUtc),
            CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tenant ID", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tenant-001", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("User ID", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("user-001", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Delete account", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenDeletionConfirmed_MarksAccountDeleted()
    {
        var accountStore = new CommercialAccountStoreStub();
        var createdUtc = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                createdUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: createdUtc),
            CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore);

        var cut = ctx.Render<Profile>();
        cut.WaitForAssertion(() => Assert.Contains("Delete account", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Delete account", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() => Assert.Contains("Confirm delete account", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Confirm delete account", StringComparison.OrdinalIgnoreCase))
            .Click();

        var deletedAccount = Assert.Single(ctx.CommercialAccountStore.Accounts);
        Assert.Equal(CommercialAccountStatus.Deleted, deletedAccount.Status);
    }
}
