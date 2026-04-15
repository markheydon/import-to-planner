namespace ImportToPlanner.Domain;

/// <summary>
/// Represents the Planner container type.
/// </summary>
public enum ContainerType
{
    /// <summary>
    /// A Microsoft 365 group container.
    /// </summary>
    Group,

    /// <summary>
    /// A Planner roster container.
    /// </summary>
    Roster,

    /// <summary>
    /// A personal user container.
    /// </summary>
    User,
}
