using ImportToPlanner.Application.CsvImport.Models;
using ImportToPlanner.Domain;

namespace ImportToPlanner.Application.ImportPlanning.Models;

/// <summary>
/// Represents an import planning request.
/// </summary>
/// <param name="ContainerId">The selected planner container identifier.</param>
/// <param name="ContainerType">The type of the selected Planner container.</param>
/// <param name="PlanId">The selected target plan identifier.</param>
/// <param name="PlanName">The selected target plan name.</param>
/// <param name="Rows">The parsed CSV rows.</param>
public sealed record ImportPlanningRequest(
    string ContainerId,
    ContainerType ContainerType,
    string PlanId,
    string PlanName,
    IReadOnlyList<CsvTaskRow> Rows);
