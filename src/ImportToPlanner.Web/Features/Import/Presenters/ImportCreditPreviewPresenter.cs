using ImportToPlanner.Application.Models;

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
}
