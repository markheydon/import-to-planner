using System.Globalization;
using ImportToPlanner.Commercial.Models;
using ImportToPlanner.Web.Features.Import.Presenters;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Presenters;

/// <summary>
/// UK English copy for the profile credits section.
/// </summary>
public static class ProfileCreditPresenter
{
    /// <summary>
    /// Section heading for organisation credits.
    /// </summary>
    public static string BuildCreditsSectionHeading() => "Organisation credits";

    /// <summary>
    /// Remaining credits headline label.
    /// </summary>
    public static string BuildRemainingLabel() => "Remaining credits";

    /// <summary>
    /// Formats remaining credits for profile display.
    /// </summary>
    public static string FormatRemainingCredits(ProfileCreditSummaryResult summary)
        => ImportCreditPreviewPresenter.FormatRemainingCredits(
            summary.ComplimentaryUnlimitedAccess ? null : summary.RemainingCredits,
            summary.ComplimentaryUnlimitedAccess);

    /// <summary>
    /// Complimentary unlimited helper copy.
    /// </summary>
    public static string BuildComplimentaryRemainingDescription()
        => ImportCreditPreviewPresenter.BuildComplimentaryRemainingMessage();

    /// <summary>
    /// Balance read failure message (fail closed).
    /// </summary>
    public static string BuildBalanceUnavailableMessage()
        => "Your organisation credit balance is temporarily unavailable. Try again shortly.";

    /// <summary>
    /// Activity read failure message.
    /// </summary>
    public static string BuildActivityUnavailableMessage()
        => "Credit activity is temporarily unavailable. Your balance above may still be correct.";

    /// <summary>
    /// Free monthly lot subsection heading.
    /// </summary>
    public static string BuildFreeMonthlyLotHeading() => "Current free monthly allowance";

    /// <summary>
    /// Describes the granted free monthly quantity.
    /// </summary>
    public static string BuildFreeMonthlyGrantedCopy(int grantedQuantity)
        => $"Granted this month: {grantedQuantity.ToString(CultureInfo.InvariantCulture)} credits";

    /// <summary>
    /// Describes remaining free monthly credits.
    /// </summary>
    public static string BuildFreeMonthlyRemainingCopy(int remainingQuantity)
        => $"Remaining from this allowance: {remainingQuantity.ToString(CultureInfo.InvariantCulture)} credits";

    /// <summary>
    /// Describes UTC month-end expiry for the free lot.
    /// </summary>
    public static string BuildFreeMonthlyExpiryCopy(DateTimeOffset expiresAtUtc)
        => $"Unused allowance expires at the end of the UTC month ({expiresAtUtc.UtcDateTime:dd MMM yyyy} UTC).";

    /// <summary>
    /// Paid lot subsection heading.
    /// </summary>
    public static string BuildPaidLotsHeading() => "Paid credit lots";

    /// <summary>
    /// Describes one paid lot row.
    /// </summary>
    public static string BuildPaidLotCopy(ProfilePaidLot lot)
        => $"{lot.RemainingQuantity.ToString(CultureInfo.InvariantCulture)} of {lot.GrantedQuantity.ToString(CultureInfo.InvariantCulture)} credits remaining (expires {lot.ExpiresAtUtc.UtcDateTime:dd MMM yyyy} UTC)";

    /// <summary>
    /// Activity subsection heading.
    /// </summary>
    public static string BuildActivityHeading() => "Credit activity (last 12 months)";

    /// <summary>
    /// Older history omission notice.
    /// </summary>
    public static string BuildOlderHistoryNotice()
        => "Older credit activity is not shown here. Only the last twelve months appear in this list.";

    /// <summary>
    /// Maps an activity kind to a UK English label.
    /// </summary>
    public static string BuildActivityKindLabel(ProfileCreditActivityKind kind)
        => kind switch
        {
            ProfileCreditActivityKind.FreeMonthlyGrant => "Free monthly grant",
            ProfileCreditActivityKind.FreeCreditExpiry => "Free credits expired",
            ProfileCreditActivityKind.ImportRunUsage => "Import usage",
            ProfileCreditActivityKind.PaidPurchase => "Credit purchase",
            ProfileCreditActivityKind.PaidLotExpiry => "Paid credits expired",
            _ => "Credit activity",
        };

    /// <summary>
    /// Formats one activity row quantity with direction.
    /// </summary>
    public static string BuildActivityQuantityCopy(ProfileCreditActivityEntry entry)
    {
        var quantity = entry.Quantity.ToString(CultureInfo.InvariantCulture);
        return entry.IsCreditIncrease ? $"+{quantity} credits" : $"-{quantity} credits";
    }

    /// <summary>
    /// Buy credits button label when checkout is available.
    /// </summary>
    public static string BuildBuyCreditsLabel() => "Buy credits";
}
