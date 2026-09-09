namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a CSV assignee address that could not be resolved to a destination member at preview.
/// </summary>
/// <param name="Address">The CSV assignee value.</param>
/// <param name="ReasonCode">The stable reason code for follow-up presentation.</param>
public sealed record UnresolvedAssignee(string Address, string ReasonCode);
