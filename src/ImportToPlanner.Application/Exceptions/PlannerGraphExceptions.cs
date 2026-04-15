namespace ImportToPlanner.Application.Exceptions;

/// <summary>
/// Represents an authentication failure while calling Microsoft Graph for Planner operations.
/// </summary>
public sealed class PlannerAuthenticationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlannerAuthenticationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents an authorization failure while calling Microsoft Graph for Planner operations.
/// </summary>
public sealed class PlannerPermissionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerPermissionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlannerPermissionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents a missing Graph resource during Planner operations.
/// </summary>
public sealed class PlannerNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlannerNotFoundException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents a Graph concurrency conflict during Planner operations.
/// </summary>
public sealed class PlannerConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerConflictException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlannerConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents Graph throttling during Planner operations.
/// </summary>
public sealed class PlannerThrottlingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerThrottlingException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PlannerThrottlingException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}