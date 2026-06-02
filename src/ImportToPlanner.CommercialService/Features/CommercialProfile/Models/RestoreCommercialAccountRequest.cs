using ImportToPlanner.CommercialService.Common.Models;

namespace ImportToPlanner.CommercialService.Features.CommercialProfile.Models;

/// <summary>
/// Restores a hosted commercial account during retention for the supplied session identity.
/// </summary>
/// <param name="SessionIdentity">The signed-in session identity.</param>
/// <param name="OccurredUtc">The operation timestamp in UTC.</param>
public sealed record RestoreCommercialAccountRequest(
    SessionIdentityContext SessionIdentity,
    DateTimeOffset OccurredUtc);
