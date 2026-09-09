using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents the result of creating a Planner task through the gateway.
/// </summary>
/// <param name="Snapshot">The created task snapshot.</param>
/// <param name="AppliedAssigneeIds">The assignee identifiers actually present on the created task.</param>
public sealed record CreatedPlannerTask(
    PlannerTaskSnapshot Snapshot,
    IReadOnlyList<string> AppliedAssigneeIds);
