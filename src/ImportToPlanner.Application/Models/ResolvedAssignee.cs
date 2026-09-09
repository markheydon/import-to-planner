namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a CSV assignee address resolved to a destination member at preview.
/// </summary>
/// <param name="Address">The CSV assignee value.</param>
/// <param name="MemberId">The destination member identifier to assign on create.</param>
public sealed record ResolvedAssignee(string Address, string MemberId);
