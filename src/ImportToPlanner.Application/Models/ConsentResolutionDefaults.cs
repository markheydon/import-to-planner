namespace ImportToPlanner.Application.Models;

/// <summary>
/// Defines default consent information used by planning use cases.
/// </summary>
/// <param name="RequiredScopes">The required delegated scopes.</param>
/// <param name="AdminConsentUri">The optional administrator consent URI.</param>
public sealed record ConsentResolutionDefaults(
    IReadOnlyList<string> RequiredScopes,
    Uri? AdminConsentUri);
