namespace ImportToPlanner.Application.Models;

/// <summary>
/// The final automated outcome for one source CSV task row after execution.
/// </summary>
public enum ImportSourceRowOutcomeKind
{
    /// <summary>A new Planner task was created for the row.</summary>
    Created,

    /// <summary>The row was reused or skipped without creating a new task.</summary>
    ReusedOrSkipped,

    /// <summary>Execution failed for the row after preview approval.</summary>
    Failed,
}
