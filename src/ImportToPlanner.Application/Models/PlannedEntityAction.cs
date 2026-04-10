namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents how an entity will be handled during execution.
/// </summary>
public enum PlannedEntityAction
{
    Reuse = 0,
    Create = 1,
    Skip = 2,
}
