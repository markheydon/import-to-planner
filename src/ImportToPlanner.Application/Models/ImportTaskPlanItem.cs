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
/// <param name="DueDate">The optional due date from CSV, shown in preview even when the row is skipped.</param>
/// <param name="AssigneeAddresses">The original CSV assignee addresses for preview display.</param>
/// <param name="ResolvedAssigneeIds">The destination member identifiers to assign on create.</param>
/// <param name="ResolvedAssignees">The CSV addresses resolved to destination members at preview.</param>
/// <param name="UnresolvedAssignees">The assignee addresses that need manual follow-up at preview.</param>
public sealed record ImportTaskPlanItem(
    int RowNumber,
    string TaskName,
    string Bucket,
    IReadOnlyList<string>? Goals,
    PlannedEntityAction Action,
    string? Reason = null,
    bool IsStale = false,
    string? ReportStatus = null,
    DateOnly? DueDate = null,
    IReadOnlyList<string>? AssigneeAddresses = null,
    IReadOnlyList<string>? ResolvedAssigneeIds = null,
    IReadOnlyList<ResolvedAssignee>? ResolvedAssignees = null,
    IReadOnlyList<UnresolvedAssignee>? UnresolvedAssignees = null);
