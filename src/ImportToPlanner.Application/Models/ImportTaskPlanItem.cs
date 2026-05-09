namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents one task decision in the dry-run preview.
/// </summary>
/// <param name="RowNumber">The original CSV row number.</param>
/// <param name="TaskName">The task name.</param>
/// <param name="Bucket">The resolved bucket name.</param>
/// <param name="Goals">The optional goals from CSV.</param>
/// <param name="Action">The planned action for the task.</param>
/// <param name="Reason">Optional reason for skipped actions.</param>
/// <param name="IsStale">Indicates whether this item became stale before execution.</param>
/// <param name="ReportStatus">The user-facing report status for this item.</param>
public sealed record ImportTaskPlanItem(
    int RowNumber,
    string TaskName,
    string Bucket,
    IReadOnlyList<string>? Goals,
    PlannedEntityAction Action,
    string? Reason = null,
    bool IsStale = false,
    string? ReportStatus = null);
