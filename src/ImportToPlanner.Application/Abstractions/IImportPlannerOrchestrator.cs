using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Coordinates dry-run planning and execution for CSV imports.
/// </summary>
public interface IImportPlannerOrchestrator
{
    /// <summary>
    /// Builds a dry-run preview of all create/reuse/skip actions.
    /// </summary>
    /// <param name="request">The normalized import request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dry-run preview.</returns>
    Task<ImportPlanPreview> BuildPreviewAsync(ImportRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the import actions represented by a dry-run preview.
    /// </summary>
    /// <param name="request">The normalized import request.</param>
    /// <param name="preview">The preview generated for the same request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution report.</returns>
    Task<ImportExecutionResult> ExecuteAsync(
        ImportRequest request,
        ImportPlanPreview preview,
        CancellationToken cancellationToken);
}
