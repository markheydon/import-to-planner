using Bunit;
using ImportToPlanner.Web.Features.CommercialAccounts.Pages;
using ImportToPlanner.Web.Tests.TestInfrastructure;

namespace ImportToPlanner.Web.Tests.Features.CommercialAccounts;

public sealed class ProfileCreditPurchaseTests
{
    [Fact]
    public async Task ProfilePage_WhenCommercialOn_ShowsFullPackCatalogueWithoutRecommendation()
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

        var summarySubstitute = new ProfileCreditSummaryUseCaseSubstitute { RemainingCredits = 12 };
        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditSummaryUseCaseSubstitute: summarySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Buy credit packs", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("50 credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("100 credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Recommended for this import", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cus_", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenSummaryFailsClosed_ShowsNoPackCatalogueSubstitute()
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

        var summarySubstitute = new ProfileCreditSummaryUseCaseSubstitute { FailClosed = true };
        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditSummaryUseCaseSubstitute: summarySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("temporarily unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy credit packs", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("50 credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenCommercialOff_ShowsNoBuyCreditsControls()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false, isAuthenticated: true);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Buy credit packs", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }
}
