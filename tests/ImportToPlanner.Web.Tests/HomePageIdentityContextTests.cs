using Bunit;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests;

public sealed class HomePageIdentityContextTests
{
    [Fact]
    public async Task HomePage_WhenSignedIn_ShowsSessionEmailAddress()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true, isAuthenticated: true);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("user@contoso.com", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenTenantNameAvailable_ShowsTenantNameInIdentitySummary()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: true, isAuthenticated: true);

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Contoso", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task HomePage_WhenCommercialModeEnabledAndSignedIn_ShowsProfileLink()
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
            Assert.Contains("/profile", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Profile", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }
}
