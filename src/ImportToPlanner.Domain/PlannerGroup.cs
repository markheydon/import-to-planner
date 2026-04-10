namespace ImportToPlanner.Domain;

/// <summary>
/// Represents a Microsoft 365 group that can contain Planner plans.
/// </summary>
/// <param name="Id">The group identifier.</param>
/// <param name="DisplayName">The user-facing group display name.</param>
public sealed record PlannerGroup(string Id, string DisplayName);
