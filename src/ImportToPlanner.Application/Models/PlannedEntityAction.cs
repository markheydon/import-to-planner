namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents how an entity will be handled during execution.
/// </summary>
public enum PlannedEntityAction
{
    /// <summary>
    /// Reuses an already existing entity.
    /// </summary>
    Reuse = 0,

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Skips processing the entity.
    /// </summary>
    Skip = 2,
}
