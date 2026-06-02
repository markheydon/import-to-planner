namespace ImportToPlanner.Application.ImportExecution.Exceptions;

/// <summary>
/// Represents a stale import preview that no longer matches the current Planner state.
/// </summary>
public sealed class StaleImportPreviewException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaleImportPreviewException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StaleImportPreviewException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
