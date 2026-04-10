namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents one task decision in the dry-run preview.
/// </summary>
/// <param name="RowNumber">The original CSV row number.</param>
/// <param name="TaskName">The task name.</param>
/// <param name="Bucket">The resolved bucket name.</param>
/// <param name="Goal">The optional goal/category value.</param>
/// <param name="Action">The planned action for the task.</param>
/// <param name="Reason">Optional reason for skipped actions.</param>
public sealed record ImportTaskPlanItem(
    int RowNumber,
    string TaskName,
    string Bucket,
    string? Goal,
    PlannedEntityAction Action,
    string? Reason = null);
