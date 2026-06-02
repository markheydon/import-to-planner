using ImportToPlanner.CommercialService.Common.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Models;

/// <summary>
/// Resolves hosted commercial access for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
/// <param name="CommercialModeEnabled">Whether hosted commercial mode is enabled for this request.</param>
/// <param name="OccurredUtc">The operation timestamp in UTC.</param>
public sealed record ResolveCommercialAccessRequest(
    SessionIdentityContext SessionIdentity,
    bool CommercialModeEnabled,
    DateTimeOffset OccurredUtc);
