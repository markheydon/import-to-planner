using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a user import request.
/// </summary>
/// <param name="ContainerId">The selected Planner container identifier.</param>
/// <param name="ContainerType">The type of the selected Planner container.</param>
/// <param name="PlanId">The selected target plan identifier.</param>
/// <param name="PlanName">The selected target plan name.</param>
/// <param name="Rows">The parsed CSV rows.</param>
public sealed record ImportRequest(
    string ContainerId,
    ContainerType ContainerType,
    string PlanId,
    string PlanName,
    IReadOnlyList<CsvTaskRow> Rows);
