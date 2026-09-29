namespace ImportToPlanner.Application.Models;

/// <summary>
/// One final automated outcome for a row from the uploaded source CSV.
/// </summary>
/// <param name="RowNumber">The source CSV data row number.</param>
/// <param name="TaskName">The task name from the source row.</param>
/// <param name="Outcome">The outcome kind for the row.</param>
/// <param name="TaskIdentifier">The Planner task identifier when created; otherwise null.</param>
/// <param name="Reason">Optional user-safe explanation; must not contain secrets or tenant identifiers.</param>
public sealed record ImportSourceRowOutcome(
    int RowNumber,
    string TaskName,
    ImportSourceRowOutcomeKind Outcome,
    string? TaskIdentifier = null,
    string? Reason = null);
