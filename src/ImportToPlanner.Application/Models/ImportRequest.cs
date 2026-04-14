namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a user import request.
/// </summary>
/// <param name="ContainerId">The selected Planner container identifier.</param>
/// <param name="PlanName">The target plan name.</param>
/// <param name="Rows">The parsed CSV rows.</param>
public sealed record ImportRequest(string ContainerId, string PlanName, IReadOnlyList<CsvTaskRow> Rows);
