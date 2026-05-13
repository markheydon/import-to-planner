using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Exceptions;

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

/// <summary>
/// Maps exceptions to user-safe error messages for Planner import workflows.
/// </summary>
public static class PlannerFailureMessageMapper
{
    /// <summary>
    /// Converts a planner failure into user-safe text for presenters.
    /// </summary>
    /// <param name="failure">The neutral failure metadata.</param>
    /// <returns>A user-safe error message.</returns>
    public static string ToUserSafeMessage(PlannerOperationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return failure.Category switch
        {
            PlannerFailureCategory.Authentication => "Authentication expired. Sign in again and retry.",
            PlannerFailureCategory.Authorisation => "Permission denied. Confirm the required Planner permissions and try again.",
            PlannerFailureCategory.Conflict => "Planner data changed during processing. Run a fresh preview and retry.",
            PlannerFailureCategory.Unavailable => "Planner is temporarily busy. Wait and retry the import.",
            PlannerFailureCategory.Validation => "The request is not valid. Review the input and try again.",
            _ => "An unexpected planner error occurred. Retry and check logs if the issue continues.",
        };
    }
}
