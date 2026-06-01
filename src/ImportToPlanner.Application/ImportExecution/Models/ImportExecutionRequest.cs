using ImportToPlanner.Application.ImportPlanning.Models;

namespace ImportToPlanner.Application.ImportExecution.Models;

/// <summary>
/// Represents an approved import execution request.
/// </summary>
/// <param name="Request">The planning request that produced the approved preview.</param>
/// <param name="ApprovedPreview">The approved preview that must match planner state at execution time.</param>
public sealed record ImportExecutionRequest(
    ImportPlanningRequest Request,
    ImportPlanPreview ApprovedPreview);
