using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.Import.Presenters;

/// <summary>
/// Presents execution results for the web workflow.
/// </summary>
public sealed class ImportExecutionPresenter : IImportExecutionOutputBoundary
{
    /// <summary>
    /// Gets the latest execution report view model.
    /// </summary>
    public ImportExecutionReportViewModel? ViewModel { get; private set; }

    /// <inheritdoc/>
    public Task PresentAsync(ImportExecutionResult response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(response);

        var createdItems = response.CreatedItems
            .Select(item => $"{item.Target}: {item.Name}")
            .ToArray();
        var reusedOrSkippedItems = response.ReusedOrSkippedItems
            .Select(item => $"{item.Target}: {item.Name}")
            .ToArray();
        var manualActions = response.ManualActions
            .Select(MapManualAction)
            .ToArray();
        var errorItems = response.FailureItems
            .Select(failure => MapFailureMessage(failure))
            .ToArray();
        var tasksCreatedCount = response.CreatedItems.Count(item => item.Target == PlannerFailureTarget.Task);

        ViewModel = new ImportExecutionReportViewModel(
            response.PlanId,
            createdItems,
            reusedOrSkippedItems,
            manualActions,
            errorItems,
            response.OutcomeSummary,
            tasksCreatedCount,
            response.CreditsUsed,
            response.RemainingCredits);

        return Task.CompletedTask;
    }

    private static string MapFailureMessage(PlannerOperationFailure failure)
    {
        return failure.DiagnosticCode switch
        {
            "credits.exhausted" => $"Credit exhausted: task '{failure.Reference}' was not created because your organisation has no credits remaining.",
            "credits.usage_record_failed" => $"Credit recording failed: task '{failure.Reference}' was created in Planner, but its credit usage could not be recorded.",
            "credits.ledger_unavailable" => "Import could not continue because credit balance is unavailable.",
            _ => PlannerFailureMessageMapper.ToUserSafeMessage(failure),
        };
    }

    private static ManualActionViewModel MapManualAction(ManualAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var (displayActionType, details) = action.ActionType switch
        {
            "EnsureGoalExists" => (
                "Ensure Goal Exists",
                "Verify this goal/category exists in Planner, create it if needed, then link imported tasks to it."),
            "LinkTaskToGoal" => (
                "Link Task To Goal",
                "Link this task to the goal manually in Planner."),
            _ => (
                action.ActionType,
                action.Details ?? "Review this item manually in Planner."),
        };

        return new ManualActionViewModel(displayActionType, action.GoalName, action.TaskName, details);
    }
}

/// <summary>
/// Represents presenter-owned execution report output.
/// </summary>
/// <param name="PlanId">The plan identifier.</param>
/// <param name="Created">Created item lines.</param>
/// <param name="ReusedOrSkipped">Reused/skipped item lines.</param>
/// <param name="ManualActions">Manual actions.</param>
/// <param name="Errors">Error lines.</param>
/// <param name="OutcomeSummary">Aggregate counters.</param>
/// <param name="TasksCreatedCount">Number of tasks created (not buckets).</param>
/// <param name="CreditsUsed">Credits used during the run when commercial metering is active.</param>
/// <param name="RemainingCredits">Remaining credits after the run when commercial metering is active.</param>
public sealed record ImportExecutionReportViewModel(
    string? PlanId,
    IReadOnlyList<string> Created,
    IReadOnlyList<string> ReusedOrSkipped,
    IReadOnlyList<ManualActionViewModel> ManualActions,
    IReadOnlyList<string> Errors,
    ImportExecutionOutcomeSummary OutcomeSummary,
    int TasksCreatedCount = 0,
    int? CreditsUsed = null,
    int? RemainingCredits = null);

/// <summary>
/// Represents one manual action row shaped for web presentation.
/// </summary>
/// <param name="ActionType">The presenter-owned action type label.</param>
/// <param name="GoalName">The optional goal name.</param>
/// <param name="TaskName">The optional task name.</param>
/// <param name="Details">The presenter-owned details text.</param>
public sealed record ManualActionViewModel(
    string ActionType,
    string? GoalName,
    string? TaskName,
    string Details);
