namespace ImportToPlanner.Domain;

/// <summary>
/// Represents an existing Planner task used for idempotency checks.
/// </summary>
/// <param name="Id">The task identifier.</param>
/// <param name="Title">The task name.</param>
/// <param name="PlanId">The parent plan identifier.</param>
public sealed record PlannerTaskSnapshot(string Id, string Title, string PlanId);
