namespace ImportToPlanner.Domain;

/// <summary>
/// Represents a Planner container available to the current user.
/// </summary>
/// <param name="Id">The container identifier.</param>
/// <param name="DisplayName">The user-facing container name.</param>
/// <param name="Type">The container type.</param>
public sealed record PlannerContainer(string Id, string DisplayName, ContainerType Type);
