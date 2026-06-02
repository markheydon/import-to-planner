namespace ImportToPlanner.Application.Common.Models;

/// <summary>
/// Represents neutral failure categories for planner operations.
/// </summary>
public enum PlannerFailureCategory
{
    Authentication,
    Authorisation,
    Validation,
    Conflict,
    Unavailable,
    Unknown,
}

/// <summary>
/// Represents neutral failure targets for planner operations.
/// </summary>
public enum PlannerFailureTarget
{
    Workflow,
    Container,
    Plan,
    Bucket,
    Task,
}

/// <summary>
/// Represents a neutral planner operation failure.
/// </summary>
/// <param name="Category">The failure category.</param>
/// <param name="Target">The failed target.</param>
/// <param name="Reference">The affected entity reference, if available.</param>
/// <param name="Message">A neutral message suitable for presenter translation.</param>
/// <param name="Retryable">Indicates whether retrying is reasonable.</param>
/// <param name="DiagnosticCode">A stable diagnostic code.</param>
public sealed record PlannerOperationFailure(
    PlannerFailureCategory Category,
    PlannerFailureTarget Target,
    string? Reference,
    string Message,
    bool Retryable = false,
    string? DiagnosticCode = null);
