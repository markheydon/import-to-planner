using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

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

/// <summary>
/// Defines the input boundary for executing approved import actions.
/// </summary>
public interface IImportExecutionUseCase
{
    /// <summary>
    /// Handles an execution request and emits a neutral result through the output boundary.
    /// </summary>
    /// <param name="request">The execution request.</param>
    /// <param name="outputBoundary">The execution output boundary implementation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleAsync(
        ImportExecutionRequest request,
        IImportExecutionOutputBoundary outputBoundary,
        CancellationToken cancellationToken);
}

/// <summary>
/// Defines the output boundary for import execution.
/// </summary>
public interface IImportExecutionOutputBoundary
{
    /// <summary>
    /// Presents an execution response.
    /// </summary>
    /// <param name="response">The execution response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PresentAsync(
        ImportExecutionResult response,
        CancellationToken cancellationToken);
}
