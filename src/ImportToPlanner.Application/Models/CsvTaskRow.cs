namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents one CSV row after parsing and normalization.
/// </summary>
/// <param name="RowNumber">The original CSV row number.</param>
/// <param name="TaskName">The required task name.</param>
/// <param name="Description">The optional task description.</param>
/// <param name="Priority">The optional Planner priority in the range 0-10.</param>
/// <param name="Bucket">The optional bucket name.</param>
/// <param name="Goal">The optional goal name that maps to a Planner category.</param>
public sealed record CsvTaskRow(
    int RowNumber,
    string TaskName,
    string? Description,
    int? Priority,
    string? Bucket,
    string? Goal);
