using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents an import planning request.
/// </summary>
/// <param name="ContainerId">The selected planner container identifier.</param>
/// <param name="ContainerType">The type of the selected Planner container.</param>
/// <param name="PlanId">The selected target plan identifier.</param>
/// <param name="PlanName">The selected target plan name.</param>
/// <param name="Rows">The parsed CSV rows.</param>
public sealed record ImportPlanningRequest(
    string ContainerId,
    ContainerType ContainerType,
    string PlanId,
    string PlanName,
    IReadOnlyList<CsvTaskRow> Rows);

/// <summary>
/// Represents an approved import execution request.
/// </summary>
/// <param name="Request">The planning request that produced the approved preview.</param>
/// <param name="ApprovedPreview">The approved preview that must match planner state at execution time.</param>
public sealed record ImportExecutionRequest(
    ImportPlanningRequest Request,
    ImportPlanPreview ApprovedPreview);

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
