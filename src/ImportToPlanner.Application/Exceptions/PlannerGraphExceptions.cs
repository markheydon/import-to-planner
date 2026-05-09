using System.Text.RegularExpressions;

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

/// <summary>
/// Maps exceptions to user-safe error messages for Planner import workflows.
/// </summary>
public static partial class PlannerGraphErrorMapper
{
    /// <summary>
    /// Converts an exception into a user-safe message that avoids tenant-sensitive details.
    /// </summary>
    /// <param name="exception">The source exception.</param>
    /// <param name="fallbackMessage">The fallback message if no specific mapping is available.</param>
    /// <returns>A user-safe error message.</returns>
    public static string ToUserSafeMessage(Exception exception, string fallbackMessage)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackMessage);

        return exception switch
        {
            PlannerAuthenticationException => "Authentication expired. Sign in again and retry.",
            PlannerPermissionException => "Permission denied. Confirm the required Planner permissions and try again.",
            PlannerNotFoundException => "Planner resource no longer exists. Refresh and run a fresh preview.",
            PlannerConflictException => "Planner data changed during processing. Run a fresh preview and retry.",
            PlannerThrottlingException => "Planner is temporarily busy. Wait and retry the import.",
            _ => RedactSensitiveTokens(fallbackMessage),
        };
    }

    private static string RedactSensitiveTokens(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "An unexpected error occurred while processing Planner data.";
        }

        var redacted = SensitiveTokenPattern().Replace(message, "[redacted]");
        return redacted;
    }

    [GeneratedRegex("(?i)(tenant|secret|certificate|cert|token|client[-_ ]?id|thumbprint)\\s*[:=]\\s*[^\\s,;]+")]
    private static partial Regex SensitiveTokenPattern();
}
