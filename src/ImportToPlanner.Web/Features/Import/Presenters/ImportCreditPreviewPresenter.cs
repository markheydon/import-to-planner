using System.Globalization;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Commercial.Configuration;

namespace ImportToPlanner.Web.Features.Import.Presenters;

/// <summary>
/// Builds UK English credit preview warning copy.
/// </summary>
public static class ImportCreditPreviewPresenter
{
    /// <summary>
    /// Builds the insufficient-credits warning when would-create exceeds remaining.
    /// </summary>
    public static string BuildInsufficientCreditsWarning(
        int wouldCreateCount,
        int remainingCredits,
        int shortfall)
        => $"This import would create {wouldCreateCount} new tasks, but your organisation has {remainingCredits} credits remaining. You need {shortfall} more credits before you can confirm this import.";

    /// <summary>
    /// Builds the ledger-unavailable error message for preview or confirm.
    /// </summary>
    public static string BuildLedgerUnavailableMessage()
        => "Credit balance is temporarily unavailable. Try again shortly before confirming your import.";

    /// <summary>
    /// Counts would-create tasks from a preview.
    /// </summary>
    public static int CountWouldCreateTasks(ImportPlanPreview preview)
        => preview.TaskActions.Count(task => task.Action == PlannedEntityAction.Create);

    /// <summary>
    /// Builds remaining-credits copy for preview when complimentary access applies.
    /// </summary>
    public static string BuildComplimentaryRemainingMessage()
        => "Your organisation has unlimited complimentary credits for this import.";

    /// <summary>
    /// Formats remaining credits for preview and confirm surfaces.
    /// </summary>
    public static string FormatRemainingCredits(int? remainingCredits, bool complimentaryUnlimitedAccess)
        => complimentaryUnlimitedAccess ? "Unlimited" : remainingCredits?.ToString(CultureInfo.InvariantCulture) ?? "—";

    /// <summary>
    /// Heading for the credit pack chooser on preview when credits are insufficient.
    /// </summary>
    public static string BuildCreditPackSectionHeading()
        => "Buy additional credits";

    /// <summary>
    /// Primary action label to start checkout for the selected pack.
    /// </summary>
    public static string BuildBuyCreditsActionLabel()
        => "Buy credits";

    /// <summary>
    /// Helper copy when a pack is recommended for the active shortfall.
    /// </summary>
    public static string BuildRecommendedPackHelper()
        => "Recommended for this import";

    /// <summary>
    /// User-safe message when checkout cannot be started.
    /// </summary>
    public static string BuildCheckoutUnavailableMessage()
        => "Credit checkout is temporarily unavailable. Try again shortly.";

    /// <summary>
    /// Formats a pack for display in the preview catalogue.
    /// </summary>
    public static string FormatPackDisplayLabel(CreditPackOptions pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var amount = (pack.AmountMinor / 100m).ToString("0.00", CultureInfo.InvariantCulture);
        return $"{pack.DisplayName} — {pack.CreditQuantity.ToString(CultureInfo.InvariantCulture)} credits ({amount} {pack.Currency.ToUpperInvariant()})";
    }

    /// <summary>
    /// Builds published pack choices for preview, marking the recommended pack when one covers the shortfall.
    /// </summary>
    public static IReadOnlyList<ImportCreditPackChoice> BuildPackChoices(
        IEnumerable<CreditPackOptions> packs,
        int shortfall)
    {
        ArgumentNullException.ThrowIfNull(packs);

        var published = packs
            .Where(pack => pack.Published && pack.CreditQuantity > 0)
            .OrderBy(pack => pack.CreditQuantity)
            .ThenBy(pack => pack.PackId, StringComparer.Ordinal)
            .ToList();

        var recommendedPackId = SelectRecommendedPackId(published, shortfall);
        return published
            .Select(pack => new ImportCreditPackChoice(
                pack.PackId,
                FormatPackDisplayLabel(pack),
                pack.CreditQuantity,
                string.Equals(pack.PackId, recommendedPackId, StringComparison.Ordinal)))
            .ToList();
    }

    /// <summary>
    /// Selects the smallest published pack that covers the shortfall, if any.
    /// </summary>
    public static string? SelectRecommendedPackId(IEnumerable<CreditPackOptions> packs, int shortfall)
    {
        ArgumentNullException.ThrowIfNull(packs);
        if (shortfall <= 0)
        {
            return null;
        }

        return packs
            .Where(pack => pack.Published && pack.CreditQuantity >= shortfall)
            .OrderBy(pack => pack.CreditQuantity)
            .ThenBy(pack => pack.PackId, StringComparer.Ordinal)
            .Select(pack => pack.PackId)
            .FirstOrDefault();
    }
}

/// <summary>
/// One selectable credit pack on the import preview step.
/// </summary>
/// <param name="PackId">Stable pack slug.</param>
/// <param name="DisplayLabel">UK English label for UI.</param>
/// <param name="CreditQuantity">Credits granted on confirmed payment.</param>
/// <param name="IsRecommended">Whether this pack is recommended for the active shortfall.</param>
public sealed record ImportCreditPackChoice(
    string PackId,
    string DisplayLabel,
    int CreditQuantity,
    bool IsRecommended);

