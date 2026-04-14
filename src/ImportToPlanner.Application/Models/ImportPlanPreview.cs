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
    /// Gets or sets the per-task actions.
    /// </summary>
    public required IReadOnlyList<ImportTaskPlanItem> TaskActions { get; init; }
}
