namespace ImportToPlanner.Domain;

/// <summary>
/// Represents a Planner plan snapshot.
/// </summary>
/// <param name="Id">The plan identifier.</param>
/// <param name="Title">The plan title.</param>
/// <param name="ContainerId">The containing container identifier.</param>
/// <param name="ContainerType">The containing container type.</param>
public sealed record PlannerPlan(string Id, string Title, string? ContainerId = null, ContainerType? ContainerType = null);
