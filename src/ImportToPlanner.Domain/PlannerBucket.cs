namespace ImportToPlanner.Domain;

/// <summary>
/// Represents a Planner bucket snapshot.
/// </summary>
/// <param name="Id">The bucket identifier.</param>
/// <param name="Name">The bucket name.</param>
/// <param name="PlanId">The parent plan identifier.</param>
public sealed record PlannerBucket(string Id, string Name, string PlanId);
