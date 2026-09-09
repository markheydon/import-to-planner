namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a post-import manual action required in Planner UI.
/// </summary>
/// <param name="ActionType">The action type (for example, CreateGoal or LinkTaskToGoal).</param>
/// <param name="GoalName">The optional goal name.</param>
/// <param name="TaskName">The optional task name.</param>
/// <param name="Details">Optional additional details.</param>
/// <param name="PersonIdentifier">The CSV assignee address for <c>AssignPersonToTask</c> actions.</param>
public sealed record ManualAction(
    string ActionType,
    string? GoalName,
    string? TaskName,
    string? Details,
    string? PersonIdentifier = null);
