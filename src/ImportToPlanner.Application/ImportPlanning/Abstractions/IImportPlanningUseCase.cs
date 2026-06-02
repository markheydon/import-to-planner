using ImportToPlanner.Application.ImportPlanning.Models;

namespace ImportToPlanner.Application.ImportPlanning.Abstractions;

/// <summary>
/// Defines the input boundary for planning import actions.
/// </summary>
public interface IImportPlanningUseCase
{
    /// <summary>
    /// Handles a planning request and emits a neutral preview through the output boundary.
    /// </summary>
    /// <param name="request">The planning request.</param>
    /// <param name="outputBoundary">The planning output boundary implementation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleAsync(
        ImportPlanningRequest request,
        IImportPlanningOutputBoundary outputBoundary,
        CancellationToken cancellationToken);
}
