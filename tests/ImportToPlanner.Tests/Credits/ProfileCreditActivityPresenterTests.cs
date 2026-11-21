using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Web.Features.CommercialAccounts.Presenters;

namespace ImportToPlanner.Tests.Credits;

public sealed class ProfileCreditActivityPresenterTests
{
    [Fact]
    public void BuildActivityKindLabel_PaidAdjustment_UsesRefundCopy()
    {
        var label = ProfileCreditPresenter.BuildActivityKindLabel(ProfileCreditActivityKind.PaidAdjustment);

        Assert.Equal("Refund of unused credits", label);
    }

    [Fact]
    public void BuildActivityQuantityCopy_PaidAdjustment_ShowsDebit()
    {
        var entry = new ProfileCreditActivityEntry(
            DateTimeOffset.UtcNow,
            ProfileCreditActivityKind.PaidAdjustment,
            15,
            null,
            false);

        var copy = ProfileCreditPresenter.BuildActivityQuantityCopy(entry);

        Assert.Equal("-15 credits", copy);
    }
}
