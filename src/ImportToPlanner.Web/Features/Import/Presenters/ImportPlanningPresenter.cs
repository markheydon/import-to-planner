using ImportToPlanner.Application.ImportPlanning.Abstractions;
using ImportToPlanner.Application.ImportPlanning.Models;

namespace ImportToPlanner.Web.Features.Import.Presenters;

/// <summary>
/// Presents planning results for the web workflow.
/// </summary>
public sealed class ImportPlanningPresenter : IImportPlanningOutputBoundary
{
    /// <summary>
    /// Gets the latest planning view model.
    /// </summary>
    public ImportPlanningViewModel? ViewModel { get; private set; }

    /// <inheritdoc/>
    public Task PresentAsync(ImportPlanPreview response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(response);

        ViewModel = new ImportPlanningViewModel(
            response,
            response.BucketActions
                .OrderBy(action => action.Key, StringComparer.OrdinalIgnoreCase)
                .Select(action => new ImportBucketActionViewModel(action.Key, action.Value.ToString()))
                .ToArray(),
            response.TaskActions
                .Select(task => new ImportTaskActionViewModel(
                    task.RowNumber,
                    task.TaskName,
                    task.Bucket,
                    task.Goals is { Count: > 0 } ? string.Join(", ", task.Goals) : string.Empty,
                    task.Action.ToString(),
                    task.Reason))
                .ToArray());

        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents planning output shaped for UI binding.
/// </summary>
/// <param name="Preview">The neutral planning preview.</param>
/// <param name="BucketActions">Bucket rows for display.</param>
/// <param name="TaskActions">Task rows for display.</param>
public sealed record ImportPlanningViewModel(
    ImportPlanPreview Preview,
    IReadOnlyList<ImportBucketActionViewModel> BucketActions,
    IReadOnlyList<ImportTaskActionViewModel> TaskActions);

/// <summary>
/// Represents one bucket action in the planning view.
/// </summary>
/// <param name="BucketName">The bucket name.</param>
/// <param name="Action">The action label.</param>
public sealed record ImportBucketActionViewModel(string BucketName, string Action);

/// <summary>
/// Represents one task action in the planning view.
/// </summary>
/// <param name="RowNumber">The CSV row number.</param>
/// <param name="TaskName">The task name.</param>
/// <param name="Bucket">The bucket name.</param>
/// <param name="Goals">The goals string.</param>
/// <param name="Action">The action label.</param>
/// <param name="Reason">The optional reason text.</param>
public sealed record ImportTaskActionViewModel(
    int RowNumber,
    string TaskName,
    string Bucket,
    string Goals,
    string Action,
    string? Reason);
