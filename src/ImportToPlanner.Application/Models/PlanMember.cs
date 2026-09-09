namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents a destination member who can be assigned to Planner tasks.
/// </summary>
/// <param name="Id">The opaque destination user identifier used on create.</param>
/// <param name="Mail">The work email address, which may be empty.</param>
/// <param name="SignInName">The organisational sign-in name (UPN), which may be empty.</param>
public sealed record PlanMember(string Id, string? Mail, string? SignInName);
