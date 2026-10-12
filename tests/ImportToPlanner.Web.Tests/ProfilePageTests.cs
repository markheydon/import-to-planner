using Bunit;
using ImportToPlanner.Web.Features.CommercialAccounts.Pages;
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

    [Fact]
    public async Task ProfilePage_WhenAccountAlreadyDeleted_ShowsScheduledDeletionStatusAndNoDeleteAction()
    {
        var accountStore = new CommercialAccountStoreStub();
        var deletedUtc = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        var retentionExpiresUtc = deletedUtc.AddMonths(6);

        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                DeletedUtc: deletedUtc,
                RetentionExpiresUtc: retentionExpiresUtc,
                RestoredUtc: null,
                LastSignInOutcomeUtc: deletedUtc),
            CancellationToken.None);

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("scheduled for deletion", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(retentionExpiresUtc.UtcDateTime.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture), cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Confirm delete account", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenCommercialOn_ShowsRemainingCreditsFromSummary()
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

        var summarySubstitute = new ProfileCreditSummaryUseCaseSubstitute { RemainingCredits = 18 };
        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditSummaryUseCaseSubstitute: summarySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Organisation credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("18", cut.Markup, StringComparison.Ordinal);
            Assert.Equal("tenant-001", summarySubstitute.LastTenantId);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenSummaryFails_ShowsBalanceErrorWithoutZeroRemaining()
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
            Assert.DoesNotContain("Remaining credits</", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("insufficient", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Buy credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenCommercialOff_DoesNotShowCreditsSection()
    {
        await using var ctx = new HomePageTestContext(commercialModeEnabled: false, isAuthenticated: true);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Organisation credits", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenFreeMonthlyLotPresent_ShowsLotDetails()
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

        var expiresAtUtc = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var summarySubstitute = new ProfileCreditSummaryUseCaseSubstitute
        {
            RemainingCredits = 20,
            FreeMonthlyLot = new ProfileFreeMonthlyLot(25, 20, expiresAtUtc, createdUtc),
        };

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditSummaryUseCaseSubstitute: summarySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current free monthly allowance", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Granted this month: 25 credits", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Remaining from this allowance: 20 credits", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenActivityHasOlderHistory_ShowsOmissionCopy()
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

        var activitySubstitute = new ProfileCreditActivityUseCaseSubstitute { HasOlderHistory = true };

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditActivityUseCaseSubstitute: activitySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("last twelve months", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenActivityFailsButSummarySucceeds_ShowsActivityErrorOnly()
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
        var activitySubstitute = new ProfileCreditActivityUseCaseSubstitute { FailClosed = true };

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditSummaryUseCaseSubstitute: summarySubstitute,
            profileCreditActivityUseCaseSubstitute: activitySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("12", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Credit activity is temporarily unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProfilePage_WhenComplimentary_ShowsUnlimitedWithoutFreeLot()
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

        var summarySubstitute = new ProfileCreditSummaryUseCaseSubstitute { ComplimentaryUnlimitedAccess = true };
        var occurredUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var activitySubstitute = new ProfileCreditActivityUseCaseSubstitute
        {
            Entries =
            [
                new ProfileCreditActivityEntry(
                    occurredUtc,
                    ProfileCreditActivityKind.ImportRunUsage,
                    3,
                    "run-1",
                    false),
            ],
        };

        await using var ctx = new HomePageTestContext(
            commercialModeEnabled: true,
            isAuthenticated: true,
            commercialAccountStoreStub: accountStore,
            profileCreditSummaryUseCaseSubstitute: summarySubstitute,
            profileCreditActivityUseCaseSubstitute: activitySubstitute);

        var cut = ctx.Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unlimited", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Current free monthly allowance", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Import usage", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }
}
