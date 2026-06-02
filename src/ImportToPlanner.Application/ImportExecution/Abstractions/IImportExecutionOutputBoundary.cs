using ImportToPlanner.Application.ImportExecution.Models;

namespace ImportToPlanner.Application.ImportExecution.Abstractions;

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
