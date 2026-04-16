namespace ImportToPlanner.Domain;

/// <summary>
/// Represents a Planner plan snapshot.
/// </summary>
/// <param name="Id">The plan identifier.</param>
/// <param name="Title">The plan title.</param>
/// <param name="ContainerId">The containing container identifier.</param>
/// <param name="ContainerType">The containing container type.</param>
/// <param name="ContainerUrl">The canonical Graph container URL, when available.</param>
/// <param name="RawContainerType">The raw Graph container type value, when available.</param>
public sealed record PlannerPlan(
    string Id,
    string Title,
    string? ContainerId = null,
    ContainerType? ContainerType = null,
    string? ContainerUrl = null,
    string? RawContainerType = null);
