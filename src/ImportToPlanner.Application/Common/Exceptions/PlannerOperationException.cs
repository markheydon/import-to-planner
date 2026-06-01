using ImportToPlanner.Application.Common.Models;

namespace ImportToPlanner.Application.Common.Exceptions;

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
