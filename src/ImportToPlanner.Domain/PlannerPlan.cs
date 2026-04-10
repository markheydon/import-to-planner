namespace ImportToPlanner.Domain;

/// <summary>
/// Represents a Planner plan snapshot.
/// </summary>
/// <param name="Id">The plan identifier.</param>
/// <param name="Title">The plan title.</param>
/// <param name="GroupId">The containing group identifier.</param>
public sealed record PlannerPlan(string Id, string Title, string GroupId);
