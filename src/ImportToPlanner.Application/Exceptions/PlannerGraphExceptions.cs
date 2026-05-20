using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Exceptions;

/// <summary>
/// Represents a consent resolution that blocks use-case execution.
/// </summary>
public sealed class ConsentBlockedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentBlockedException"/> class.
    /// </summary>
    /// <param name="resolution">The structured consent resolution that blocked execution.</param>
    public ConsentBlockedException(ConsentResolution resolution)
        : base("Consent resolution blocked use-case execution.")
    {
        ArgumentNullException.ThrowIfNull(resolution);
        Resolution = resolution;
    }

    /// <summary>
    /// Gets the structured consent resolution that blocked execution.
    /// </summary>
    public ConsentResolution Resolution { get; }
}

/// <summary>
/// Represents a planner operation failure surfaced by an adapter.
/// </summary>
public sealed class PlannerOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerOperationException"/> class.
    /// </summary>
    /// <param name="failure">The neutral failure metadata.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlannerOperationException(PlannerOperationFailure failure, Exception? innerException = null)
        : base(failure.Message, innerException)
    {
        Failure = failure;
    }

    /// <summary>
    /// Gets the neutral failure metadata.
    /// </summary>
    public PlannerOperationFailure Failure { get; }
}

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
