namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a complete dry-run preview of import actions.
/// </summary>
public sealed record ImportPlanPreview
{
    /// <summary>
    /// Gets or sets the selected container identifier.
    /// </summary>
    public required string ContainerId { get; init; }

    /// <summary>
    /// Gets or sets the plan identifier if the plan already exists; otherwise <see langword="null"/>.
    /// </summary>
    public string? PlanId { get; init; }


    /// <summary>
    /// Gets or sets the plan name.
    /// </summary>
    public required string PlanName { get; init; }

    /// <summary>
    /// Gets or sets the planned action for the plan.
    /// </summary>
    public required PlannedEntityAction PlanAction { get; init; }

    /// <summary>
    /// Gets or sets the planned bucket actions keyed by bucket name.
    /// </summary>
    public required IReadOnlyDictionary<string, PlannedEntityAction> BucketActions { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether preview was generated with unresolved validation errors.
    /// </summary>
    public bool HasValidationErrors { get; init; }

    /// <summary>
    /// Gets or sets the request fingerprint used to validate execution request parity.
    /// </summary>
    public required string RequestFingerprint { get; init; }

    /// <summary>
    /// Gets or sets the planner state fingerprint captured during preview.
    /// </summary>
    public required string PlannerStateFingerprint { get; init; }

    /// <summary>
    /// Gets or sets when the preview was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the per-task actions.
    /// </summary>
    public required IReadOnlyList<ImportTaskPlanItem> TaskActions { get; init; }
}
