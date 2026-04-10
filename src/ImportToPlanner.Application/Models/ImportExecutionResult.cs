namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents the final import execution report.
/// </summary>
public sealed record ImportExecutionResult
{
    /// <summary>
    /// Gets or sets the created item descriptions.
    /// </summary>
    public required IReadOnlyList<string> Created { get; init; }

    /// <summary>
    /// Gets or sets the reused or skipped item descriptions.
    /// </summary>
    public required IReadOnlyList<string> ReusedOrSkipped { get; init; }

    /// <summary>
    /// Gets or sets the execution errors.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }
}
