using ImportToPlanner.Application.ImportPlanning.Models;

namespace ImportToPlanner.Application.ImportPlanning.Abstractions;

/// <summary>
/// Defines the output boundary for import planning.
/// </summary>
public interface IImportPlanningOutputBoundary
{
    /// <summary>
    /// Presents a planning response.
    /// </summary>
    /// <param name="response">The planning response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PresentAsync(
        ImportPlanPreview response,
        CancellationToken cancellationToken);
}
