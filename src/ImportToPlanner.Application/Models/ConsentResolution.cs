namespace ImportToPlanner.Application.Models;

/// <summary>
/// Represents consent resolution outcomes for the active tenant.
/// </summary>
public enum ConsentResolutionStatus
{
    Unknown,
    Granted,
    UserConsentAvailable,
    AdminConsentRequired,
    Declined,
    Unavailable,
}

/// <summary>
/// Represents a structured consent resolution result.
/// </summary>
/// <param name="Status">The resolved consent status.</param>
/// <param name="RequiredScopes">The required delegated scopes.</param>
/// <param name="AdminConsentUri">The optional administrator consent URI.</param>
/// <param name="MessageKey">A stable presenter-facing message key.</param>
/// <param name="DiagnosticCode">An optional stable diagnostic code.</param>
public sealed record ConsentResolution(
    ConsentResolutionStatus Status,
    IReadOnlyList<string> RequiredScopes,
    Uri? AdminConsentUri,
    string MessageKey,
    string? DiagnosticCode = null)
{
    /// <summary>
    /// Returns a granted consent resolution.
    /// </summary>
    /// <param name="requiredScopes">The required delegated scopes.</param>
    /// <returns>A granted result.</returns>
    public static ConsentResolution Granted(IReadOnlyList<string> requiredScopes)
    {
        ArgumentNullException.ThrowIfNull(requiredScopes);
        return new ConsentResolution(ConsentResolutionStatus.Granted, requiredScopes, null, "consent.granted");
    }
}
