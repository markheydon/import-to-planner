using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;

namespace ImportToPlanner.Web.Features.Import.Presenters;

/// <summary>
/// Presents execution results for the web workflow.
/// </summary>
public sealed class ImportExecutionPresenter(ExecutionReportCsvExporter csvExporter) : IImportExecutionOutputBoundary
{
    private ImportExecutionResult? _lastExecutionResult;

    /// <summary>
    /// Gets the latest execution report view model.
    /// </summary>
    public ImportExecutionReportViewModel? ViewModel { get; private set; }

    /// <inheritdoc/>
    public Task PresentAsync(ImportExecutionResult response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(response);

        _lastExecutionResult = response;

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

    /// <summary>
    /// Builds the execution report CSV download payload from the latest presented result.
    /// </summary>
    /// <returns>The CSV file when a report is available; otherwise null.</returns>
    public ExecutionReportCsvFile? TryBuildExecutionReportCsvDownload()
    {
        if (_lastExecutionResult is null || ViewModel is null)
        {
            return null;
        }

        var rows = BuildExportRows(_lastExecutionResult, ViewModel);
        return csvExporter.Export(rows, DateTimeOffset.UtcNow);
    }

    private static List<ExecutionReportCsvRow> BuildExportRows(
        ImportExecutionResult executionResult,
        ImportExecutionReportViewModel viewModel)
    {
        var rows = executionResult.SourceRowOutcomes
            .OrderBy(outcome => outcome.RowNumber)
            .Select(outcome => MapSourceRowOutcome(outcome, executionResult))
            .ToList();

        foreach (var manualAction in viewModel.ManualActions)
        {
            rows.Add(MapManualFollowUpRow(manualAction));
        }

        return rows;
    }

    private static ExecutionReportCsvRow MapSourceRowOutcome(
        ImportSourceRowOutcome outcome,
        ImportExecutionResult executionResult)
    {
        var outcomeLabel = outcome.Outcome switch
        {
            ImportSourceRowOutcomeKind.Created => "Created",
            ImportSourceRowOutcomeKind.ReusedOrSkipped => "Reused or skipped",
            ImportSourceRowOutcomeKind.Failed => "Failed",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome.Outcome, "Unknown source row outcome."),
        };

        return new ExecutionReportCsvRow(
            ExecutionReportCsvRecordType.SourceRow,
            outcome.RowNumber,
            outcome.TaskName,
            outcomeLabel,
            outcome.TaskIdentifier,
            ResolveSourceRowReasonOrDetails(outcome, executionResult),
            null,
            null);
    }

    private static string? ResolveSourceRowReasonOrDetails(
        ImportSourceRowOutcome outcome,
        ImportExecutionResult executionResult)
    {
        if (outcome.Outcome == ImportSourceRowOutcomeKind.Failed)
        {
            var failure = FindTaskFailure(outcome.TaskName, executionResult.FailureItems);
            return failure is not null ? MapFailureMessage(failure) : outcome.Reason;
        }

        if (outcome.Outcome == ImportSourceRowOutcomeKind.Created && !string.IsNullOrWhiteSpace(outcome.Reason))
        {
            var creditRecordFailure = FindTaskFailure(outcome.TaskName, executionResult.FailureItems);
            if (creditRecordFailure is not null
                && string.Equals(creditRecordFailure.DiagnosticCode, "credits.usage_record_failed", StringComparison.Ordinal))
            {
                return MapFailureMessage(creditRecordFailure);
            }
        }

        return outcome.Reason;
    }

    private static PlannerOperationFailure? FindTaskFailure(
        string taskName,
        IReadOnlyList<PlannerOperationFailure> failures)
    {
        return failures.FirstOrDefault(failure =>
            failure.Target == PlannerFailureTarget.Task
            && string.Equals(failure.Reference, taskName, StringComparison.Ordinal));
    }

    private static ExecutionReportCsvRow MapManualFollowUpRow(ManualActionViewModel manualAction)
    {
        return new ExecutionReportCsvRow(
            ExecutionReportCsvRecordType.ManualFollowUp,
            null,
            manualAction.TaskName,
            "Manual follow-up",
            null,
            manualAction.Details,
            manualAction.ActionType,
            manualAction.GoalName);
    }

    private static string MapFailureMessage(PlannerOperationFailure failure)
    {
        return failure.DiagnosticCode switch
        {
            "credits.exhausted" => $"Credit exhausted: task '{failure.Reference}' was not created because your organisation has no credits remaining.",
            "credits.usage_record_failed" => $"Credit recording failed: task '{failure.Reference}' was created in Planner, but its credit usage could not be recorded.",
            "credits.ledger_unavailable" => "Import could not continue because credit balance is unavailable.",
            "credits.balance_report_unavailable" => "Remaining credits could not be loaded for this execution report.",
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
            "AssignPersonToTask" => (
                "Assign person to task",
                FormatAssignPersonDetails(action)),
            _ => (
                action.ActionType,
                action.Details ?? "Review this item manually in Planner."),
        };

        return new ManualActionViewModel(displayActionType, action.GoalName, action.TaskName, details);
    }

    private static string FormatAssignPersonDetails(ManualAction action)
    {
        var reason = action.Details switch
        {
            "not-a-member" => "This person is not a member of the destination and could not be assigned automatically.",
            "not-an-address" => "The value is not a work email or sign-in name and could not be matched.",
            "assignment-refused" => "Planner refused the assignment; assign this person manually in Planner.",
            "destination-limit" => "The destination limit for assignees was reached; assign this person manually in Planner.",
            _ => "Assign this person manually in Planner.",
        };

        if (string.IsNullOrWhiteSpace(action.PersonIdentifier))
        {
            return reason;
        }

        return $"{action.PersonIdentifier}: {reason}";
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
