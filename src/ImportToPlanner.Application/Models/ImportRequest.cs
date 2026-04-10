namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a user import request.
/// </summary>
/// <param name="GroupId">The selected Microsoft 365 group identifier.</param>
/// <param name="PlanName">The target plan name.</param>
/// <param name="Rows">The parsed CSV rows.</param>
public sealed record ImportRequest(string GroupId, string PlanName, IReadOnlyList<CsvTaskRow> Rows);
