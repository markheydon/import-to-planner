namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a complete dry-run preview of import actions.
/// </summary>
public sealed record ImportPlanPreview
{
    /// <summary>
    /// Gets or sets the selected group identifier.
    /// </summary>
    public required string GroupId { get; init; }

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
    /// Gets or sets the planned goal/category actions keyed by goal text.
    /// </summary>
    public required IReadOnlyDictionary<string, PlannedEntityAction> GoalActions { get; init; }

    /// <summary>
    /// Gets or sets the per-task actions.
    /// </summary>
    public required IReadOnlyList<ImportTaskPlanItem> TaskActions { get; init; }
}
