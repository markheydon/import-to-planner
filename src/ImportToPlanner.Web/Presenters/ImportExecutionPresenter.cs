using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Exceptions;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Presenters;

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
        var errorItems = response.FailureItems
            .Select(PlannerFailureMessageMapper.ToUserSafeMessage)
            .ToArray();

        ViewModel = new ImportExecutionReportViewModel(
            response.PlanId,
            createdItems,
            reusedOrSkippedItems,
            response.ManualActions,
            errorItems,
            response.OutcomeSummary);

        return Task.CompletedTask;
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
public sealed record ImportExecutionReportViewModel(
    string? PlanId,
    IReadOnlyList<string> Created,
    IReadOnlyList<string> ReusedOrSkipped,
    IReadOnlyList<ManualAction> ManualActions,
    IReadOnlyList<string> Errors,
    ImportExecutionOutcomeSummary OutcomeSummary);
