namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents one CSV row after parsing and normalization.
/// </summary>
/// <param name="RowNumber">The original CSV row number.</param>
/// <param name="TaskName">The required task name.</param>
/// <param name="Description">The optional task description.</param>
/// <param name="Priority">The optional Planner priority in the range 0-10.</param>
/// <param name="Bucket">The optional bucket name.</param>
/// <param name="Goal">The optional goal name used to generate manual post-import actions.</param>
/// <param name="DueDate">The optional calendar due date from the CSV Due Date column.</param>
/// <param name="AssigneeAddresses">The optional assignee addresses from the CSV Assigned To column.</param>
public sealed record CsvTaskRow(
    int RowNumber,
    string TaskName,
    string? Description,
    int? Priority,
    string? Bucket,
    string? Goal,
    DateOnly? DueDate = null,
    IReadOnlyList<string>? AssigneeAddresses = null);
