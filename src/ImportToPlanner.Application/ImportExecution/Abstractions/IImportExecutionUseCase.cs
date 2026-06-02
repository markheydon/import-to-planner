using ImportToPlanner.Application.ImportExecution.Models;

namespace ImportToPlanner.Application.ImportExecution.Abstractions;

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
